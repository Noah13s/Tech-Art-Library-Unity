using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DefaultExecutionOrder(100)]
public class SphereSurfacePatchGenerator : MonoBehaviour
{
    [Header("References")]
    public PerspectiveIllusionObject planet;
    public FloatPrecisionPlayer player;
    public Texture2D heightMap;             // Height map texture

    [Header("Proximity Settings")]
    public float proximityRange = 50f;      // Range (from planet's surface) to generate the patch

    [Header("Mesh Settings")]
    public int gridResolution = 10;         // Number of segments for the patch
    public float minPatchSize = 10f;        // Smallest patch size (when far away)
    public float maxPatchSize = 100f;       // Largest patch size (up close)
    [Tooltip("Small outward offset in meters that keeps the close-up patch from depth-fighting with the coarse planet mesh.")]
    [Min(0f)] public float surfaceSeparation = 0.5f;

    [Header("Height Map Control")]
    public float elevationStrength = 10f;   // Maximum elevation displacement from height map
    public Vector2 heightMapUVScale = Vector2.one;  // Scale for height map UV sampling
    public Vector2 heightMapUVOffset = Vector2.zero;  // Offset for height map UV sampling
    [Range(0f, 1f)]
    public float displacementMin = 0f;      // Raw height value corresponding to ground level (0 elevation)
    [Range(0f, 1f)]
    public float displacementMax = 1f;      // Raw height value corresponding to peak elevation (full displacement)

    public enum UVMappingMode { Spherical, Planar }
    [Header("UV Mapping Control")]
    public UVMappingMode uvMappingMode = UVMappingMode.Planar;
    public Vector2 uvScale = Vector2.one;
    public Vector2 uvOffset = Vector2.zero;
    public Material material;

    [Header("Ground Collision")]
    [Tooltip("Keeps the logical double-precision player position above the displaced terrain.")]
    public bool preventGroundPenetration = true;
    [Tooltip("Distance in meters maintained between the player's origin and the terrain.")]
    [Min(0f)] public float groundClearance = 1f;
    [Tooltip("Exponential damping applied to sideways velocity while the player is grounded. Higher values stop faster.")]
    [Min(0f)] public float groundFriction = 8f;
    [Tooltip("Distance above the sampled surface that still counts as grounded.")]
    [Min(0f)] public float groundedTolerance = 0.25f;
    [Tooltip("Tangential speeds below this value are stopped completely.")]
    [Min(0f)] public float groundStopSpeed = 0.05f;
    [Tooltip("Enable only if another physics object needs to interact with the generated patch collider.")]
    public bool usePatchCollider = false;

    [Header("Close-up LOD")]
    [Tooltip("Hides the coarse planet sphere while this local surface patch is visible. The coarse sphere cannot share the local render and shadow space without precision artifacts.")]
    public bool hidePlanetRendererWhileActive = true;
    [Tooltip("Surface distance below which the local patch fully replaces the coarse planet renderer.")]
    [Min(0f)] public float coarsePlanetHideRange = 5000f;

    public new Collider collider;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh patchMesh;
    private MaterialPropertyBlock materialProperties;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;
    private int cachedResolution = -1;
    private DoubleVector3 previousPlayerPosition;
    private bool hasPreviousPlayerPosition;
    private Renderer planetRenderer;
    private ShadowCastingMode originalPlanetShadowMode;
    private bool originalPlanetRendererEnabled;
    private bool hasOriginalPlanetShadowMode;

    private static readonly int OffsetProperty = Shader.PropertyToID("_Offset");
    private static readonly int TilingProperty = Shader.PropertyToID("_Tiling");

    private readonly struct SurfaceCoordinates
    {
        public readonly double x;
        public readonly double y;

        public SurfaceCoordinates(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        materialProperties = new MaterialPropertyBlock();
        if (material != null)
        {
            meshRenderer.sharedMaterial = material;
        }

        // The generated terrain must receive the player's shadow, but casting the
        // enormous patch into its own shadow map produces severe precision acne.
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;

        if (collider == null)
        {
            collider = GetComponent<Collider>();
        }

        if (planet != null)
        {
            planetRenderer = planet.GetComponent<Renderer>();
            if (planetRenderer != null)
            {
                originalPlanetShadowMode = planetRenderer.shadowCastingMode;
                originalPlanetRendererEnabled = planetRenderer.enabled;
                hasOriginalPlanetShadowMode = true;
            }
        }

        patchMesh = new Mesh();
        patchMesh.name = "Surface Patch Mesh";
        patchMesh.MarkDynamic();
        meshFilter.mesh = patchMesh;

        if (player != null)
        {
            previousPlayerPosition = player.playerPosition;
            hasPreviousPlayerPosition = true;
        }

        SetPatchVisible(false);
    }

    private void UpdateSurfacePatch()
    {
        if (planet == null || player == null || patchMesh == null)
        {
            SetPatchVisible(false);
            return;
        }

        // Use the same player-relative center and scale as the planet. This keeps the
        // patch aligned while the planet transitions between perspective and true scale.
        planet.CalculateRenderState(
            out DoubleVector3 renderedCenter,
            out double renderedScale,
            out _,
            out double surfaceDistance);

        if (surfaceDistance <= proximityRange)
        {
            float distanceRatio = proximityRange > 0f
                ? Mathf.Clamp01(Mathf.Max(0f, (float)surfaceDistance) / proximityRange)
                : 0f;
            float patchSize = Mathf.Lerp(maxPatchSize, minPatchSize, distanceRatio);

            if (patchSize <= Mathf.Epsilon)
            {
                SetPatchVisible(false);
                return;
            }

            double perspectiveScale = planet.simulationScale > double.Epsilon
                ? renderedScale / planet.simulationScale
                : 1.0;
            float renderedPatchSize = (float)(patchSize * perspectiveScale);
            float renderedElevationStrength = (float)(elevationStrength * perspectiveScale);
            float renderedSurfaceSeparation = (float)(surfaceSeparation * perspectiveScale);

            transform.position = player.transform.position;
            GenerateSurfacePatch(
                renderedCenter,
                renderedScale * 0.5,
                planet.simulationScale * 0.5,
                renderedPatchSize,
                renderedElevationStrength,
                renderedSurfaceSeparation);
            SetPatchVisible(true, surfaceDistance <= coarsePlanetHideRange);
        }
        else
        {
            SetPatchVisible(false);
        }
    }

    void LateUpdate()
    {
        if (planet == null || player == null)
        {
            SetPatchVisible(false);
            return;
        }

        // Resolve ground contact before generating the visible patch. Generating in
        // Update used the previous logical position for one frame and made the terrain
        // appear to jump while the player skimmed the surface.
        if (preventGroundPenetration)
        {
            ConstrainPlayerAboveGround();
        }

        UpdateSurfacePatch();

        previousPlayerPosition = player.playerPosition;
        hasPreviousPlayerPosition = true;
    }

    void GenerateSurfacePatch(
        DoubleVector3 relativeCenter,
        double planetRadius,
        double simulationRadius,
        float patchSize,
        float renderedElevationStrength,
        float renderedSurfaceSeparation)
    {
        int resolution = Mathf.Max(1, gridResolution);
        EnsureMeshBuffers(resolution);

        // The player is the origin in the rendered coordinate system.
        DoubleVector3 direction = relativeCenter.Negate().Normalized();
        if (direction.Magnitude() <= double.Epsilon)
        {
            direction = new DoubleVector3(0, 1, 0);
        }

        // Compute the point on the sphere closest to the player.
        DoubleVector3 surfacePoint = relativeCenter + direction * planetRadius;

        // Build a tangent plane at surfacePoint.
        DoubleVector3 up = direction;
        DoubleVector3 right = new DoubleVector3(0, 1, 0).Cross(up);
        if (right.Magnitude() <= double.Epsilon)
        {
            right = new DoubleVector3(0, 0, 1).Cross(up);
        }

        right = right.Normalized();
        DoubleVector3 forward = right.Cross(up).Normalized();
        SurfaceCoordinates surfaceOrigin = GetSurfaceCoordinatesInMeters(direction, simulationRadius);

        if (uvMappingMode == UVMappingMode.Planar)
        {
            UpdateMaterialOffset(surfaceOrigin);
        }

        // Create a grid in the tangent plane and project each point onto the sphere.
        int vertexIndex = 0;
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                double tX = (double)x / resolution;
                double tY = (double)y / resolution;
                double offsetU = (tX - 0.5) * patchSize;
                double offsetV = (tY - 0.5) * patchSize;
                DoubleVector3 pointOnPlane = surfacePoint + right * offsetU + forward * offsetV;
                DoubleVector3 dirFromCenter = (pointOnPlane - relativeCenter).Normalized();
                double displacedRadius = planetRadius + renderedSurfaceSeparation;

                Vector2 sphericalUV = GetSphericalUV(dirFromCenter);
                displacedRadius += SampleElevation(sphericalUV, renderedElevationStrength);

                DoubleVector3 pointOnSphere = relativeCenter + dirFromCenter * displacedRadius;
                vertices[vertexIndex] = (Vector3)pointOnSphere;

                if (uvMappingMode == UVMappingMode.Spherical)
                {
                    uvs[vertexIndex] = new Vector2(
                        sphericalUV.x * uvScale.x + uvOffset.x,
                        sphericalUV.y * uvScale.y + uvOffset.y);
                }
                else // Planar mapping
                {
                    // Store planet-anchored surface distances in UV0. The ground shader
                    // can now express its checker frequency in cycles per meter without
                    // depending on the rendered patch size or a lossy world position.
                    SurfaceCoordinates surfaceMeters = GetSurfaceCoordinatesInMeters(dirFromCenter, simulationRadius);
                    double localU = surfaceMeters.x - surfaceOrigin.x;
                    double longitudePeriod = 2.0 * Math.PI * simulationRadius;
                    if (longitudePeriod > double.Epsilon)
                    {
                        // Keep vertices adjacent when the patch crosses the longitude seam.
                        localU -= Math.Round(localU / longitudePeriod) * longitudePeriod;
                    }

                    uvs[vertexIndex] = new Vector2(
                        (float)(localU * uvScale.x) + uvOffset.x,
                        (float)((surfaceMeters.y - surfaceOrigin.y) * uvScale.y) + uvOffset.y);
                }

                vertexIndex++;
            }
        }

        patchMesh.Clear();
        patchMesh.indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
        patchMesh.vertices = vertices;
        patchMesh.triangles = triangles;
        patchMesh.uv = uvs;
        patchMesh.RecalculateNormals();
        patchMesh.RecalculateBounds();

        UpdateColliderBounds();
    }

    private void EnsureMeshBuffers(int resolution)
    {
        if (resolution == cachedResolution)
        {
            return;
        }

        cachedResolution = resolution;
        int rowSize = resolution + 1;
        vertices = new Vector3[rowSize * rowSize];
        uvs = new Vector2[vertices.Length];
        triangles = new int[resolution * resolution * 6];

        int triangleIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = y * rowSize + x;
                triangles[triangleIndex++] = i;
                triangles[triangleIndex++] = i + rowSize;
                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + rowSize;
                triangles[triangleIndex++] = i + rowSize + 1;
            }
        }
    }

    private static Vector2 GetSphericalUV(DoubleVector3 direction)
    {
        float x = (float)direction.x;
        float y = Mathf.Clamp((float)direction.y, -1f, 1f);
        float z = (float)direction.z;
        float u = Mathf.Atan2(z, x) / (2f * Mathf.PI) + 0.5f;
        float v = 1f - Mathf.Acos(y) / Mathf.PI;
        return new Vector2(u, v);
    }

    private static SurfaceCoordinates GetSurfaceCoordinatesInMeters(DoubleVector3 direction, double radius)
    {
        double longitude = Math.Atan2(direction.z, direction.x);
        double latitude = Math.Asin(Math.Max(-1.0, Math.Min(1.0, direction.y)));
        return new SurfaceCoordinates(longitude * radius, latitude * radius);
    }

    private void UpdateMaterialOffset(SurfaceCoordinates surfaceOrigin)
    {
        if (material == null || !material.HasProperty(OffsetProperty))
        {
            return;
        }

        float frequency = material.HasProperty(TilingProperty)
            ? Mathf.Abs(material.GetFloat(TilingProperty))
            : 0f;
        double repeatDistance = frequency > Mathf.Epsilon ? 1.0 / frequency : 1.0;
        float phaseX = WrapToRepeat(surfaceOrigin.x * uvScale.x, repeatDistance);
        float phaseY = WrapToRepeat(surfaceOrigin.y * uvScale.y, repeatDistance);

        meshRenderer.GetPropertyBlock(materialProperties);
        materialProperties.SetVector(OffsetProperty, new Vector4(phaseX, phaseY, 0f, 0f));
        meshRenderer.SetPropertyBlock(materialProperties);
    }

    private static float WrapToRepeat(double value, double repeatDistance)
    {
        double wrapped = value - Math.Floor(value / repeatDistance) * repeatDistance;
        return (float)wrapped;
    }

    private float SampleElevation(DoubleVector3 direction, float strength)
    {
        return SampleElevation(GetSphericalUV(direction), strength);
    }

    private float SampleElevation(Vector2 sphericalUV, float strength)
    {
        if (heightMap == null)
        {
            return 0f;
        }

        float texU = sphericalUV.x * heightMapUVScale.x + heightMapUVOffset.x;
        float texV = sphericalUV.y * heightMapUVScale.y + heightMapUVOffset.y;
        float rawHeight = heightMap.GetPixelBilinear(texU, texV).r;
        return Mathf.InverseLerp(displacementMin, displacementMax, rawHeight) * strength;
    }

    private void ConstrainPlayerAboveGround()
    {
        double baseRadius = planet.simulationScale * 0.5;
        DoubleVector3 currentRelativePosition = player.playerPosition - planet.simulationPosition;
        double currentDistance = currentRelativePosition.Magnitude();
        DoubleVector3 outwardDirection = currentDistance > double.Epsilon
            ? currentRelativePosition * (1.0 / currentDistance)
            : new DoubleVector3(0, 1, 0);
        double minimumRadius = baseRadius + surfaceSeparation
            + SampleElevation(outwardDirection, elevationStrength) + groundClearance;

        if (currentDistance < minimumRadius)
        {
            PlacePlayerOnSurface(outwardDirection, minimumRadius);
            return;
        }

        if (currentDistance <= minimumRadius + groundedTolerance)
        {
            ApplyGroundVelocity(outwardDirection);
            return;
        }

        if (!hasPreviousPlayerPosition)
        {
            return;
        }

        // Catch a complete pass through the planet when a very large movement step has
        // both its start and end outside the surface.
        DoubleVector3 previousRelativePosition = previousPlayerPosition - planet.simulationPosition;
        double collisionRadius = baseRadius + surfaceSeparation + groundClearance;
        if (!TryGetSphereEntry(previousRelativePosition, currentRelativePosition, collisionRadius, out DoubleVector3 entryPoint))
        {
            return;
        }

        // Refine the entry against the displaced radius at the contact direction.
        DoubleVector3 contactDirection = entryPoint.Normalized();
        for (int iteration = 0; iteration < 2; iteration++)
        {
            collisionRadius = baseRadius + surfaceSeparation
                + SampleElevation(contactDirection, elevationStrength) + groundClearance;
            if (!TryGetSphereEntry(previousRelativePosition, currentRelativePosition, collisionRadius, out entryPoint))
            {
                return;
            }

            contactDirection = entryPoint.Normalized();
        }

        PlacePlayerOnSurface(contactDirection, collisionRadius);
    }

    private static bool TryGetSphereEntry(
        DoubleVector3 segmentStart,
        DoubleVector3 segmentEnd,
        double radius,
        out DoubleVector3 entryPoint)
    {
        entryPoint = new DoubleVector3(0, 0, 0);
        DoubleVector3 segment = segmentEnd - segmentStart;
        double a = segment.Dot(segment);
        if (a <= double.Epsilon)
        {
            return false;
        }

        double b = 2.0 * segmentStart.Dot(segment);
        double c = segmentStart.Dot(segmentStart) - radius * radius;
        double discriminant = b * b - 4.0 * a * c;
        if (discriminant < 0.0)
        {
            return false;
        }

        double entryT = (-b - Math.Sqrt(discriminant)) / (2.0 * a);
        if (entryT <= 1e-9)
        {
            // Starting on the surface and moving inward is still a collision. This
            // prevents a single large step from crossing the entire planet.
            if (Math.Abs(c) <= 1e-5 && segmentStart.Dot(segment) < 0.0)
            {
                entryPoint = segmentStart.Normalized() * radius;
                return true;
            }

            return false;
        }

        if (entryT > 1.0)
        {
            return false;
        }

        entryPoint = segmentStart + segment * entryT;
        return true;
    }

    private void PlacePlayerOnSurface(DoubleVector3 outwardDirection, double radius)
    {
        player.playerPosition = planet.simulationPosition + outwardDirection * radius;

        ApplyGroundVelocity(outwardDirection);
    }

    private void ApplyGroundVelocity(DoubleVector3 outwardDirection)
    {
        DoubleVector3 velocity = player.GetVelocity();
        double normalSpeed = velocity.Dot(outwardDirection);
        double retainedNormalSpeed = Math.Max(0.0, normalSpeed);
        DoubleVector3 tangentialVelocity = velocity - outwardDirection * normalSpeed;

        double frictionFactor = Math.Exp(-groundFriction * Time.deltaTime);
        tangentialVelocity *= frictionFactor;

        if (tangentialVelocity.Magnitude() < groundStopSpeed)
        {
            tangentialVelocity = new DoubleVector3(0, 0, 0);
        }

        player.SetVelocity(outwardDirection * retainedNormalSpeed + tangentialVelocity);
    }

    private void UpdateColliderBounds()
    {
        if (!usePatchCollider)
        {
            return;
        }

        if (collider is BoxCollider boxCollider)
        {
            boxCollider.center = patchMesh.bounds.center;
            boxCollider.size = patchMesh.bounds.size;
        }
        else if (collider is MeshCollider meshCollider)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = patchMesh;
        }
    }

    private void SetPatchVisible(bool visible, bool replacePlanetRenderer = false)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }

        if (collider != null)
        {
            collider.enabled = visible && usePatchCollider;
        }

        UpdatePlanetRendererState(visible && replacePlanetRenderer);
    }

    private void UpdatePlanetRendererState(bool patchIsVisible)
    {
        if (planetRenderer == null || !hasOriginalPlanetShadowMode)
            return;

        bool useLocalPatch = patchIsVisible && hidePlanetRendererWhileActive;
        planetRenderer.enabled = useLocalPatch ? false : originalPlanetRendererEnabled;
        planetRenderer.shadowCastingMode = useLocalPatch ? ShadowCastingMode.Off : originalPlanetShadowMode;
    }

    private void OnValidate()
    {
        proximityRange = Mathf.Max(0f, proximityRange);
        gridResolution = Mathf.Max(1, gridResolution);
        minPatchSize = Mathf.Max(0f, minPatchSize);
        maxPatchSize = Mathf.Max(minPatchSize, maxPatchSize);
        surfaceSeparation = Mathf.Max(0f, surfaceSeparation);
        elevationStrength = Mathf.Max(0f, elevationStrength);
        groundClearance = Mathf.Max(0f, groundClearance);
        groundFriction = Mathf.Max(0f, groundFriction);
        groundedTolerance = Mathf.Max(0f, groundedTolerance);
        groundStopSpeed = Mathf.Max(0f, groundStopSpeed);
        coarsePlanetHideRange = Mathf.Max(0f, coarsePlanetHideRange);
    }

    private void OnDisable()
    {
        UpdatePlanetRendererState(false);
    }

    private void OnDestroy()
    {
        UpdatePlanetRendererState(false);

        if (patchMesh != null)
        {
            Destroy(patchMesh);
        }
    }
}

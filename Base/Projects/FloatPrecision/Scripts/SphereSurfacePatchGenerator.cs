using System;
using System.Collections.Generic;
using System.Linq;
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
    [Tooltip("Uniform-grid fallback resolution. Used only when adaptive tessellation is disabled.")]
    public int gridResolution = 10;
    public float minPatchSize = 10f;        // Smallest patch size (when far away)
    public float maxPatchSize = 100f;       // Largest patch size (up close)
    [Tooltip("Small outward offset in meters that keeps the close-up patch from depth-fighting with the coarse planet mesh.")]
    [Min(0f)] public float surfaceSeparation = 0.5f;

    [Header("Adaptive Terrain Tessellation")]
    [Tooltip("Uses a crack-free quadtree so triangles are concentrated near the camera and in rough terrain.")]
    public bool adaptiveTessellation = true;
    [Tooltip("Lowest subdivision level everywhere. Three produces an 8 x 8 coarse base without over-tessellating plains.")]
    [Range(1, 8)] public int minimumSubdivisionLevel = 3;
    [Tooltip("Highest subdivision level permitted in detailed terrain. Ten allows roughly 244 m cells in a 250 km patch.")]
    [Range(3, 12)] public int maximumSubdivisionLevel = 10;
    [Tooltip("Target cell size on visually flat ground near the player, in simulation metres.")]
    [Min(1f)] public float flatTerrainCellSize = 4000f;
    [Tooltip("Target cell size in mountainous terrain near the player, in simulation metres.")]
    [Min(1f)] public float roughTerrainCellSize = 350f;
    [Tooltip("Height variation that begins to be considered non-flat, in metres.")]
    [Min(0f)] public float flatHeightVariation = 40f;
    [Tooltip("Height variation considered fully mountainous, in metres.")]
    [Min(0.01f)] public float roughHeightVariation = 700f;
    [Tooltip("Radius around the player that receives the strongest tessellation, in metres.")]
    [Min(1f)] public float highDetailDistance = 100000f;
    [Tooltip("Cell-size multiplier at the edge of the detailed patch. Larger values reduce distant triangle cost.")]
    [Range(1f, 12f)] public float distantCellSizeMultiplier = 5f;
    [Tooltip("Hard leaf budget. The generator stops refining when this many adaptive cells have been allocated.")]
    [Range(256, 50000)] public int maximumLeafCount = 12000;
    [Tooltip("Minimum player movement before rebuilding the anchored mesh. Between rebuilds the mesh is translated in player-relative space.")]
    [Min(0f)] public float minimumRebuildDistance = 100f;
    [Tooltip("Relative patch-size or perspective-scale change that forces a rebuild.")]
    [Range(0.001f, 0.25f)] public float relativeRebuildThreshold = 0.025f;

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
    private readonly List<QuadLeaf> adaptiveLeaves = new();
    private readonly Dictionary<long, TerrainSample> adaptiveSamples = new();
    private readonly Dictionary<long, int> adaptiveVertexIndices = new();
    private readonly Dictionary<int, SortedSet<int>> horizontalBoundaries = new();
    private readonly Dictionary<int, SortedSet<int>> verticalBoundaries = new();
    private readonly List<Vector3> adaptiveVertices = new();
    private readonly List<Vector2> adaptiveUVs = new();
    private readonly List<int> adaptiveTriangles = new();
    private readonly List<long> leafBoundary = new();
    private DoubleVector3 meshAnchorPlayerPosition;
    private bool hasMeshAnchor;
    private float lastSimulationPatchSize = -1f;
    private double lastPerspectiveScale = -1.0;
    private int currentLeafCount;
    private float activeSimulationPatchSize;
    private bool patchIsVisible;
    private Vector3 exclusionWorldAxis = Vector3.up;
    private float exclusionHalfAngle;
    private uint exclusionRevision;
    private DoubleVector3 previousPlayerPosition;
    private bool hasPreviousPlayerPosition;
    private Renderer planetRenderer;
    private ShadowCastingMode originalPlanetShadowMode;
    private bool originalPlanetRendererEnabled;
    private bool hasOriginalPlanetShadowMode;

    private static readonly int OffsetProperty = Shader.PropertyToID("_Offset");
    private static readonly int TilingProperty = Shader.PropertyToID("_Tiling");

    public int CurrentVertexCount => patchMesh != null ? patchMesh.vertexCount : 0;
    public int CurrentTriangleCount => patchMesh != null && patchMesh.subMeshCount > 0
        ? (int)(patchMesh.GetIndexCount(0) / 3)
        : 0;
    public int CurrentLeafCount => currentLeafCount;
    public bool IsPatchVisible => patchIsVisible;
    public PerspectiveIllusionObject Planet => planet;
    public uint ExclusionRevision => exclusionRevision;

    private readonly struct QuadLeaf
    {
        public readonly int x;
        public readonly int y;
        public readonly int size;
        public readonly int depth;

        public QuadLeaf(int x, int y, int size, int depth)
        {
            this.x = x;
            this.y = y;
            this.size = size;
            this.depth = depth;
        }
    }

    private readonly struct TerrainSample
    {
        public readonly Vector3 position;
        public readonly Vector2 uv;
        public readonly float simulationElevation;

        public TerrainSample(Vector3 position, Vector2 uv, float simulationElevation)
        {
            this.position = position;
            this.uv = uv;
            this.simulationElevation = simulationElevation;
        }
    }

    private struct AdaptiveBuildContext
    {
        public DoubleVector3 relativeCenter;
        public DoubleVector3 surfacePoint;
        public DoubleVector3 right;
        public DoubleVector3 forward;
        public double renderedPlanetRadius;
        public double simulationPlanetRadius;
        public float renderedPatchSize;
        public float simulationPatchSize;
        public float renderedElevationStrength;
        public float renderedSurfaceSeparation;
        public float surfaceDistance;
        public int gridSize;
        public SurfaceCoordinates surfaceOrigin;
    }

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

            bool rebuild = ShouldRebuildPatch(patchSize, perspectiveScale);
            if (rebuild)
            {
                transform.position = player.transform.position;
                transform.localScale = Vector3.one;
                GenerateSurfacePatch(
                    renderedCenter,
                    renderedScale * 0.5,
                    planet.simulationScale * 0.5,
                    patchSize,
                    renderedPatchSize,
                    renderedElevationStrength,
                    renderedSurfaceSeparation,
                    (float)surfaceDistance);

                meshAnchorPlayerPosition = player.playerPosition;
                hasMeshAnchor = true;
                lastSimulationPatchSize = patchSize;
                lastPerspectiveScale = perspectiveScale;
                UpdateBuiltExclusion();
            }
            else
            {
                // Keep the generated vertices anchored to the same real point on the
                // planet while the player moves. This avoids rebuilding a large adaptive
                // mesh every frame and preserves double-precision stability.
                DoubleVector3 anchorOffset = meshAnchorPlayerPosition - player.playerPosition;
                transform.position = player.transform.position + (Vector3)(anchorOffset * perspectiveScale);
                float scaleRatio = lastPerspectiveScale > double.Epsilon
                    ? (float)(perspectiveScale / lastPerspectiveScale)
                    : 1f;
                transform.localScale = Vector3.one * scaleRatio;
            }

            activeSimulationPatchSize = patchSize;
            SetPatchVisible(true, surfaceDistance <= coarsePlanetHideRange);
        }
        else
        {
            SetPatchVisible(false);
            activeSimulationPatchSize = 0f;
            transform.localScale = Vector3.one;
        }
    }

    private void UpdateBuiltExclusion()
    {
        exclusionHalfAngle = 0f;
        if (planet == null || !hasMeshAnchor || lastSimulationPatchSize <= 0f)
        {
            exclusionRevision++;
            return;
        }

        DoubleVector3 outward = meshAnchorPlayerPosition - planet.simulationPosition;
        double centerDistance = outward.Magnitude();
        double planetRadius = planet.simulationScale * 0.5;
        if (centerDistance > double.Epsilon && planetRadius > double.Epsilon)
        {
            exclusionWorldAxis = (Vector3)(outward * (1.0 / centerDistance));
            exclusionHalfAngle = Mathf.Atan2(lastSimulationPatchSize * 0.5f, (float)planetRadius);
        }

        exclusionRevision++;
    }

    /// <summary>
    /// Returns the planet-space half-angle occupied by one side of the local square. The planet surface
    /// generator uses this to remove coarse triangles below the detailed terrain.
    /// Both generators use this same projected-square definition so their boundaries match.
    /// </summary>
    public bool TryGetPlanetExclusion(out Vector3 worldAxis, out float angularRadius)
    {
        worldAxis = Vector3.up;
        angularRadius = 0f;
        if (!patchIsVisible || !hasMeshAnchor || exclusionHalfAngle <= 0f)
        {
            return false;
        }

        worldAxis = exclusionWorldAxis;
        angularRadius = exclusionHalfAngle;
        return true;
    }

    private bool ShouldRebuildPatch(float patchSize, double perspectiveScale)
    {
        if (!hasMeshAnchor || patchMesh == null || patchMesh.vertexCount == 0)
        {
            return true;
        }

        double moved = (player.playerPosition - meshAnchorPlayerPosition).Magnitude();
        float movementThreshold = Mathf.Max(
            minimumRebuildDistance,
            Mathf.Min(roughTerrainCellSize * 0.5f, patchSize * 0.002f));
        if (moved >= movementThreshold)
        {
            return true;
        }

        float patchChange = Mathf.Abs(patchSize - lastSimulationPatchSize) /
            Mathf.Max(1f, lastSimulationPatchSize);
        double scaleChange = Math.Abs(perspectiveScale - lastPerspectiveScale) /
            Math.Max(1e-9, Math.Abs(lastPerspectiveScale));
        return patchChange >= relativeRebuildThreshold || scaleChange >= relativeRebuildThreshold;
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

    /// <summary>
    /// Resets the swept-collision origin after an intentional simulation-space teleport.
    /// Without this, diagnostic/editor placement can look like a single enormous
    /// movement step through the planet and is correctly snapped back to the surface.
    /// </summary>
    public void ResetPlayerMotionHistory()
    {
        if (player == null && planet != null)
        {
            player = planet.player;
        }

        if (player == null)
        {
            hasPreviousPlayerPosition = false;
            return;
        }

        previousPlayerPosition = player.playerPosition;
        hasPreviousPlayerPosition = true;
        hasMeshAnchor = false;
    }

    void GenerateSurfacePatch(
        DoubleVector3 relativeCenter,
        double planetRadius,
        double simulationRadius,
        float simulationPatchSize,
        float renderedPatchSize,
        float renderedElevationStrength,
        float renderedSurfaceSeparation,
        float surfaceDistance)
    {
        if (adaptiveTessellation)
        {
            GenerateAdaptiveSurfacePatch(
                relativeCenter,
                planetRadius,
                simulationRadius,
                simulationPatchSize,
                renderedPatchSize,
                renderedElevationStrength,
                renderedSurfaceSeparation,
                surfaceDistance);
            return;
        }

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
                double offsetU = (tX - 0.5) * renderedPatchSize;
                double offsetV = (tY - 0.5) * renderedPatchSize;
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
        patchMesh.RecalculateTangents();
        patchMesh.RecalculateBounds();

        currentLeafCount = resolution * resolution;

        UpdateColliderBounds();
    }

    private void GenerateAdaptiveSurfacePatch(
        DoubleVector3 relativeCenter,
        double renderedPlanetRadius,
        double simulationPlanetRadius,
        float simulationPatchSize,
        float renderedPatchSize,
        float renderedElevationStrength,
        float renderedSurfaceSeparation,
        float surfaceDistance)
    {
        DoubleVector3 direction = relativeCenter.Negate().Normalized();
        if (direction.Magnitude() <= double.Epsilon)
        {
            direction = new DoubleVector3(0, 1, 0);
        }

        DoubleVector3 right = new DoubleVector3(0, 1, 0).Cross(direction);
        if (right.Magnitude() <= double.Epsilon)
        {
            right = new DoubleVector3(0, 0, 1).Cross(direction);
        }

        right = right.Normalized();
        DoubleVector3 forward = right.Cross(direction).Normalized();
        var context = new AdaptiveBuildContext
        {
            relativeCenter = relativeCenter,
            surfacePoint = relativeCenter + direction * renderedPlanetRadius,
            right = right,
            forward = forward,
            renderedPlanetRadius = renderedPlanetRadius,
            simulationPlanetRadius = simulationPlanetRadius,
            renderedPatchSize = renderedPatchSize,
            simulationPatchSize = simulationPatchSize,
            renderedElevationStrength = renderedElevationStrength,
            renderedSurfaceSeparation = renderedSurfaceSeparation,
            surfaceDistance = Mathf.Max(0f, surfaceDistance),
            gridSize = 1 << maximumSubdivisionLevel,
            surfaceOrigin = GetSurfaceCoordinatesInMeters(direction, simulationPlanetRadius)
        };

        if (uvMappingMode == UVMappingMode.Planar)
        {
            UpdateMaterialOffset(context.surfaceOrigin);
        }

        adaptiveLeaves.Clear();
        adaptiveSamples.Clear();
        adaptiveVertexIndices.Clear();
        horizontalBoundaries.Clear();
        verticalBoundaries.Clear();
        adaptiveVertices.Clear();
        adaptiveUVs.Clear();
        adaptiveTriangles.Clear();

        int remainingLeafBudget = Mathf.Max(0, maximumLeafCount - 1);
        BuildAdaptiveLeaves(
            ref context,
            new QuadLeaf(0, 0, context.gridSize, 0),
            ref remainingLeafBudget);

        BuildBoundaryLookup();
        for (int i = 0; i < adaptiveLeaves.Count; i++)
        {
            TriangulateAdaptiveLeaf(ref context, adaptiveLeaves[i]);
        }

        patchMesh.Clear();
        patchMesh.indexFormat = adaptiveVertices.Count > ushort.MaxValue
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;
        patchMesh.SetVertices(adaptiveVertices);
        patchMesh.SetUVs(0, adaptiveUVs);
        patchMesh.SetTriangles(adaptiveTriangles, 0, true);
        patchMesh.RecalculateNormals();
        patchMesh.RecalculateTangents();
        patchMesh.RecalculateBounds();

        currentLeafCount = adaptiveLeaves.Count;
        UpdateColliderBounds();
    }

    private void BuildAdaptiveLeaves(
        ref AdaptiveBuildContext context,
        QuadLeaf node,
        ref int remainingLeafBudget)
    {
        if (ShouldSplitLeaf(ref context, node) && remainingLeafBudget >= 3)
        {
            remainingLeafBudget -= 3;
            int half = node.size / 2;
            int nextDepth = node.depth + 1;
            BuildAdaptiveLeaves(ref context, new QuadLeaf(node.x, node.y, half, nextDepth), ref remainingLeafBudget);
            BuildAdaptiveLeaves(ref context, new QuadLeaf(node.x, node.y + half, half, nextDepth), ref remainingLeafBudget);
            BuildAdaptiveLeaves(ref context, new QuadLeaf(node.x + half, node.y + half, half, nextDepth), ref remainingLeafBudget);
            BuildAdaptiveLeaves(ref context, new QuadLeaf(node.x + half, node.y, half, nextDepth), ref remainingLeafBudget);
            return;
        }

        adaptiveLeaves.Add(node);
    }

    private bool ShouldSplitLeaf(ref AdaptiveBuildContext context, QuadLeaf node)
    {
        if (node.depth < minimumSubdivisionLevel)
        {
            return true;
        }

        if (node.depth >= maximumSubdivisionLevel || node.size <= 1)
        {
            return false;
        }

        int half = node.size / 2;
        TerrainSample bottomLeft = GetAdaptiveSample(ref context, node.x, node.y);
        TerrainSample topLeft = GetAdaptiveSample(ref context, node.x, node.y + node.size);
        TerrainSample topRight = GetAdaptiveSample(ref context, node.x + node.size, node.y + node.size);
        TerrainSample bottomRight = GetAdaptiveSample(ref context, node.x + node.size, node.y);
        TerrainSample center = GetAdaptiveSample(ref context, node.x + half, node.y + half);
        TerrainSample left = GetAdaptiveSample(ref context, node.x, node.y + half);
        TerrainSample top = GetAdaptiveSample(ref context, node.x + half, node.y + node.size);
        TerrainSample right = GetAdaptiveSample(ref context, node.x + node.size, node.y + half);
        TerrainSample bottom = GetAdaptiveSample(ref context, node.x + half, node.y);

        float minimum = Mathf.Min(
            Mathf.Min(bottomLeft.simulationElevation, topLeft.simulationElevation),
            Mathf.Min(topRight.simulationElevation, bottomRight.simulationElevation));
        float maximum = Mathf.Max(
            Mathf.Max(bottomLeft.simulationElevation, topLeft.simulationElevation),
            Mathf.Max(topRight.simulationElevation, bottomRight.simulationElevation));
        minimum = Mathf.Min(minimum, Mathf.Min(center.simulationElevation,
            Mathf.Min(Mathf.Min(left.simulationElevation, top.simulationElevation),
                Mathf.Min(right.simulationElevation, bottom.simulationElevation))));
        maximum = Mathf.Max(maximum, Mathf.Max(center.simulationElevation,
            Mathf.Max(Mathf.Max(left.simulationElevation, top.simulationElevation),
                Mathf.Max(right.simulationElevation, bottom.simulationElevation))));

        float cornerAverage = 0.25f * (
            bottomLeft.simulationElevation + topLeft.simulationElevation +
            topRight.simulationElevation + bottomRight.simulationElevation);
        float edgeAverage = 0.25f * (
            left.simulationElevation + top.simulationElevation +
            right.simulationElevation + bottom.simulationElevation);
        float curvature = Mathf.Max(
            Mathf.Abs(center.simulationElevation - cornerAverage),
            Mathf.Abs(center.simulationElevation - edgeAverage));
        float heightVariation = maximum - minimum + curvature * 2f;
        float roughness = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(flatHeightVariation, roughHeightVariation, heightVariation));

        float cellSize = context.simulationPatchSize * node.size / context.gridSize;
        float centerX = ((node.x + half) / (float)context.gridSize - 0.5f) * context.simulationPatchSize;
        float centerY = ((node.y + half) / (float)context.gridSize - 0.5f) * context.simulationPatchSize;
        float cameraDistance = Mathf.Sqrt(
            centerX * centerX + centerY * centerY +
            context.surfaceDistance * context.surfaceDistance);
        float distanceFactor = Mathf.SmoothStep(
            0f,
            1f,
            cameraDistance / Mathf.Max(1f, highDetailDistance));

        float nearTargetSize = Mathf.Lerp(flatTerrainCellSize, roughTerrainCellSize, roughness);
        float targetSize = nearTargetSize * Mathf.Lerp(1f, distantCellSizeMultiplier, distanceFactor);
        return cellSize > Mathf.Max(1f, targetSize);
    }

    private TerrainSample GetAdaptiveSample(ref AdaptiveBuildContext context, int x, int y)
    {
        long key = GridKey(x, y);
        if (adaptiveSamples.TryGetValue(key, out TerrainSample sample))
        {
            return sample;
        }

        sample = EvaluateAdaptiveSample(
            ref context,
            x / (double)context.gridSize,
            y / (double)context.gridSize);
        adaptiveSamples.Add(key, sample);
        return sample;
    }

    private TerrainSample EvaluateAdaptiveSample(
        ref AdaptiveBuildContext context,
        double normalizedX,
        double normalizedY)
    {
        double offsetU = (normalizedX - 0.5) * context.renderedPatchSize;
        double offsetV = (normalizedY - 0.5) * context.renderedPatchSize;
        DoubleVector3 pointOnPlane = context.surfacePoint +
            context.right * offsetU + context.forward * offsetV;
        DoubleVector3 direction = (pointOnPlane - context.relativeCenter).Normalized();
        Vector2 sphericalUV = GetSphericalUV(direction);
        float simulationElevation = SampleElevation(sphericalUV, elevationStrength);
        float renderedElevation = SampleElevation(sphericalUV, context.renderedElevationStrength);
        DoubleVector3 pointOnSphere = context.relativeCenter + direction *
            (context.renderedPlanetRadius + context.renderedSurfaceSeparation + renderedElevation);

        Vector2 meshUV;
        if (uvMappingMode == UVMappingMode.Spherical)
        {
            meshUV = new Vector2(
                sphericalUV.x * uvScale.x + uvOffset.x,
                sphericalUV.y * uvScale.y + uvOffset.y);
        }
        else
        {
            SurfaceCoordinates surfaceMeters = GetSurfaceCoordinatesInMeters(
                direction,
                context.simulationPlanetRadius);
            double localU = surfaceMeters.x - context.surfaceOrigin.x;
            double longitudePeriod = 2.0 * Math.PI * context.simulationPlanetRadius;
            if (longitudePeriod > double.Epsilon)
            {
                localU -= Math.Round(localU / longitudePeriod) * longitudePeriod;
            }

            meshUV = new Vector2(
                (float)(localU * uvScale.x) + uvOffset.x,
                (float)((surfaceMeters.y - context.surfaceOrigin.y) * uvScale.y) + uvOffset.y);
        }

        return new TerrainSample((Vector3)pointOnSphere, meshUV, simulationElevation);
    }

    private void BuildBoundaryLookup()
    {
        for (int i = 0; i < adaptiveLeaves.Count; i++)
        {
            QuadLeaf leaf = adaptiveLeaves[i];
            AddBoundaryPoint(leaf.x, leaf.y);
            AddBoundaryPoint(leaf.x, leaf.y + leaf.size);
            AddBoundaryPoint(leaf.x + leaf.size, leaf.y + leaf.size);
            AddBoundaryPoint(leaf.x + leaf.size, leaf.y);
        }
    }

    private void AddBoundaryPoint(int x, int y)
    {
        if (!horizontalBoundaries.TryGetValue(y, out SortedSet<int> horizontal))
        {
            horizontal = new SortedSet<int>();
            horizontalBoundaries.Add(y, horizontal);
        }

        horizontal.Add(x);

        if (!verticalBoundaries.TryGetValue(x, out SortedSet<int> vertical))
        {
            vertical = new SortedSet<int>();
            verticalBoundaries.Add(x, vertical);
        }

        vertical.Add(y);
    }

    private void TriangulateAdaptiveLeaf(ref AdaptiveBuildContext context, QuadLeaf leaf)
    {
        leafBoundary.Clear();

        // Clockwise in tangent-plane coordinates, matching the original grid winding.
        AppendVerticalBoundary(leaf.x, leaf.y, leaf.y + leaf.size, true);
        AppendHorizontalBoundary(leaf.y + leaf.size, leaf.x, leaf.x + leaf.size, true);
        AppendVerticalBoundary(leaf.x + leaf.size, leaf.y, leaf.y + leaf.size, false);
        AppendHorizontalBoundary(leaf.y, leaf.x, leaf.x + leaf.size, false);

        if (leafBoundary.Count > 1 && leafBoundary[0] == leafBoundary[leafBoundary.Count - 1])
        {
            leafBoundary.RemoveAt(leafBoundary.Count - 1);
        }

        double centerX = (leaf.x + leaf.size * 0.5) / context.gridSize;
        double centerY = (leaf.y + leaf.size * 0.5) / context.gridSize;
        TerrainSample centerSample = EvaluateAdaptiveSample(ref context, centerX, centerY);
        int centerIndex = adaptiveVertices.Count;
        adaptiveVertices.Add(centerSample.position);
        adaptiveUVs.Add(centerSample.uv);

        for (int i = 0; i < leafBoundary.Count; i++)
        {
            int a = GetOrCreateAdaptiveVertex(ref context, leafBoundary[i]);
            int b = GetOrCreateAdaptiveVertex(
                ref context,
                leafBoundary[(i + 1) % leafBoundary.Count]);
            adaptiveTriangles.Add(centerIndex);
            adaptiveTriangles.Add(a);
            adaptiveTriangles.Add(b);
        }
    }

    private void AppendHorizontalBoundary(int y, int minimumX, int maximumX, bool ascending)
    {
        if (!horizontalBoundaries.TryGetValue(y, out SortedSet<int> values))
        {
            return;
        }

        if (ascending)
        {
            foreach (int x in values.GetViewBetween(minimumX, maximumX))
            {
                AppendBoundaryKey(GridKey(x, y));
            }
        }
        else
        {
            foreach (int x in values.GetViewBetween(minimumX, maximumX).Reverse())
            {
                AppendBoundaryKey(GridKey(x, y));
            }
        }
    }

    private void AppendVerticalBoundary(int x, int minimumY, int maximumY, bool ascending)
    {
        if (!verticalBoundaries.TryGetValue(x, out SortedSet<int> values))
        {
            return;
        }

        if (ascending)
        {
            foreach (int y in values.GetViewBetween(minimumY, maximumY))
            {
                AppendBoundaryKey(GridKey(x, y));
            }
        }
        else
        {
            foreach (int y in values.GetViewBetween(minimumY, maximumY).Reverse())
            {
                AppendBoundaryKey(GridKey(x, y));
            }
        }
    }

    private void AppendBoundaryKey(long key)
    {
        if (leafBoundary.Count == 0 || leafBoundary[leafBoundary.Count - 1] != key)
        {
            leafBoundary.Add(key);
        }
    }

    private int GetOrCreateAdaptiveVertex(ref AdaptiveBuildContext context, long key)
    {
        if (adaptiveVertexIndices.TryGetValue(key, out int index))
        {
            return index;
        }

        int x = (int)(key >> 32);
        int y = (int)(key & uint.MaxValue);
        TerrainSample sample = GetAdaptiveSample(ref context, x, y);
        index = adaptiveVertices.Count;
        adaptiveVertices.Add(sample.position);
        adaptiveUVs.Add(sample.uv);
        adaptiveVertexIndices.Add(key, index);
        return index;
    }

    private static long GridKey(int x, int y)
    {
        return ((long)x << 32) | (uint)y;
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
        if (patchIsVisible != visible)
        {
            exclusionRevision++;
        }

        patchIsVisible = visible;
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
        minimumSubdivisionLevel = Mathf.Clamp(minimumSubdivisionLevel, 1, 8);
        maximumSubdivisionLevel = Mathf.Clamp(
            maximumSubdivisionLevel,
            Mathf.Max(3, minimumSubdivisionLevel),
            12);
        flatTerrainCellSize = Mathf.Max(1f, flatTerrainCellSize);
        roughTerrainCellSize = Mathf.Clamp(roughTerrainCellSize, 1f, flatTerrainCellSize);
        flatHeightVariation = Mathf.Max(0f, flatHeightVariation);
        roughHeightVariation = Mathf.Max(flatHeightVariation + 0.01f, roughHeightVariation);
        highDetailDistance = Mathf.Max(1f, highDetailDistance);
        distantCellSizeMultiplier = Mathf.Clamp(distantCellSizeMultiplier, 1f, 12f);
        maximumLeafCount = Mathf.Clamp(maximumLeafCount, 256, 50000);
        minimumRebuildDistance = Mathf.Max(0f, minimumRebuildDistance);
        relativeRebuildThreshold = Mathf.Clamp(relativeRebuildThreshold, 0.001f, 0.25f);
        elevationStrength = Mathf.Max(0f, elevationStrength);
        groundClearance = Mathf.Max(0f, groundClearance);
        groundFriction = Mathf.Max(0f, groundFriction);
        groundedTolerance = Mathf.Max(0f, groundedTolerance);
        groundStopSpeed = Mathf.Max(0f, groundStopSpeed);
        coarsePlanetHideRange = Mathf.Max(0f, coarsePlanetHideRange);
        hasMeshAnchor = false;
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

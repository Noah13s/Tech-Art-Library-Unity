using UnityEngine;
using System.Collections.Generic;

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
    public UVMappingMode uvMappingMode = UVMappingMode.Spherical;
    public Vector2 uvScale = Vector2.one;
    public Vector2 uvOffset = Vector2.zero;
    public Material material;

    public new Collider collider;

    private MeshFilter meshFilter;
    private Mesh patchMesh;

    void Start()
    {
        // Get (or add) MeshFilter and MeshRenderer components
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (GetComponent<MeshRenderer>() == null)
            gameObject.AddComponent<MeshRenderer>();

        patchMesh = new Mesh();
        patchMesh.name = "Surface Patch Mesh";
        meshFilter.mesh = patchMesh;
    }

    void Update()
    {
        if (planet == null || player == null)
            return;

        // Use the surfaceDistance computed by the PerspectiveIllusionObject
        if (planet.surfaceDistance <= proximityRange)
        {
            // Compute the effective planet center in absolute coordinates.
            // planet.transform.position is relative to the player, so add player.playerPosition.
            Vector3 effectiveCenter = planet.transform.position + (Vector3)player.playerPosition;

            // Update material offset (if needed)
            DoubleVector3 offset = player.playerPosition - planet.simulationPosition;
            if (material!=null)
            {
                material.SetVector("_Offset", new Vector2((float)offset.x, (float)offset.y));
            }

            // Calculate patch size based on distance: up close, use maxPatchSize; far away, use minPatchSize.
            float patchSize = Mathf.Lerp(maxPatchSize, minPatchSize, (float)planet.surfaceDistance / proximityRange);


            GenerateSurfacePatch(effectiveCenter, patchSize);
        }
        else
        {
            if (patchMesh != null)
                patchMesh.Clear();
        }
    }

    private void FixedUpdate()
    {
        if (collider != null)
        {
            collider.transform.position = patchMesh.bounds.center;
            collider.transform.localScale = patchMesh.bounds.size;
        }
    }

    void GenerateSurfacePatch(Vector3 effectiveCenter, float patchSize)
    {
        // Convert effective center to DoubleVector3 for high-precision math.
        DoubleVector3 effectiveCenterDV = new DoubleVector3(effectiveCenter.x, effectiveCenter.y, effectiveCenter.z);
        DoubleVector3 playerPosDV = player.playerPosition;

        // Planet's radius (assuming planet.simulationScale represents the full diameter)
        double effectiveRadius = planet.transform.localScale.x * 0.5;
        // Direction from the planet's center to the player (in absolute coordinates)
        DoubleVector3 direction = (playerPosDV - effectiveCenterDV).Normalized();
        // Compute the point on the sphere closest to the player.
        DoubleVector3 surfacePoint = effectiveCenterDV + direction * effectiveRadius;

        // Build a tangent plane at surfacePoint.
        DoubleVector3 up = direction;
        DoubleVector3 right = new DoubleVector3(0, 1, 0).Cross(up);
        if (right.Magnitude() == 0)
            right = new DoubleVector3(0, 0, 1).Cross(up);
        right = right.Normalized();
        DoubleVector3 forward = right.Cross(up).Normalized();

        List<DoubleVector3> vertices = new List<DoubleVector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Create a grid in the tangent plane and project each point onto the sphere.
        for (int y = 0; y <= gridResolution; y++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                double tX = (double)x / gridResolution;
                double tY = (double)y / gridResolution;
                double offsetU = (tX - 0.5) * patchSize;
                double offsetV = (tY - 0.5) * patchSize;
                DoubleVector3 pointOnPlane = surfacePoint + right * offsetU + forward * offsetV;
                DoubleVector3 dirFromCenter = (pointOnPlane - effectiveCenterDV).Normalized();
                DoubleVector3 pointOnSphere = effectiveCenterDV + dirFromCenter * effectiveRadius;

                // --- Elevation from Height Map with Displacement Min/Max ---
                if (heightMap != null)
                {
                    // Compute spherical UV coordinates based on the planet's simulation center.
                    Vector3 normalForUV = ((Vector3)(pointOnSphere - planet.simulationPosition)).normalized;
                    float texU = Mathf.Atan2(normalForUV.z, normalForUV.x) / (2 * Mathf.PI) + 0.5f;
                    float texV = 1 - (Mathf.Acos(normalForUV.y) / Mathf.PI);

                    // Apply additional UV scale and offset.
                    texU = texU * heightMapUVScale.x + heightMapUVOffset.x;
                    texV = texV * heightMapUVScale.y + heightMapUVOffset.y;

                    // Sample height map (red channel)
                    float rawHeight = heightMap.GetPixelBilinear(texU, texV).r;
                    // Remap rawHeight using displacementMin and displacementMax.
                    float remappedHeight = Mathf.InverseLerp(displacementMin, displacementMax, rawHeight);
                    // Compute elevation (only positive displacement; ground level remains 0).
                    float elevation = remappedHeight * elevationStrength;
                    // Apply displacement along the vertex normal.
                    Vector3 displacement = normalForUV * elevation;
                    pointOnSphere = pointOnSphere + new DoubleVector3(displacement.x, displacement.y, displacement.z);
                }

                // --- Generate UVs for the Patch ---
                Vector2 uvCoord;
                if (uvMappingMode == UVMappingMode.Spherical)
                {
                    Vector3 uvNormal = ((Vector3)(pointOnSphere - planet.simulationPosition)).normalized;
                    float uvX = Mathf.Atan2(uvNormal.z, uvNormal.x) / (2 * Mathf.PI) + 0.5f;
                    float uvY = 1 - (Mathf.Acos(uvNormal.y) / Mathf.PI);
                    uvCoord = new Vector2(uvX, uvY);
                }
                else // Planar mapping
                {
                    float planarU = (float)(offsetU / patchSize + 0.5);
                    float planarV = (float)(offsetV / patchSize + 0.5);
                    uvCoord = new Vector2(planarU, planarV);
                }
                // Apply global UV tiling and offset.
                uvCoord = new Vector2(uvCoord.x * uvScale.x + uvOffset.x, uvCoord.y * uvScale.y + uvOffset.y);
                uvs.Add(uvCoord);

                // Make vertex relative to player (for high precision rendering)
                vertices.Add(pointOnSphere - playerPosDV);
            }
        }

        // Build triangles for the grid.
        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                int i = y * (gridResolution + 1) + x;
                triangles.Add(i);
                triangles.Add(i + gridResolution + 1);
                triangles.Add(i + 1);

                triangles.Add(i + 1);
                triangles.Add(i + gridResolution + 1);
                triangles.Add(i + gridResolution + 2);
            }
        }

        patchMesh.Clear();
        patchMesh.SetVertices(vertices.ConvertAll(v => (Vector3)v));
        patchMesh.SetTriangles(triangles, 0);
        patchMesh.SetUVs(0, uvs);
        patchMesh.RecalculateNormals();
    }
}

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
    public AnimationCurve heightMapRemapCurve = AnimationCurve.Linear(0, 0, 1, 1); // Remaps the raw height value

    public enum UVMappingMode { Spherical, Planar }
    [Header("UV Mapping Control")]
    public UVMappingMode uvMappingMode = UVMappingMode.Spherical;
    public Vector2 uvScale = Vector2.one;
    public Vector2 uvOffset = Vector2.zero;
    public Material material;

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

            DoubleVector3 offset = player.playerPosition - planet.simulationPosition;
            material.SetVector("_Offset", new Vector2((float)offset.x, (float)offset.y));

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

    void GenerateSurfacePatch(Vector3 effectiveCenter, float patchSize)
    {
        DoubleVector3 effectiveCenterDV = new DoubleVector3(effectiveCenter.x, effectiveCenter.y, effectiveCenter.z);
        DoubleVector3 playerPosDV = player.playerPosition;

        // Planet's radius (assuming simulationScale represents full diameter)
        double effectiveRadius = planet.transform.localScale.x * 0.5;
        DoubleVector3 direction = (playerPosDV - effectiveCenterDV).Normalized();
        DoubleVector3 surfacePoint = effectiveCenterDV + direction * effectiveRadius;

        // Build a tangent plane at surfacePoint
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

                // --- Elevation from Height Map with More Control ---
                if (heightMap != null)
                {
                    // Calculate spherical UV coordinates based on the planet's simulation center.
                    Vector3 normalForUV = ((Vector3)(pointOnSphere - planet.simulationPosition)).normalized;
                    float texU = Mathf.Atan2(normalForUV.z, normalForUV.x) / (2 * Mathf.PI) + 0.5f;
                    float texV = 1 - (Mathf.Acos(normalForUV.y) / Mathf.PI);

                    // Apply additional UV scale and offset.
                    texU = texU * heightMapUVScale.x + heightMapUVOffset.x;
                    texV = texV * heightMapUVScale.y + heightMapUVOffset.y;

                    // Sample height map (red channel)
                    float rawHeight = heightMap.GetPixelBilinear(texU, texV).r;
                    // Remap the raw height value via an AnimationCurve for finer control.
                    float remappedHeight = heightMapRemapCurve.Evaluate(rawHeight);
                    // Use the remapped height to compute the elevation.
                    float elevation = remappedHeight * elevationStrength;

                    // Apply elevation (only positive displacement).
                    Vector3 displacement = normalForUV * elevation;
                    pointOnSphere = pointOnSphere + new DoubleVector3(displacement.x, displacement.y, displacement.z);
                }

                // --- Generate UVs for the Patch with extra controls ---
                Vector2 uvCoord;
                if (uvMappingMode == UVMappingMode.Spherical)
                {
                    Vector3 uvNormal = ((Vector3)(pointOnSphere - planet.simulationPosition)).normalized;
                    float uvX = Mathf.Atan2(uvNormal.z, uvNormal.x) / (2 * Mathf.PI) + 0.5f;
                    float uvY = 1 - (Mathf.Acos(uvNormal.y) / Mathf.PI);
                    uvCoord = new Vector2(uvX, uvY);
                }
                {
                    // Calculate relative position to surfacePoint in the tangent plane.
                    // Map the offset values from [-patchSize/2, patchSize/2] to [0,1]
                    float planarU = (float)(offsetU / patchSize + 0.5);
                    float planarV = (float)(offsetV / patchSize + 0.5);
                    uvCoord = new Vector2(planarU, planarV);
                }
                // Apply the global tiling and offset parameters.
                uvCoord = new Vector2(uvCoord.x * uvScale.x + uvOffset.x, uvCoord.y * uvScale.y + uvOffset.y);
                uvs.Add(uvCoord);

                // Make vertex relative to player (for high precision rendering)
                vertices.Add(pointOnSphere - playerPosDV);
            }
        }

        // Build triangles for the grid
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

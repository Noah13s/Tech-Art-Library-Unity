using UnityEngine;
using System.Collections.Generic;

public class SphereSurfacePatchGenerator : MonoBehaviour
{
    [Header("References")]
    public PerspectiveIllusionObject planet;
    public FloatPrecisionPlayer player;
    public Texture2D heightMap;             // Height map texture
    public float elevationIntensity = 10f;  // Controls how much the height map affects the mesh

    [Header("Proximity Settings")]
    public float proximityRange = 50f;      // Range (from planet's surface) to generate the patch

    [Header("Mesh Settings")]
    public int gridResolution = 10;         // Number of segments for the patch
    public float minPatchSize = 10f;        // Smallest patch size (when far away)
    public float maxPatchSize = 100f;       // Largest patch size (up close)

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
        // Convert effective center to DoubleVector3
        DoubleVector3 effectiveCenterDV = new DoubleVector3(effectiveCenter.x, effectiveCenter.y, effectiveCenter.z);
        DoubleVector3 playerPosDV = player.playerPosition;

        // Use the planet's effective scale to determine its radius.
        // Assuming planet.simulationScale represents the full diameter, the radius is:
        double effectiveRadius = planet.transform.localScale.x * 0.5;

        // Direction from the planet's center to the player (in absolute coordinates)
        DoubleVector3 direction = (playerPosDV - effectiveCenterDV).Normalized();

        // Compute the surface point on the planet (the point closest to the player)
        DoubleVector3 surfacePoint = effectiveCenterDV + direction * effectiveRadius;

        // Build a tangent plane at the surfacePoint.
        // Use the direction from center to player as the 'up' vector.
        DoubleVector3 up = direction;
        // Compute a 'right' vector via cross product with an arbitrary vector.
        DoubleVector3 right = new DoubleVector3(0, 1, 0).Cross(up);
        if (right.Magnitude() == 0)
            right = new DoubleVector3(0, 0, 1).Cross(up);
        right = right.Normalized();
        DoubleVector3 forward = right.Cross(up).Normalized();

        List<DoubleVector3> vertices = new List<DoubleVector3>();
        List<int> triangles = new List<int>();

        // Create a grid in the tangent plane and project each point onto the sphere.
        for (int y = 0; y <= gridResolution; y++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                // Offsets in the plane, centered at 0
                double u = ((double)x / gridResolution - 0.5) * patchSize;
                double v = ((double)y / gridResolution - 0.5) * patchSize;

                // Point on the tangent plane
                DoubleVector3 pointOnPlane = surfacePoint + right * u + forward * v;

                // Project the point onto the sphere by normalizing the direction from planet center
                DoubleVector3 dirFromCenter = (pointOnPlane - effectiveCenterDV).Normalized();
                DoubleVector3 pointOnSphere = effectiveCenterDV + dirFromCenter * effectiveRadius;

                // --- Elevation from Height Map ---
                if (heightMap != null)
                {
                    // Calculate spherical UV coordinates from the normalized direction
                    // Use the planet's actual simulationPosition as the center for UV calculation.
                    Vector3 normalV3 = (Vector3)(pointOnSphere - planet.simulationPosition).Normalized();
                    float texU = Mathf.Atan2(normalV3.z, normalV3.x) / (2 * Mathf.PI) + 0.5f;
                    float texV = Mathf.Acos(normalV3.y) / Mathf.PI;
                    // Sample the height map (assuming height is stored in the red channel)
                    float elevation = heightMap.GetPixelBilinear(texU, texV).r * elevationIntensity;
                    // Displace the point along the normal.
                    Vector3 displacement = normalV3 * elevation;
                    pointOnSphere = pointOnSphere + new DoubleVector3(displacement.x, displacement.y, displacement.z);
                }

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

        // Update mesh (convert DoubleVector3 to Vector3)
        patchMesh.Clear();
        patchMesh.SetVertices(vertices.ConvertAll(v => (Vector3)v));
        patchMesh.SetTriangles(triangles, 0);
        patchMesh.RecalculateNormals();
    }
}

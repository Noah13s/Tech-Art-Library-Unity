using UnityEngine;
using System.Collections.Generic;

public class PlanetSurfaceGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform planetTransform;   // Planet's transform (position and scale)
    public Transform playerTransform;   // Player's transform

    [Header("Proximity Settings")]
    public float proximityRange = 50f;    // Range to detect proximity to planet's surface

    [Header("Mesh Settings")]
    public int gridResolution = 10;       // Number of segments for the patch
    public float minPatchSize = 10f;      // Smallest patch size (far away)
    public float maxPatchSize = 100f;     // Largest patch size (up close)

    private MeshFilter meshFilter;
    private Mesh patchMesh;

    void Start()
    {
        // Get or add a MeshFilter component on this GameObject
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Ensure there is a MeshRenderer
        if (GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }

        // Create a new mesh for the surface patch
        patchMesh = new Mesh();
        patchMesh.name = "Surface Patch Mesh";
        meshFilter.mesh = patchMesh;
    }

    void Update()
    {
        if (planetTransform == null || playerTransform == null)
            return;

        // Calculate the planet's radius from its scale (assuming it's a sphere)
        float planetRadius = planetTransform.lossyScale.x * 0.5f;
        Vector3 planetCenter = planetTransform.position;

        // Calculate the player's distance to the planet's center and surface
        float distToCenter = Vector3.Distance(playerTransform.position, planetCenter);
        float distanceToSurface = Mathf.Abs(distToCenter - planetRadius);

        // Check if the player is within proximity range
        if (distanceToSurface <= proximityRange)
        {
            float patchSize = CalculatePatchSize(distanceToSurface);
            GenerateSurfacePatch(patchSize);
        }
        else
        {
            // Clear the mesh if the player is out of range
            if (patchMesh != null)
            {
                patchMesh.Clear();
            }
        }
    }

    float CalculatePatchSize(float distanceToSurface)
    {
        // Scale patch size based on distance
        return Mathf.Lerp(maxPatchSize, minPatchSize, distanceToSurface / proximityRange);
    }

    void GenerateSurfacePatch(float patchSize)
    {
        // Direction from the planet's center to the player
        Vector3 direction = (playerTransform.position - planetTransform.position).normalized;
        // Find the closest point on the planet's surface
        Vector3 surfacePoint = planetTransform.position + direction * (planetTransform.lossyScale.x * 0.5f);

        // Build a local coordinate system on the patch:
        Vector3 up = direction;
        Vector3 right = Vector3.Cross(up, Vector3.up);
        if (right == Vector3.zero)
            right = Vector3.Cross(up, Vector3.forward);
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, up).normalized;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Create a grid in the tangent plane and project each point onto the sphere
        for (int y = 0; y <= gridResolution; y++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                // Compute offsets in the patch, centered around 0
                float u = ((float)x / gridResolution - 0.5f) * patchSize;
                float v = ((float)y / gridResolution - 0.5f) * patchSize;

                // Compute point in the tangent plane
                Vector3 pointOnPlane = surfacePoint + right * u + forward * v;

                // Project the point onto the sphere's surface
                Vector3 dirFromCenter = (pointOnPlane - planetTransform.position).normalized;
                Vector3 pointOnSphere = planetTransform.position + dirFromCenter * (planetTransform.lossyScale.x * 0.5f);

                vertices.Add(pointOnSphere);
            }
        }

        // Create triangles for the grid
        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                int i = y * (gridResolution + 1) + x;
                // First triangle
                triangles.Add(i);
                triangles.Add(i + gridResolution + 1);
                triangles.Add(i + 1);

                // Second triangle
                triangles.Add(i + 1);
                triangles.Add(i + gridResolution + 1);
                triangles.Add(i + gridResolution + 2);
            }
        }

        // Update the mesh
        patchMesh.Clear();
        patchMesh.SetVertices(vertices);
        patchMesh.SetTriangles(triangles, 0);
        patchMesh.RecalculateNormals();
    }
}

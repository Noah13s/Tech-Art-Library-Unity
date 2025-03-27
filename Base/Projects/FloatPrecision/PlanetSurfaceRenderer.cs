using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PlanetSurfacePatchRenderer : MonoBehaviour
{
    public PerspectiveIllusionObject perspectiveObject; // Parent that holds planet simulation info
    public FloatPrecisionPlayer player;                   // For retrieving player simulation position

    // Settings for the local patch mesh
    [SerializeField] private int gridResolution = 20;      // Number of vertices per side
    [SerializeField] private float patchAngularSize = 10f;   // Angular size in degrees of the patch

    private Mesh patchMesh;
    private MeshFilter meshFilter;

    // We'll regenerate the patch when the player is close enough
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        patchMesh = new Mesh();
        meshFilter.mesh = patchMesh;
        GeneratePatchMesh();
    }

    void Update()
    {
        // Compute the patch center: find the point on the planet's surface closest to the player.
        // Use the player's simulation position (converted to world space via PerspectiveIllusionObject)
        DoubleVector3 planetSimPos = perspectiveObject.simulationPosition;
        DoubleVector3 playerSimPos = player.playerPosition;
        DoubleVector3 toPlayer = playerSimPos - planetSimPos;
        // Get the direction and assume the planet is a sphere with radius based on current scale.
        float planetRadius = transform.localScale.x * 0.5f;
        Vector3 patchCenter = transform.position + ((Vector3)toPlayer.Normalized()) * planetRadius;

        // Now, in a local coordinate system centered at patchCenter,
        // the patch mesh’s vertex values are small, reducing floating‑point issues.
        // Here we reproject our patch vertices:
        Vector3[] vertices = patchMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Convert the vertex from local space to world space then back to patch-local space.
            Vector3 worldPos = transform.TransformPoint(vertices[i]);
            vertices[i] = worldPos - patchCenter;
        }
        patchMesh.vertices = vertices;
        patchMesh.RecalculateNormals();

        // Optionally, update patch orientation if the player moves significantly.
    }

    // Generates a simple grid mesh that approximates a patch of the planet's surface.
    void GeneratePatchMesh()
    {
        Vector3[] vertices = new Vector3[(gridResolution + 1) * (gridResolution + 1)];
        int[] triangles = new int[gridResolution * gridResolution * 6];

        // For simplicity, we map the patch to a tangent plane.
        // The patch covers an angular size, so we convert angular offsets (in radians) to a local grid.
        float halfAngle = patchAngularSize * 0.5f * Mathf.Deg2Rad;
        for (int y = 0; y <= gridResolution; y++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                float u = (float)x / gridResolution;
                float v = (float)y / gridResolution;
                // Map u,v to angular offsets in the tangent plane.
                float angleX = Mathf.Lerp(-halfAngle, halfAngle, u);
                float angleY = Mathf.Lerp(-halfAngle, halfAngle, v);

                // Use a simple approximation: for small angles, sin(angle) ~ angle.
                // Define the displacement on the tangent plane.
                Vector3 offset = new Vector3(Mathf.Sin(angleX), Mathf.Sin(angleY), 0);
                // Place vertices on a spherical surface patch.
                // For a sphere of radius R, a point offset by (angleX, angleY) on the tangent plane 
                // can be approximated by projecting onto the sphere:
                float R = transform.localScale.x * 0.5f;
                float z = Mathf.Sqrt(Mathf.Max(R * R - offset.x * offset.x - offset.y * offset.y, 0));
                vertices[y * (gridResolution + 1) + x] = new Vector3(offset.x, offset.y, z);
            }
        }

        // Create triangles for the grid.
        int triIndex = 0;
        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                int index = y * (gridResolution + 1) + x;
                triangles[triIndex++] = index;
                triangles[triIndex++] = index + gridResolution + 1;
                triangles[triIndex++] = index + 1;

                triangles[triIndex++] = index + 1;
                triangles[triIndex++] = index + gridResolution + 1;
                triangles[triIndex++] = index + gridResolution + 2;
            }
        }

        patchMesh.vertices = vertices;
        patchMesh.triangles = triangles;
        patchMesh.RecalculateNormals();
    }
}

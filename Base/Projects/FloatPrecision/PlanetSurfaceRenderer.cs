using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PlanetSurfaceRenderer : MonoBehaviour
{
    public PerspectiveIllusionObject perspectiveObject;
    public double closeUpStartRange = 5000f;
    public FloatPrecisionPlayer player;

    [SerializeField] private Transform closeUpTarget; // GameObject at (0,0,0) with local mesh
    [SerializeField] private float detailAngleThreshold = 0.5f;

    private Mesh originalMesh;
    private Mesh localMesh;
    private MeshFilter closeUpMeshFilter;
    private bool meshGenerated = false;
    private DoubleVector3 referencePlayerPosition;

    void Start()
    {
        if (closeUpTarget == null)
        {
            Debug.LogError("PlanetSurfaceRenderer: closeUpTarget is not assigned!");
            return;
        }

        // Get the mesh filter of the close-up object
        closeUpMeshFilter = closeUpTarget.GetComponent<MeshFilter>();
        if (closeUpMeshFilter == null)
        {
            Debug.LogError("PlanetSurfaceRenderer: closeUpTarget must have a MeshFilter!");
            return;
        }

        // Store the original planet mesh
        originalMesh = GetComponent<MeshFilter>().sharedMesh;

        // Create a new mesh for close-up rendering
        localMesh = new Mesh();
        closeUpMeshFilter.mesh = localMesh;
    }

    void Update()
    {
        if (perspectiveObject == null || closeUpTarget == null)
            return;


        if (perspectiveObject.surfaceDistance < closeUpStartRange)
        {
            if (!meshGenerated)
            {
                GenerateCloseUpMesh();
                meshGenerated = true;
            }

        }
        // Move the close-up mesh to follow the player
        DoubleVector3 relativePosition = (referencePlayerPosition - player.playerPosition);
        Vector3 playerLocalPos = (Vector3)relativePosition;
        closeUpTarget.position = playerLocalPos * 0.01f;
    }

    private void GenerateCloseUpMesh()
    {
        Vector3 playerLocalPos = transform.InverseTransformPoint(Vector3.zero);
        Vector3 directionToPlayer = playerLocalPos.normalized;

        // Get original mesh data
        Vector3[] vertices = originalMesh.vertices;
        Vector3[] modifiedVertices = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexDir = vertices[i].normalized;
            float dot = Vector3.Dot(vertexDir, directionToPlayer);

            if (dot > detailAngleThreshold)
            {
                // Convert the original planet mesh coordinates into the new local space.
                modifiedVertices[i] = vertices[i] - playerLocalPos;
            }
            else
            {
                // Hide the vertex (collapse it to center to remove unwanted areas)
                modifiedVertices[i] = Vector3.zero;
            }
        }

        // Apply the modified vertices to the close-up mesh
        localMesh.vertices = modifiedVertices;
        localMesh.triangles = originalMesh.triangles; // Keep the original connectivity
        localMesh.normals = originalMesh.normals;
        localMesh.RecalculateNormals();
        closeUpTarget.localScale = transform.localScale;

        referencePlayerPosition = player.playerPosition;
    }
}

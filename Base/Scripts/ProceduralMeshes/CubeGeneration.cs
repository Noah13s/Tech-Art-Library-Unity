using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeGeneration : MonoBehaviour
{
    [Header("Cube Dimensions")]
    public float length = 1f; // Front to back
    public float width = 1f;  // Left to right
    public float height = 1f; // Bottom to top


    private void OnValidate()
    {
        GenerateCube();
    }

    void GenerateCube()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        // Vertices (each face has its own set of vertices for hard edges)
        Vector3[] vertices = new Vector3[]
        {
            // Bottom face
            new Vector3(-width / 2, -height / 2, -length / 2), // 0
            new Vector3(width / 2, -height / 2, -length / 2),  // 1
            new Vector3(width / 2, -height / 2, length / 2),   // 2
            new Vector3(-width / 2, -height / 2, length / 2),  // 3

            // Top face
            new Vector3(-width / 2, height / 2, -length / 2),  // 4
            new Vector3(width / 2, height / 2, -length / 2),   // 5
            new Vector3(width / 2, height / 2, length / 2),    // 6
            new Vector3(-width / 2, height / 2, length / 2),   // 7

            // Front face
            new Vector3(-width / 2, -height / 2, length / 2),  // 8
            new Vector3(width / 2, -height / 2, length / 2),   // 9
            new Vector3(width / 2, height / 2, length / 2),    // 10
            new Vector3(-width / 2, height / 2, length / 2),   // 11

            // Back face
            new Vector3(width / 2, -height / 2, -length / 2),  // 12
            new Vector3(-width / 2, -height / 2, -length / 2), // 13
            new Vector3(-width / 2, height / 2, -length / 2),  // 14
            new Vector3(width / 2, height / 2, -length / 2),   // 15

            // Left face
            new Vector3(-width / 2, -height / 2, -length / 2), // 16
            new Vector3(-width / 2, -height / 2, length / 2),  // 17
            new Vector3(-width / 2, height / 2, length / 2),   // 18
            new Vector3(-width / 2, height / 2, -length / 2),  // 19

            // Right face
            new Vector3(width / 2, -height / 2, length / 2),   // 20
            new Vector3(width / 2, -height / 2, -length / 2),  // 21
            new Vector3(width / 2, height / 2, -length / 2),   // 22
            new Vector3(width / 2, height / 2, length / 2),    // 23
        };

        // Triangles
        int[] triangles = new int[]
        {
            // Bottom face
            0, 1, 2,
            0, 2, 3,

            // Top face
            4, 6, 5,
            4, 7, 6,

            // Front face
            8, 9, 10,
            8, 10, 11,

            // Back face
            12, 13, 14,
            12, 14, 15,

            // Left face
            16, 17, 18,
            16, 18, 19,

            // Right face
            20, 21, 22,
            20, 22, 23
        };

        // Normals (flat shading, one normal per face)
        Vector3[] normals = new Vector3[]
        {
            // Bottom face
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,

            // Top face
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,

            // Front face
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,

            // Back face
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,

            // Left face
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,

            // Right face
            Vector3.right, Vector3.right, Vector3.right, Vector3.right
        };

        // UVs (optional, useful for textures)
        Vector2[] uvs = new Vector2[]
        {
            // Bottom face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),

            // Top face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),

            // Front face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),

            // Back face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),

            // Left face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),

            // Right face
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        meshFilter.mesh = mesh;
    }


    void ClearChildren()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = null;
    }

    // Custom Editor for the script
    [CustomEditor(typeof(CubeGeneration))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default Inspector
            DrawDefaultInspector();

            // Get reference to the script
            CubeGeneration script = (CubeGeneration)target;

            // Add a custom button
            if (GUILayout.Button("Generate Mesh"))
            {
                script.GenerateCube();
            }

            if (GUILayout.Button("Clear"))
            {
                script.ClearChildren();
            }
        }
    }
}

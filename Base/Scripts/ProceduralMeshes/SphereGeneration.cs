using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class SphereGeneration : MonoBehaviour
{
    [Header("Sphere Parameters")]
    public float diameter = 2f; // Total diameter of the sphere
    public int nbRings = 16;    // Number of horizontal rings
    public int nbSegments = 16; // Number of vertical segments


    private void OnValidate()
    {
        GenerateSphere();
    }

    void GenerateSphere()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        float radius = diameter / 2f; // Calculate radius from diameter

        // Vertices and UVs
        Vector3[] vertices = new Vector3[(nbRings + 1) * (nbSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];

        int vertIndex = 0;
        for (int ring = 0; ring <= nbRings; ring++)
        {
            float phi = Mathf.PI * (float)ring / nbRings; // Angle from top to bottom (latitude)

            for (int segment = 0; segment <= nbSegments; segment++)
            {
                float theta = 2f * Mathf.PI * (float)segment / nbSegments; // Angle around the sphere (longitude)

                // Spherical coordinates
                float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = radius * Mathf.Cos(phi);
                float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);

                vertices[vertIndex] = new Vector3(x, y, z);
                uvs[vertIndex] = new Vector2((float)segment / nbSegments, (float)ring / nbRings);

                vertIndex++;
            }
        }

        // Triangles (reversed winding order for correct normals)
        int[] triangles = new int[nbRings * nbSegments * 6];
        int triIndex = 0;

        for (int ring = 0; ring < nbRings; ring++)
        {
            for (int segment = 0; segment < nbSegments; segment++)
            {
                int current = ring * (nbSegments + 1) + segment;
                int next = current + nbSegments + 1;

                // First triangle (reversed order)
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
                triangles[triIndex++] = current + 1;

                // Second triangle (reversed order)
                triangles[triIndex++] = next + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;
            }
        }

        // Assign mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals(); // Calculate normals for shading

        meshFilter.mesh = mesh;
    }

    void ClearChildren()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = null;
    }

    // Custom Editor for the script
    [CustomEditor(typeof(SphereGeneration))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default Inspector
            DrawDefaultInspector();

            // Get reference to the script
            SphereGeneration script = (SphereGeneration)target;

            // Add a custom button
            if (GUILayout.Button("Generate Mesh"))
            {
                script.GenerateSphere();
            }

            if (GUILayout.Button("Clear"))
            {
                script.ClearChildren();
            }
        }
    }
}

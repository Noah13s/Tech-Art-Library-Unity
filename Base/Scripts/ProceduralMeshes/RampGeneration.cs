using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class RampGeneration : MonoBehaviour
{
    [Header("Ramp Parameters")]
    public float length = 5f;  // Length of the ramp
    public float width = 2f;   // Width of the ramp
    public float height = 3f;  // Maximum height of the ramp
    public float curvature = 0.5f; // Curvature factor (0 = flat, >0 = more curved)
    public float wallHeight = 3f; // Height of the walls

    private void OnValidate()
    {
        GenerateRamp();
    }

    void GenerateRamp()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        int segments = 20; // Number of segments along the ramp for curvature resolution

        // Vertices for the ramp
        Vector3[] rampVertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[rampVertices.Length];
        int vertIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; // Interpolation factor
            float x = t * length;
            float z = -width / 2;

            // Curved height (parabolic curve)
            float y = height * Mathf.Pow(t, curvature);

            // Left edge vertex
            rampVertices[vertIndex] = new Vector3(x, y, z);
            uvs[vertIndex] = new Vector2(t, 0);
            vertIndex++;

            // Right edge vertex
            z = width / 2;
            rampVertices[vertIndex] = new Vector3(x, y, z);
            uvs[vertIndex] = new Vector2(t, 1);
            vertIndex++;
        }

        // Triangles for the ramp
        int[] rampTriangles = new int[segments * 6];
        int triIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int start = i * 2;

            // First triangle for the ramp (counterclockwise winding)
            rampTriangles[triIndex++] = start;
            rampTriangles[triIndex++] = start + 1;
            rampTriangles[triIndex++] = start + 2;

            // Second triangle for the ramp (counterclockwise winding)
            rampTriangles[triIndex++] = start + 2;
            rampTriangles[triIndex++] = start + 1;
            rampTriangles[triIndex++] = start + 3;
        }

        // Create mesh for the ramp
        mesh.vertices = rampVertices;
        mesh.triangles = rampTriangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        // Now, add wall meshes to the same mesh
        AppendLeftWallMesh(ref mesh); // Left wall
        AppendRightWallMesh(ref mesh);  // Right wall

        // Add back wall
        AppendBackWall(ref mesh);

        // Add bottom wall
        AppendBottomWall(ref mesh);

        // Assign the final mesh to the MeshFilter
        meshFilter.mesh = mesh;
    }

    // Function to generate the left wall and append to the ramp mesh
    void AppendLeftWallMesh(ref Mesh mesh)
    {
        int segments = 20; // Same number of segments as the ramp
        int startVertIndex = mesh.vertices.Length;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[vertices.Length];
        int vertIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = t * length;
            float y = height * Mathf.Pow(t, curvature);

            // Left wall vertices
            vertices[vertIndex] = new Vector3(x, 0, -width / 2); // Bottom of the wall
            uvs[vertIndex] = new Vector2(t, 0);
            vertIndex++;

            vertices[vertIndex] = new Vector3(x, y, -width / 2); // Top of the wall
            uvs[vertIndex] = new Vector2(t, 1);
            vertIndex++;
        }

        // Adding the triangles for the left wall (counterclockwise winding)
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int start = i * 2;

            // First triangle for the left wall (counterclockwise winding)
            triangles[triIndex++] = start + startVertIndex;
            triangles[triIndex++] = start + 1 + startVertIndex;
            triangles[triIndex++] = start + 2 + startVertIndex;

            // Second triangle for the left wall (counterclockwise winding)
            triangles[triIndex++] = start + 2 + startVertIndex;
            triangles[triIndex++] = start + 1 + startVertIndex;
            triangles[triIndex++] = start + 3 + startVertIndex;
        }

        // Append the left wall mesh data to the ramp's mesh
        AppendWallMesh(ref mesh, vertices, triangles, uvs);
    }

    // Function to generate the right wall and append to the ramp mesh
    void AppendRightWallMesh(ref Mesh mesh)
    {
        int segments = 20; // Same number of segments as the ramp
        int startVertIndex = mesh.vertices.Length;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[vertices.Length];
        int vertIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float x = t * length;
            float y = height * Mathf.Pow(t, curvature);

            // Right wall vertices
            vertices[vertIndex] = new Vector3(x, 0, width / 2); // Bottom of the wall
            uvs[vertIndex] = new Vector2(t, 0);
            vertIndex++;

            vertices[vertIndex] = new Vector3(x, y, width / 2); // Top of the wall
            uvs[vertIndex] = new Vector2(t, 1);
            vertIndex++;
        }

        // Adding the triangles for the right wall (clockwise winding)
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int start = i * 2;

            // First triangle for the right wall (clockwise winding)
            triangles[triIndex++] = start + startVertIndex;
            triangles[triIndex++] = start + 2 + startVertIndex;
            triangles[triIndex++] = start + 1 + startVertIndex;

            // Second triangle for the right wall (clockwise winding)
            triangles[triIndex++] = start + 1 + startVertIndex;
            triangles[triIndex++] = start + 2 + startVertIndex;
            triangles[triIndex++] = start + 3 + startVertIndex;
        }

        // Append the right wall mesh data to the ramp's mesh
        AppendWallMesh(ref mesh, vertices, triangles, uvs);
    }

    // Function to append wall mesh data to the ramp's mesh
    void AppendWallMesh(ref Mesh mesh, Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        Vector3[] newVertices = new Vector3[mesh.vertices.Length + vertices.Length];
        System.Array.Copy(mesh.vertices, newVertices, mesh.vertices.Length);
        System.Array.Copy(vertices, 0, newVertices, mesh.vertices.Length, vertices.Length);
        mesh.vertices = newVertices;

        int[] newTriangles = new int[mesh.triangles.Length + triangles.Length];
        System.Array.Copy(mesh.triangles, newTriangles, mesh.triangles.Length);
        System.Array.Copy(triangles, 0, newTriangles, mesh.triangles.Length, triangles.Length);
        mesh.triangles = newTriangles;

        Vector2[] newUVs = new Vector2[mesh.uv.Length + uvs.Length];
        System.Array.Copy(mesh.uv, newUVs, mesh.uv.Length);
        System.Array.Copy(uvs, 0, newUVs, mesh.uv.Length, uvs.Length);
        mesh.uv = newUVs;

        mesh.RecalculateNormals(); // Recalculate normals after appending
    }

    // Function to generate the back wall and append to the ramp mesh
    void AppendBackWall(ref Mesh mesh)
    {
        int segments = 2; // Only two vertices for the back wall (a simple vertical rectangle)
        int startVertIndex = mesh.vertices.Length;
        Vector3[] vertices = new Vector3[segments * 2]; // Two vertices for each side of the wall
        int[] triangles = new int[2 * 3]; // Two triangles for the back wall
        Vector2[] uvs = new Vector2[vertices.Length];
        int vertIndex = 0;

        // Back wall vertices (fixed at the end of the ramp along the X-axis)
        float x = length; // The back wall is at the end of the ramp along the X-axis

        // Bottom of the back wall (spanning the full width at Y = 0)
        vertices[vertIndex] = new Vector3(x, 0, -width / 2); // Bottom-left corner
        uvs[vertIndex] = new Vector2(0, 0);
        vertIndex++;

        vertices[vertIndex] = new Vector3(x, 0, width / 2); // Bottom-right corner
        uvs[vertIndex] = new Vector2(1, 0);
        vertIndex++;

        // Top of the back wall (spanning the full width at Y = height)
        vertices[vertIndex] = new Vector3(x, height, -width / 2); // Top-left corner
        uvs[vertIndex] = new Vector2(0, 1);
        vertIndex++;

        vertices[vertIndex] = new Vector3(x, height, width / 2); // Top-right corner
        uvs[vertIndex] = new Vector2(1, 1);
        vertIndex++;

        // Adding the triangles for the back wall (counterclockwise winding)
        int triIndex = 0;

        // First triangle for the back wall (counterclockwise winding)
        triangles[triIndex++] = startVertIndex;        // Bottom-left
        triangles[triIndex++] = startVertIndex + 2;    // Top-left
        triangles[triIndex++] = startVertIndex + 1;    // Bottom-right

        // Second triangle for the back wall (counterclockwise winding)
        triangles[triIndex++] = startVertIndex + 1;    // Bottom-right
        triangles[triIndex++] = startVertIndex + 2;    // Top-left
        triangles[triIndex++] = startVertIndex + 3;    // Top-right

        // Append the back wall mesh data to the ramp's mesh
        AppendWallMesh(ref mesh, vertices, triangles, uvs);
    }


    // Function to generate the bottom wall and append to the ramp mesh
    void AppendBottomWall(ref Mesh mesh)
    {
        int segments = 2; // Two vertices for the bottom wall (a simple horizontal rectangle)
        int startVertIndex = mesh.vertices.Length;
        Vector3[] vertices = new Vector3[segments * 2]; // Two vertices for each side of the wall
        int[] triangles = new int[2 * 3]; // Two triangles for the bottom wall
        Vector2[] uvs = new Vector2[vertices.Length];
        int vertIndex = 0;

        // Bottom wall vertices (spanning the full length at Z = 0, across the width of the ramp)
        float y = 0; // The bottom wall sits at y = 0 (the base of the ramp)

        // Left side of the bottom wall
        vertices[vertIndex] = new Vector3(0, y, -width / 2);
        uvs[vertIndex] = new Vector2(0, 0);
        vertIndex++;

        // Right side of the bottom wall
        vertices[vertIndex] = new Vector3(length, y, -width / 2);
        uvs[vertIndex] = new Vector2(1, 0);
        vertIndex++;

        // Right side of the bottom wall (opposite side)
        vertices[vertIndex] = new Vector3(0, y, width / 2);
        uvs[vertIndex] = new Vector2(0, 1);
        vertIndex++;

        // Left side of the bottom wall (opposite side)
        vertices[vertIndex] = new Vector3(length, y, width / 2);
        uvs[vertIndex] = new Vector2(1, 1);
        vertIndex++;

        // Adding the triangles for the bottom wall (counterclockwise winding)
        int triIndex = 0;

        // First triangle for the bottom wall (counterclockwise winding)
        triangles[triIndex++] = startVertIndex;        // Bottom-left
        triangles[triIndex++] = startVertIndex + 1;    // Bottom-right
        triangles[triIndex++] = startVertIndex + 2;    // Top-left

        // Second triangle for the bottom wall (counterclockwise winding)
        triangles[triIndex++] = startVertIndex + 2;    // Top-left
        triangles[triIndex++] = startVertIndex + 1;    // Bottom-right
        triangles[triIndex++] = startVertIndex + 3;    // Top-right

        // Append the bottom wall mesh data to the ramp's mesh
        AppendWallMesh(ref mesh, vertices, triangles, uvs);
    }


    void ClearChildren()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }
}

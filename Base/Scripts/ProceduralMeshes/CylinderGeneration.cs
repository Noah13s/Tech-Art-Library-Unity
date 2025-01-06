using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CylinderGeneration : MonoBehaviour
{
    [Header("Cylinder Dimensions")]
    public float radius = 0.5f;
    public float height = 1f;
    public int segments = 32;

    private void OnValidate()
    {
        GenerateCylinder();
    }

    void GenerateCylinder()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        int verticesPerSegment = 6; // 2 for bottom, 2 for top, 2 for side
        int vertexCount = segments * verticesPerSegment + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];

        // Center points for caps
        vertices[vertexCount - 2] = new Vector3(0, -height / 2, 0);
        vertices[vertexCount - 1] = new Vector3(0, height / 2, 0);
        uvs[vertexCount - 2] = new Vector2(0.5f, 0.5f);
        uvs[vertexCount - 1] = new Vector2(0.5f, 0.5f);
        normals[vertexCount - 2] = Vector3.down;
        normals[vertexCount - 1] = Vector3.up;

        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            float nextAngle = 2 * Mathf.PI * (i + 1) / segments;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float nextX = Mathf.Cos(nextAngle) * radius;
            float nextZ = Mathf.Sin(nextAngle) * radius;

            int baseIndex = i * verticesPerSegment;
            Vector3 sideNormal = new Vector3(x, 0, z).normalized;
            Vector3 nextSideNormal = new Vector3(nextX, 0, nextZ).normalized;

            // Bottom vertices
            vertices[baseIndex] = new Vector3(x, -height / 2, z);
            vertices[baseIndex + 1] = new Vector3(nextX, -height / 2, nextZ);
            normals[baseIndex] = Vector3.down;
            normals[baseIndex + 1] = Vector3.down;

            // Top vertices
            vertices[baseIndex + 2] = new Vector3(x, height / 2, z);
            vertices[baseIndex + 3] = new Vector3(nextX, height / 2, nextZ);
            normals[baseIndex + 2] = Vector3.up;
            normals[baseIndex + 3] = Vector3.up;

            // Side vertices
            vertices[baseIndex + 4] = new Vector3(x, -height / 2, z);
            vertices[baseIndex + 5] = new Vector3(x, height / 2, z);
            normals[baseIndex + 4] = sideNormal;
            normals[baseIndex + 5] = sideNormal;

            // UVs
            float uStart = (float)i / segments;
            float uEnd = (float)(i + 1) / segments;
            uvs[baseIndex] = new Vector2(uStart, 0);
            uvs[baseIndex + 1] = new Vector2(uEnd, 0);
            uvs[baseIndex + 2] = new Vector2(uStart, 1);
            uvs[baseIndex + 3] = new Vector2(uEnd, 1);
            uvs[baseIndex + 4] = new Vector2(uStart, 0);
            uvs[baseIndex + 5] = new Vector2(uStart, 1);
        }

        int[] triangles = new int[segments * 12];
        int triangleIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * verticesPerSegment;
            int nextBaseIndex = ((i + 1) % segments) * verticesPerSegment;

            // Bottom cap
            triangles[triangleIndex++] = vertexCount - 2;
            triangles[triangleIndex++] = baseIndex;
            triangles[triangleIndex++] = baseIndex + 1;

            // Top cap
            triangles[triangleIndex++] = vertexCount - 1;
            triangles[triangleIndex++] = baseIndex + 3;
            triangles[triangleIndex++] = baseIndex + 2;

            // Side faces
            triangles[triangleIndex++] = baseIndex + 4;
            triangles[triangleIndex++] = baseIndex + 5;
            triangles[triangleIndex++] = nextBaseIndex + 4;

            triangles[triangleIndex++] = nextBaseIndex + 4;
            triangles[triangleIndex++] = baseIndex + 5;
            triangles[triangleIndex++] = nextBaseIndex + 5;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        meshFilter.mesh = mesh;
    }

    void ClearMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = null;
    }

    [CustomEditor(typeof(CylinderGeneration))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CylinderGeneration script = (CylinderGeneration)target;

            if (GUILayout.Button("Generate Mesh"))
            {
                script.GenerateCylinder();
            }

            if (GUILayout.Button("Clear"))
            {
                script.ClearMesh();
            }
        }
    }
}
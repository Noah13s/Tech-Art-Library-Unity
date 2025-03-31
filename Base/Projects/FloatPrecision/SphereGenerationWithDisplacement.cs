using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphereGenerationWithDisplacement : MonoBehaviour
{
    [Header("Sphere Parameters")]
    public float diameter = 2f; // Total diameter of the sphere
    public int nbRings = 16;    // Number of horizontal rings
    public int nbSegments = 16; // Number of vertical segments

    [Header("Height Map Displacement")]
    public Texture2D heightMap;         // Height map texture (should be grayscale)
    public float displacementStrength = 0.2f; // Maximum displacement (positive only)

    // These two values, in the range 0 to 1, define which raw height values correspond to 0 and 1 displacement.
    [Range(0f, 1f)] public float displacementMin = 0.0f;
    [Range(0f, 1f)] public float displacementMax = 1.0f;

    private void OnValidate()
    {
        GenerateSphere();
    }

    void GenerateSphere()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        float radius = diameter / 2f; // Calculate radius from diameter

        // Vertices, UVs, and Normals
        Vector3[] vertices = new Vector3[(nbRings + 1) * (nbSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        int vertIndex = 0;

        for (int ring = 0; ring <= nbRings; ring++)
        {
            float phi = Mathf.PI * ring / nbRings; // Latitude angle (0 at top, PI at bottom)
            for (int segment = 0; segment <= nbSegments; segment++)
            {
                float theta = 2f * Mathf.PI * segment / nbSegments; // Longitude angle

                // Spherical coordinates with a 180° rotation on the Y-axis (negate X and Z)
                float x = -radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = radius * Mathf.Cos(phi);
                float z = -radius * Mathf.Sin(phi) * Mathf.Sin(theta);

                Vector3 vertex = new Vector3(x, y, z);
                // UV mapping: u from 0 to 1 along longitude, v from 0 (north pole) to 1 (south pole)
                Vector2 uv = new Vector2((float)segment / nbSegments, 1f - (float)ring / nbRings);
                Vector3 normal = vertex.normalized;

                // --- Displace vertex using height map ---
                if (heightMap != null)
                {
                    // Sample the height map using the computed UV coordinates.
                    // We assume height is stored in the red channel.
                    float rawHeight = heightMap.GetPixelBilinear(uv.x, uv.y).r;
                    // Remap rawHeight: values below displacementMin become 0, above displacementMax become 1.
                    float remappedHeight = Mathf.InverseLerp(displacementMin, displacementMax, rawHeight);
                    // Compute displacement (only positive: the vertex is pushed outward along its normal)
                    float displacement = remappedHeight * displacementStrength;
                    vertex += normal * displacement;
                }

                vertices[vertIndex] = vertex;
                uvs[vertIndex] = uv;
                normals[vertIndex] = normal;
                vertIndex++;
            }
        }

        // Build triangles.
        int[] triangles = new int[nbRings * nbSegments * 6];
        int triIndex = 0;
        for (int ring = 0; ring < nbRings; ring++)
        {
            for (int segment = 0; segment < nbSegments; segment++)
            {
                int current = ring * (nbSegments + 1) + segment;
                int next = current + nbSegments + 1;

                // First triangle (reversed order for proper normals)
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
                triangles[triIndex++] = current + 1;

                // Second triangle (reversed order)
                triangles[triIndex++] = next + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;
            }
        }

        // Assign mesh data.
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals; // Use manually calculated normals

        meshFilter.mesh = mesh;
    }

    void ClearChildren()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = null;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SphereGenerationWithDisplacement))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SphereGenerationWithDisplacement script = (SphereGenerationWithDisplacement)target;

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
#endif
}

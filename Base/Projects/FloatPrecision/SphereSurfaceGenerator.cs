using UnityEngine;
using System.Collections.Generic;

public class SphereSurfaceGenerator : MonoBehaviour
{
    public double simulationScale = 1.0; // The scale of the simulation (planet) the surface of the planet starts at that value
    public DoubleVector3 simulationPosition = new DoubleVector3(0, 0, 0); // The center of the simulation (planet)
    public FloatPrecisionPlayer player;
    public double generationRange = 1000.0; // How far to generate the surface around the player

    private Mesh mesh;
    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    void Update()
    {
        // Calculate the distance between the player and the simulation center
        double distanceToSimulationCenter = (simulationPosition - player.playerPosition).Magnitude();

        // Generate the surface if the player is within the generation range
        if (distanceToSimulationCenter <= generationRange)
        {
            GenerateSurface();
        }
        else
        {
            mesh.Clear(); // Clear the mesh if the player is outside the generation range
        }
    }

    void GenerateSurface()
    {
        // Create a list to store the vertices, edges, and faces
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Define resolution of the sphere segment (latitude and longitude divisions)
        int longitudeSegments = 20;  // Number of divisions around the Y axis
        int latitudeSegments = 10;   // Number of divisions from top to bottom

        double sphereRadius = (simulationPosition - player.playerPosition).Magnitude() * simulationScale; // Scale the radius based on distance and scale

        // Generate vertices for the surface (only the portion within range of the player)
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            double theta = (lat * Mathf.PI) / latitudeSegments; // Latitude angle

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                double phi = (lon * 2 * Mathf.PI) / longitudeSegments; // Longitude angle

                // Calculate the position of the vertex on the sphere's surface
                double x = sphereRadius * Mathf.Sin((float)theta) * Mathf.Cos((float)phi);
                double y = sphereRadius * Mathf.Cos((float)theta);
                double z = sphereRadius * Mathf.Sin((float)theta) * Mathf.Sin((float)phi);

                // Calculate the vertex position and adjust based on the simulation's center
                DoubleVector3 vertexPosition = new DoubleVector3(x, y, z) + simulationPosition;

                // Check if the vertex is within the generation range of the player
                double distanceToPlayer = (vertexPosition - player.playerPosition).Magnitude();
                if (distanceToPlayer <= generationRange)
                {
                    vertices.Add((Vector3)(vertexPosition));
                }
            }
        }

        // Generate faces (triangles) for the generated vertices
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                // Create triangles for the mesh
                if (lat != latitudeSegments - 1)
                {
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);
                }

                if (lat != 0)
                {
                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }
        }

        // Apply vertices and triangles to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
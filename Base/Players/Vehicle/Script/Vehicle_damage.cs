using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle_Deformation : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("The threshold impact force required to cause deformation to the vehicle.")]
    public float damageThreshold = 100f;

    [Tooltip("The amount of mesh deformation per damage point.")]
    public float deformationStrength = 0.05f;

    [Header("Vehicle Mesh")]
    [Tooltip("Array of vehicle mesh parts that will deform upon collision.")]
    public MeshFilter[] vehicleMeshFilters;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing from the vehicle!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check the impact force on the vehicle
        float impactForce = collision.relativeVelocity.magnitude * rb.mass;

        // If the impact force exceeds the damage threshold, apply mesh deformation
        if (impactForce > damageThreshold)
        {
            // Apply mesh deformation based on the collision
            ApplyMeshDeformation(collision.contacts[0].point, impactForce);
        }
    }

    private void ApplyMeshDeformation(Vector3 collisionPoint, float impactForce)
    {
        // We use the magnitude of the impact force to determine how much deformation to apply
        float deformationAmount = impactForce * deformationStrength;

        // Avoid excessive deformation that could distort the mesh or cause it to disappear
        deformationAmount = Mathf.Clamp(deformationAmount, 0, 1);  // Limit deformation amount

        foreach (MeshFilter meshFilter in vehicleMeshFilters)
        {
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] deformedVertices = new Vector3[vertices.Length];

            // Apply deformation to each vertex based on its distance to the collision point
            for (int i = 0; i < vertices.Length; i++)
            {
                // Calculate distance from the vertex to the collision point in world space
                Vector3 worldVertex = meshFilter.transform.TransformPoint(vertices[i]);
                float distance = Vector3.Distance(worldVertex, collisionPoint);

                // If the vertex is within the deformation range, apply force
                if (distance < deformationAmount)
                {
                    // Apply deformation strength to the vertex position
                    Vector3 deformation = (worldVertex - collisionPoint).normalized * (deformationAmount - distance);
                    deformedVertices[i] = vertices[i] + deformation;
                }
                else
                {
                    deformedVertices[i] = vertices[i];
                }
            }

            // Update the mesh with the deformed vertices
            mesh.vertices = deformedVertices;

            // Recalculate the mesh normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }
    }
}

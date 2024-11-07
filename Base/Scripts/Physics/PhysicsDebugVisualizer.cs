using UnityEngine;
using System.Collections.Generic;

public class PhysicsDebugger : MonoBehaviour
{
    public Rigidbody rb;

    // Visualization options
    public bool showVelocity = true;
    public bool showAppliedForce = true;
    public bool showContactPoints = true;

    // Force visualization modes
    public bool averageForceMode = true;  // True for average, false for contact points

    public Color velocityColor = Color.blue;
    public Color appliedForceColor = Color.red;
    public Color contactPointColor = Color.green;

    public float vectorScale = 0.5f;

    private List<Vector3> appliedForces = new List<Vector3>();
    private List<Vector3> contactPoints = new List<Vector3>();

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Clear forces and contact points from previous frame
        appliedForces.Clear();
        contactPoints.Clear();
    }

    public void ApplyForce(Vector3 force, Vector3 position)
    {
        rb.AddForceAtPosition(force, position, ForceMode.Force);
        appliedForces.Add(force);
        contactPoints.Add(position);
    }

    void OnCollisionStay(Collision collision)
    {
        // Record contact points during collision
        foreach (ContactPoint contact in collision.contacts)
        {
            contactPoints.Add(contact.point);
        }
    }

    void OnDrawGizmos()
    {
        if (rb == null) return;

        // Draw velocity vector
        if (showVelocity)
        {
            Gizmos.color = velocityColor;
            Gizmos.DrawLine(rb.position, rb.position + rb.velocity * vectorScale);
            Gizmos.DrawSphere(rb.position + rb.velocity * vectorScale, 0.05f);
        }

        // Draw applied forces
        if (showAppliedForce && appliedForces.Count > 0)
        {
            Gizmos.color = appliedForceColor;
            if (averageForceMode)
            {
                // Draw the average force vector
                Vector3 averageForce = Vector3.zero;
                foreach (var force in appliedForces) averageForce += force;
                averageForce /= appliedForces.Count;

                Gizmos.DrawLine(rb.position, rb.position + averageForce * vectorScale);
                Gizmos.DrawSphere(rb.position + averageForce * vectorScale, 0.05f);
            }
            else
            {
                // Draw force at each contact point
                for (int i = 0; i < appliedForces.Count; i++)
                {
                    Gizmos.DrawLine(contactPoints[i], contactPoints[i] + appliedForces[i] * vectorScale);
                    Gizmos.DrawSphere(contactPoints[i] + appliedForces[i] * vectorScale, 0.05f);
                }
            }
        }

        // Draw contact points
        if (showContactPoints)
        {
            Gizmos.color = contactPointColor;
            foreach (Vector3 contact in contactPoints)
            {
                Gizmos.DrawSphere(contact, 0.05f);
            }
        }
    }
}

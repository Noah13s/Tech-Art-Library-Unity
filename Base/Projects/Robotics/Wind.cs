using System.Collections;
using System.Collections.Generic;
using TechArtUtility;
using UnityEngine;

public class Wind : MonoBehaviour
{
    public float windForce = 10f;
    public float windRange = 20f;
    public float windAngle = 30f;

    void Update()
    {
        ApplyWind();
    }

    void ApplyWind()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, windRange);

        foreach (Collider col in colliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null)
                rb = col.attachedRigidbody;

            if (rb != null && !rb.isKinematic)
            {
                Vector3 toTarget = (rb.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toTarget);

                if (angle <= windAngle)
                {
                    // Apply wind force in the wind direction
                    Vector3 windDirection = transform.forward;
                    rb.AddForce(windDirection * windForce, ForceMode.Force);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, windRange);

        // Draw wind direction
        Gizmos.color = Color.cyan;
        DebugUtility.DrawFilledCone(transform.position, -transform.forward, windRange, windAngle, 20, Color.blue);

    }
}
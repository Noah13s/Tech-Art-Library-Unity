using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public float conveyorForce = 10.0f;
    public float additionalDrag = 5.0f;   // Extra drag applied when the object moves against the conveyor direction

    void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            // Use the conveyor belt's forward direction as the movement direction
            Vector3 conveyorDirection = transform.forward.normalized;

            // Get the object's velocity direction (if moving)
            Vector3 objectVelocityDirection = rb.velocity.normalized;

            // Calculate the alignment between the object's velocity and the conveyor direction
            float alignment = Vector3.Dot(objectVelocityDirection, conveyorDirection);

            // Apply conveyor force in the conveyor direction
            rb.AddForce(conveyorDirection * conveyorForce, ForceMode.Force);

            // If the alignment is below a threshold (e.g., moving opposite to the conveyor)
            if (alignment < 0.5f)  // Adjust the threshold as needed
            {
                // Apply additional drag if moving against conveyor direction
                rb.drag = additionalDrag;
            }
            else
            {
                // Reset to default drag when aligned with conveyor
                rb.drag = 0;
            }
        }
    }
}

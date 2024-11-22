using UnityEngine;
using UnityEngine.UIElements;

public class ConveyorBelt : MonoBehaviour
{
    public float conveyorForce = 1f;
    public float additionalDrag = 5.0f;   // Extra drag applied when the object moves against the conveyor direction
    public float visualSpeed = 0.5f;

    private PhysicMaterial physMat;

    private void OnValidate()
    {
        if (conveyorForce < 0 ) { conveyorForce = 0f; }
    }

    private void Awake()
    {
        // Setup the PhysicMaterial to have no friction
        physMat = GetComponent<Collider>().material = new PhysicMaterial("Instanced PhysMat");
        physMat.staticFriction = 0f;
        physMat.dynamicFriction = 0f;
        physMat.frictionCombine = PhysicMaterialCombine.Minimum;
    }

    private void Update()
    {
        // Update the conveyor speed in the renderer (for visuals)
        this.GetComponent<MeshRenderer>().material.SetFloat("_Speed", conveyorForce * visualSpeed);

        // Adjust friction when conveyor is off
        if (conveyorForce == 0f)
        {
            physMat.dynamicFriction = 0.15f;  // Apply friction when conveyor is stopped
        }
        else
        {
            physMat.dynamicFriction = 0f;  // No friction when conveyor is moving
        }
    }

    void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;

        if (rb != null)
        {
            // Deactivates the physics object sleep threshold so that it doesn't go to sleep while in contact with the conveyor
            rb.sleepThreshold = 0f;
            // Use the conveyor belt's forward direction as the movement direction
            Vector3 conveyorDirection = transform.forward.normalized;

            float conveyorAngle = Vector3.Angle(transform.up, Vector3.up);
            conveyorAngle = Mathf.Abs(Mathf.Cos(conveyorAngle * Mathf.Deg2Rad));
            conveyorAngle = (1 - conveyorAngle) * 500f;


            // If the conveyor is moving and the object is stopped (velocity close to zero), "push" the object to move again
            if (conveyorForce != 0f && rb.velocity.magnitude < 0.1f)
            {
                // Apply force in the direction of the conveyor if the object is at rest
                rb.AddForce(conveyorDirection * (conveyorForce * conveyorAngle), ForceMode.Force);
            }

            // Get the object's velocity direction (if moving)
            Vector3 objectVelocityDirection = rb.velocity.normalized;

            // Calculate the alignment between the object's velocity and the conveyor direction
            float alignment = Vector3.Dot(objectVelocityDirection, conveyorDirection);

            Debug.Log(conveyorAngle);

            // Apply additional drag if moving against conveyor direction (simulates friction for direction change)
            if (alignment < 0.5f)  // Checks if the object is sliding in other direction than the direction of the conveyor
            {
                rb.drag = additionalDrag;
            }
            else
            {
                // Reset to default drag when aligned with the conveyor
                rb.drag = 0;
            }

            // Apply force at each contact point to simulate conveyor movement
            foreach (ContactPoint contact in collision.contacts)
            {
                // Apply conveyor force at each contact point in the conveyor's direction
                rb.AddForceAtPosition(conveyorDirection * conveyorForce, contact.point, ForceMode.Force);
            }
        }
    }

    // Resets the sleepThreshold once the physics object exits the collision
    private void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null) {
            rb.sleepThreshold = rb.velocity.sqrMagnitude * 0.5f;
        }
    }
}

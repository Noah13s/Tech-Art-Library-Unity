using UnityEngine;

public class Propeller : MonoBehaviour
{
    [Tooltip("Clockwise (1) or Counterclockwise (-1) spin direction for propeller rotation.")]
    public int spinDirection = 1; // Set to 1 for clockwise, -1 for counterclockwise
    private float thrust = 0f; // Thrust value for the propeller
    private Rigidbody propellerRigidbody; // Rigidbody for the propeller

    void Start()
    {
        // Get or add Rigidbody component
        propellerRigidbody = GetComponent<Rigidbody>();
        if (propellerRigidbody == null)
        {
            propellerRigidbody = gameObject.AddComponent<Rigidbody>();
            propellerRigidbody.useGravity = false; // Propellers should not be affected by gravity
        }

        // Setting drag values for stability; adjust as needed for realism
        propellerRigidbody.angularDrag = 0.1f;
        propellerRigidbody.drag = 0.1f;
    }

    // Method called by DroneController to set the thrust
    public void SetThrust(float value)
    {
        thrust = value;
        ApplyPropellerRotation();
    }

    void ApplyPropellerRotation()
    {
        // Calculate rotational force proportional to thrust and spin direction
        float rotationalForce = thrust * spinDirection;

        // Apply torque to the Rigidbody to simulate propeller rotation
        propellerRigidbody.AddTorque(transform.up * rotationalForce, ForceMode.Force);
    }
}

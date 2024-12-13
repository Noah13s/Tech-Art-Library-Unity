using UnityEngine;
using UnityEngine.InputSystem;

public class Plane_Player : MonoBehaviour
{
    public float gravityForce = 9.8f;        // Simulated gravity force
    public float alignmentSpeed = 2f;        // Speed at which the plane aligns to its velocity vector
    public float maxThrust = 1000f;          // Maximum thrust force
    public float pitchSpeed = 50f;           // Speed at which the plane pitches (rotates around X-axis)
    public float gizmoRadius = 0.2f;        // Radius of the gizmo sphere
    public float liftCoefficient = 1.2f; // Coefficient controlling lift strength
    public float wingArea = 20f;        // Wing area in square meters
    public float airDensity = 1.225f;  // Air density at sea level in kg/m³

#if ENABLE_INPUT_SYSTEM
    private Plane_Controls controls;
    private InputAction pitchUp;
    private InputAction pitchDown;
    private InputAction throttleAction;
#endif

    private Rigidbody rb;
    private float currentThrust = 0f;         // Current thrust based on input


    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on the plane!");
            return;
        }

#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new Plane_Controls();
        controls.Enable();
        pitchUp = controls.Plane_Action_Map.PitchUp;
        pitchDown = controls.Plane_Action_Map.PitchDown;
        throttleAction = controls.Plane_Action_Map.Throttle;
#endif
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Apply gravity
        SimulateGravity();

        // Apply constant forward motion
        SimulateForwardMotion();

        // Apply thrust (throttle)
        HandleThrottle();

        // Align the plane to its velocity vector
        AlignToVelocity();

        // Apply pitch control affecting velocity and rotation
        HandlePitchControl();

        // Apply lift
        ApplyLift();
    }

    private void SimulateGravity()
    {
        Vector3 gravity = Vector3.down * gravityForce;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }

    private void SimulateForwardMotion()
    {
        // Apply a constant forward force in the plane's current forward direction
        rb.AddForce(transform.forward * currentThrust, ForceMode.Force);

    }

    private void HandleThrottle()
    {
        // Read the throttle input (e.g., from a trigger or axis input)
        currentThrust = throttleAction.ReadValue<float>() * maxThrust;

        // Ensure the thrust is within bounds
        currentThrust = Mathf.Clamp(currentThrust, 0f, maxThrust);
    }
    private void AlignToVelocity()
    {
        float yVelocityMagnitude = Mathf.Abs(rb.velocity.y);
        // Check if the velocity magnitude is significant
        if (yVelocityMagnitude > 10f)
        {
            // Calculate the target rotation based on the velocity vector
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);

            // Smoothly rotate the plane to align with the velocity vector
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyLift()
    {
        // Get the velocity vector
        Vector3 velocity = rb.velocity;

        // Calculate the speed (magnitude of the velocity vector)
        float speed = velocity.magnitude;

        // Calculate the angle between the velocity vector and the plane's forward vector
        float angleOfAttack = Vector3.Dot(velocity.normalized, transform.forward);

        // Ensure the angle of attack is between 0 and 1 (clamped)
        // This simulates how much the orientation contributes to lift generation
        angleOfAttack = Mathf.Clamp01(angleOfAttack);

        // Calculate lift force using speed and angle of attack
        float liftForceMagnitude = 0.5f * liftCoefficient * airDensity * speed * speed * wingArea * angleOfAttack;

        // Apply lift force upwards relative to the plane
        Vector3 liftForce = transform.up * liftForceMagnitude;
        rb.AddForce(liftForce, ForceMode.Force);
    }

    private void HandlePitchControl()
    {
        float yVelocityMagnitude = Mathf.Abs(rb.velocity.y);
        // Get pitch inputs (positive for up, negative for down)
        float pitchInput = pitchUp.ReadValue<float>() - pitchDown.ReadValue<float>();

        // If there is input, apply pitch control by rotating around the local X-axis (the pitch axis)
        if (pitchInput != 0 && rb.velocity.magnitude > 10f)
        {
            // Rotate the plane around its local X-axis based on pitch input
            transform.Rotate(Vector3.right * pitchInput * pitchSpeed * Time.fixedDeltaTime);

            // Adjust the velocity direction to reflect the new rotation
            // This ensures the plane's forward direction is updated after the pitch rotation
            rb.velocity = transform.forward * rb.velocity.magnitude;
        }
    }

    void OnDisable()
    {
        // Disable the controls when the script is disabled
#if ENABLE_INPUT_SYSTEM
        controls.Disable();
#endif
    }


    // Gizmo to visualize the center of mass
    void OnDrawGizmos()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Get the position of the center of mass relative to the plane's transform
            Vector3 centerOfMassPosition = transform.position + transform.rotation * rb.centerOfMass;

            // Set the color of the Gizmo
            Gizmos.color = Color.red;

            // Draw a sphere at the center of mass position
            Gizmos.DrawSphere(centerOfMassPosition, gizmoRadius);
        }
    }
}

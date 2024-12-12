using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Plane_Player : MonoBehaviour
{
    [Header("Plane Settings")]
    public float maxSpeed = 300f;
    public float thrustForce = 1000f;
    public float maxRotationalSpeed = 60f;
    public float liftFactor = 1.2f;
    public float dragCoefficient = 0.05f;

    [Header("Audio Settings")]
    public AudioSource engineAudioSource;
    public float minPitch = 1.0f;
    public float maxPitch = 3.0f;
    public float minVolume = 0.2f;
    public float maxVolume = 1.0f;

    [field: Header("Vehicle Metrics")]
    [Tooltip("Current speed of the vehicle in kilometers per hour (km/h).")]
    public float speedKMH { get; private set; }

    [Tooltip("Current speed of the vehicle in miles per hour (mph).")]
    public float speedMPH { get; private set; }

    [Tooltip("Current altitude of the vehicle above ground level in meters.")]
    public float altitudeMeters { get; private set; }

#if ENABLE_INPUT_SYSTEM
    private Plane_Controls controls;
    private InputAction pitchUpAction;
    private InputAction pitchDownAction;
    private InputAction rollRightAction;
    private InputAction rollLeftAction;
    private InputAction yawRightAction;
    private InputAction yawLeftAction;
    private InputAction throttleAction;
#endif

    private Rigidbody rb;
    private float currentThrottle = 0f;
    private float currentPitch = 0f;
    private float currentYaw = 0f;
    private float currentRoll = 0f;

    void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing!");
            return;
        }

        rb.useGravity = false; // We'll simulate gravity ourselves

#if ENABLE_INPUT_SYSTEM
        controls = new Plane_Controls();
        controls.Enable();

        pitchUpAction = controls.Plane_Action_Map.PitchUp;
        pitchDownAction = controls.Plane_Action_Map.PitchDown;
        rollRightAction = controls.Plane_Action_Map.RollRight;
        rollLeftAction = controls.Plane_Action_Map.RollLeft;
        yawRightAction = controls.Plane_Action_Map.YawRight;
        yawLeftAction = controls.Plane_Action_Map.YawLeft;
        throttleAction = controls.Plane_Action_Map.Throttle;
#endif
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        HandleNewInputSystem();
#endif
        UpdateMetrics();
        UpdateEngineSound();
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        float throttleInput = throttleAction.ReadValue<float>();
        float pitchInput = pitchUpAction.ReadValue<float>() - pitchDownAction.ReadValue<float>();
        float yawInput = yawRightAction.ReadValue<float>() - yawLeftAction.ReadValue<float>();
        float rollInput = rollRightAction.ReadValue<float>() - rollLeftAction.ReadValue<float>();

        ControlPlane(throttleInput, pitchInput, yawInput, rollInput);
    }
#endif

    private void ControlPlane(float throttle, float pitch, float yaw, float roll)
    {
        // Update throttle and apply forward thrust
        currentThrottle = Mathf.Clamp01(throttle);
        float thrust = currentThrottle * thrustForce;
        rb.AddForce(transform.forward * thrust);

        // Calculate velocity and its magnitude
        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;
        Vector3 forward = transform.forward;

        // Calculate angle of attack (AoA) between forward direction and velocity
        float angleOfAttack = 0f;
        if (speed > 0.1f)
        {
            angleOfAttack = Vector3.Angle(forward, velocity.normalized);
        }

        // Convert AoA to radians
        float angleOfAttackRad = Mathf.Deg2Rad * angleOfAttack;

        // Calculate lift: Lift is proportional to speed² * cos(AoA)
        float lift = liftFactor * speed * speed * Mathf.Cos(angleOfAttackRad);
        Vector3 liftForce = Vector3.up * lift;

        // Apply lift force
        rb.AddForce(liftForce);

        // Apply drag (aerodynamic resistance)
        Vector3 drag = -velocity.normalized * (dragCoefficient * speed * speed); // Linear drag
        float inducedDrag = Mathf.Sin(angleOfAttackRad) * lift; // Induced drag from lift
        rb.AddForce(drag + velocity.normalized * -inducedDrag);

        // Simulate gravity manually to balance lift
        Vector3 gravityForce = Vector3.down * 9.81f * rb.mass;
        rb.AddForce(gravityForce);

        // Apply rotational controls (roll, pitch, yaw)
        float rollRotation = roll * maxRotationalSpeed * Time.deltaTime;
        float yawRotation = yaw * maxRotationalSpeed * Time.deltaTime;
        float pitchRotation = pitch * maxRotationalSpeed * Time.deltaTime;

        transform.Rotate(Vector3.forward, -rollRotation); // Roll
        transform.Rotate(Vector3.up, yawRotation);        // Yaw
        transform.Rotate(Vector3.right, pitchRotation);   // Pitch

        // Adjust velocity direction when pitching down or up
        if (throttle == 0f) // When gliding
        {
            Vector3 gravityAcceleration = gravityForce / rb.mass;
            Vector3 forwardGravityComponent = Vector3.Project(gravityAcceleration, forward);
            rb.AddForce(forwardGravityComponent, ForceMode.Acceleration);
        }
    }

    private void UpdateMetrics()
    {
        // Speed Calculation
        float speedMS = rb.velocity.magnitude; // Speed in meters per second
        speedKMH = speedMS * 3.6f;             // Convert to kilometers per hour
        speedMPH = speedMS * 2.23694f;         // Convert to miles per hour

        // Altitude Calculation
        altitudeMeters = transform.position.y - altitudeMeters;
    }



    private void UpdateEngineSound()
    {
        if (engineAudioSource == null) return;

        float normalizedThrottle = Mathf.Clamp01(currentThrottle);
        float normalizedSpeed = Mathf.Clamp01(rb.velocity.magnitude / maxSpeed);

        float pitch = Mathf.Lerp(minPitch, maxPitch, normalizedThrottle + normalizedSpeed);
        float volume = Mathf.Lerp(minVolume, maxVolume, normalizedThrottle);

        engineAudioSource.pitch = pitch;
        engineAudioSource.volume = volume;
    }
}

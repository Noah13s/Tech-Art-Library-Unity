using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Vehicle_Player : MonoBehaviour
{

    [Tooltip("Array of wheel data for all wheels on the vehicle.")]
    public Vehicle_Wheel[] wheels;

    [Header("Vehicle Settings")]
    [Tooltip("The motor force applied to drive wheels (in Nm).")]
    public float motorForce = 1500f;

    [Tooltip("The maximum steering angle for steering wheels (in degrees).")]
    public float steeringAngle = 30f;

    [field: Header("Vehicle Speed")]
    [Tooltip("Current speed of the vehicle in kilometers per hour (km/h).")]
    public float speedKMH { get; private set; }

    [field: Header("Vehicle Speed")]
    [Tooltip("Current speed of the vehicle in miles per hour (mph).")]
    public float speedMPH { get; private set; }

    [Header("Engine Sound Settings")]
    [Tooltip("AudioSource component for the engine sound.")]
    public AudioSource engineAudioSource;

    [Tooltip("Minimum pitch of the engine sound.")]
    public float minPitch = 1.0f;

    [Tooltip("Maximum pitch of the engine sound.")]
    public float maxPitch = 3.0f;

    [Tooltip("Minimum volume of the engine sound.")]
    public float minVolume = 0.2f;

    [Tooltip("Maximum volume of the engine sound.")]
    public float maxVolume = 1.0f;

#if ENABLE_INPUT_SYSTEM
    private Vehicle_Controls controls;
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction handbrakeAction;
    private InputAction fullThrottleAction;
#endif

    private Rigidbody rb;
    private bool isHandbraking = false;
    private float currentSteeringAngle = 0f; // Store the current steering angle for gradual changes
    private float steeringSpeed = 5f; // Speed of steering transition (higher value means faster)

    void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing from the vehicle!");
            return;
        }

#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new Vehicle_Controls();
        controls.Enable();
        forwardAction = controls.Vehicle_Action_Map.Forward;
        backwardAction = controls.Vehicle_Action_Map.Backwards;
        rightAction = controls.Vehicle_Action_Map.Right;
        leftAction = controls.Vehicle_Action_Map.Left;
        handbrakeAction = controls.Vehicle_Action_Map.Handbrake;
        fullThrottleAction = controls.Vehicle_Action_Map.FullThrottle;
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
        UpdateWheelVisuals();
        UpdateSpeed();
        HandleSkidmarks();
        UpdateEngineSound();
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        // Read inputs
        float forward = forwardAction.ReadValue<float>();
        float backward = backwardAction.ReadValue<float>();
        float turn = rightAction.ReadValue<float>() - leftAction.ReadValue<float>();
        isHandbraking = handbrakeAction.IsPressed();
        bool isFullThrottle = fullThrottleAction.IsPressed();

        // Apply movement
        Drive(forward - backward, turn, isHandbraking, isFullThrottle);
    }
#endif

    private void Drive(float forwardInput, float turnInput, bool handbrake, bool isFullThrottle)
    {
        // Adjust throttle based on shift key press
        float throttle = forwardInput;
        if (forwardInput > 0)
        {
            // If moving forward, set throttle to 50% by default, or 100% if Shift is pressed
            throttle = isFullThrottle ? 1f : 0.5f;
        }

        float motor = handbrake ? 0f : throttle * motorForce; // Disable motor torque during handbrake // Apply motor force based on throttle value
        float targetSteeringAngle = turnInput * steeringAngle;

        // Gradually change the current steering angle towards the target angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, Time.deltaTime * steeringSpeed);

        foreach (Vehicle_Wheel wheel in wheels)
        {
            var wheeldata = wheel.wheelData;
            // Apply steering
            if (wheeldata.isSteering)
            {
                float finalSteering = currentSteeringAngle * (wheeldata.invertSteering ? -1 : 1);
                wheeldata.wheelCollider.steerAngle = finalSteering;
            }

            // Apply motor force or handbrake
            if (wheeldata.isDrive)
            {
                if (handbrake)
                {
                    wheeldata.wheelCollider.motorTorque = 0f; // Disable motor during handbrake
                }
                else
                {
                    wheeldata.wheelCollider.motorTorque = motor;
                }
            }

            // Apply braking force
            if (wheeldata.canBrake)
            {
                if (handbrake)
                {
                    wheeldata.wheelCollider.brakeTorque = wheeldata.brakeForce; // Apply custom brake force
                }
                else
                {
                    wheeldata.wheelCollider.brakeTorque = 0f; // Release brake when not handbraking
                }
            }
        }
    }

    private void UpdateWheelVisuals()
    {
        foreach (Vehicle_Wheel wheel in wheels)
        {
            var wheeldata = wheel.wheelData;
            WheelCollider collider = wheeldata.wheelCollider;
            Transform mesh = wheeldata.wheelMesh;

            // Update wheel mesh position and rotation
            collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            mesh.position = position;
            mesh.rotation = rotation;
        }
    }

    private void UpdateSpeed()
    {
        // Calculate speed from the Rigidbody's velocity (in meters per second)
        float speedMS = rb.velocity.magnitude;

        // Convert speed to kilometers per hour (km/h) and miles per hour (mph)
        speedKMH = speedMS * 3.6f;
        speedMPH = speedMS * 2.23694f;
    }

    private void HandleSkidmarks()
    {
        foreach (Vehicle_Wheel wheel in wheels)
        {
            var wheeldata = wheel.wheelData;
            // Check if the wheel is slipping and whether we should display skidmarks
            if (wheeldata.wheelCollider.isGrounded)
            {
                WheelHit hit;
                wheeldata.wheelCollider.GetGroundHit(out hit);

                // Simple check for skidding (based on wheel slip)
                if (Mathf.Abs(hit.sidewaysSlip) > 0.5f || Mathf.Abs(hit.forwardSlip) > 0.5f)
                {
                    // Enable the skid mark by enabling the TrailRenderer
                    if (wheeldata.skidTrail != null)
                    {
                        wheeldata.skidTrail.emitting = true;
                    }
                }
                else
                {
                    // Disable the skid mark when not skidding
                    if (wheeldata.skidTrail != null)
                    {
                        wheeldata.skidTrail.emitting = false;
                    }
                }
            }
            else
            {
                // Ensure the skid mark is turned off when the wheel is not grounded
                if (wheeldata.skidTrail != null)
                {
                    wheeldata.skidTrail.emitting = false;
                }
            }
        }
    }

    private void UpdateEngineSound()
    {
        if (engineAudioSource == null) return;

        // Use speed or throttle to adjust the engine sound pitch and volume
        float normalizedSpeed = Mathf.Clamp01(speedKMH / 200f); // Assuming 200 km/h is max speed
        float pitch = Mathf.Lerp(minPitch, maxPitch, normalizedSpeed);
        float volume = Mathf.Lerp(minVolume, maxVolume, normalizedSpeed);

        engineAudioSource.pitch = pitch;
        engineAudioSource.volume = volume;
    }
}

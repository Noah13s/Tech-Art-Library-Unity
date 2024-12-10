using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Vehicle_Player : MonoBehaviour
{
    [System.Serializable]
    public struct WheelData
    {
        [Tooltip("The Transform representing the visual model of the wheel.")]
        public Transform wheelMesh;

        [Tooltip("The WheelCollider handling the physics of the wheel.")]
        public WheelCollider wheelCollider;

        [Tooltip("If true, this wheel will be used for steering.")]
        public bool isSteering;

        [Tooltip("If true, this wheel will be used for applying motor force.")]
        public bool isDrive;

        [Tooltip("If true, the steering direction for this wheel is inverted.")]
        public bool invertSteering;

        [Tooltip("If true, this wheel is capable of applying braking force.")]
        public bool canBrake;

        [Tooltip("The maximum braking force applied by this wheel (in Nm).")]
        public float brakeForce;

        [Tooltip("The TrailRenderer used to show skidmarks for this wheel.")]
        public TrailRenderer skidTrail;
    }

    [Tooltip("Array of wheel data for all wheels on the vehicle.")]
    public WheelData[] wheels;

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

#if ENABLE_INPUT_SYSTEM
    private Vehicle_Controls controls;
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction handbrakeAction;
#endif

    private Rigidbody rb;
    private bool isHandbraking = false;

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
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        // Read inputs
        float forward = forwardAction.ReadValue<float>();
        float backward = backwardAction.ReadValue<float>();
        float turn = rightAction.ReadValue<float>() - leftAction.ReadValue<float>();
        isHandbraking = handbrakeAction.IsPressed();

        // Apply movement
        Drive(forward - backward, turn, isHandbraking);
    }
#endif

    private void Drive(float forwardInput, float turnInput, bool handbrake)
    {
        float motor = handbrake ? 0f : forwardInput * motorForce; // Disable motor torque during handbrake
        float steering = turnInput * steeringAngle;

        foreach (WheelData wheel in wheels)
        {
            // Apply steering
            if (wheel.isSteering)
            {
                float finalSteering = steering * (wheel.invertSteering ? -1 : 1);
                wheel.wheelCollider.steerAngle = finalSteering;
            }

            // Apply motor force or handbrake
            if (wheel.isDrive)
            {
                if (handbrake)
                {
                    wheel.wheelCollider.motorTorque = 0f; // Disable motor during handbrake
                }
                else
                {
                    wheel.wheelCollider.motorTorque = motor;
                }
            }

            // Apply braking force
            if (wheel.canBrake)
            {
                if (handbrake)
                {
                    wheel.wheelCollider.brakeTorque = wheel.brakeForce; // Apply custom brake force
                }
                else
                {
                    wheel.wheelCollider.brakeTorque = 0f; // Release brake when not handbraking
                }
            }
        }
    }

    private void UpdateWheelVisuals()
    {
        foreach (WheelData wheel in wheels)
        {
            WheelCollider collider = wheel.wheelCollider;
            Transform mesh = wheel.wheelMesh;

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
        foreach (WheelData wheel in wheels)
        {
            // Check if the wheel is slipping and whether we should display skidmarks
            if (wheel.wheelCollider.isGrounded)
            {
                WheelHit hit;
                wheel.wheelCollider.GetGroundHit(out hit);

                // Simple check for skidding (based on wheel slip)
                if (Mathf.Abs(hit.sidewaysSlip) > 0.5f || Mathf.Abs(hit.forwardSlip) > 0.5f)
                {
                    // Enable the skid mark by enabling the TrailRenderer
                    if (wheel.skidTrail != null)
                    {
                        wheel.skidTrail.emitting = true;
                    }
                }
                else
                {
                    // Disable the skid mark when not skidding
                    if (wheel.skidTrail != null)
                    {
                        wheel.skidTrail.emitting = false;
                    }
                }
            }
            else
            {
                // Ensure the skid mark is turned off when the wheel is not grounded
                if (wheel.skidTrail != null)
                {
                    wheel.skidTrail.emitting = false;
                }
            }
        }
    }
}

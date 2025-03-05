using System;
using UnityEngine;

public class Rotor : MonoBehaviour
{
    [Serializable]
    public struct RotorSetup// TL;DR
    {
        [Tooltip("In Kg/m3")]
        [SerializeField] public float airDensity;
        [Tooltip("In m")]
        [SerializeField] public float rotorDiscRadius;
        [SerializeField] public float rotorSpeed;
        [SerializeField] public float thrustCoefficient;
    }

    public RotorSetup rotorSetup = new()
    {
        airDensity = 1.225f,
        rotorDiscRadius = 0.5f,
        rotorSpeed = 0f,
        thrustCoefficient = 0.005f,
    };

    [Header("Rotor Parameters")]
    [Tooltip("Target Revolution Per Minute for the rotor to reach.")]
    public float targetSpeedRPM = 300f;

    [Tooltip("Acceleration time in seconds it takes the rotor to reach a target RPM.")]
    public float spoolupTime = 30f;

    [Tooltip("The acceleration from standstill is different from the acceleration mid-flight this factor determines how much faster it is mid flight.")]
    [Range(2f, 4f)] public float midFlightAccelerationFactor = 2f;

    [SerializeField] Transform rotorMesh;

    [Header("Rotor Data")]
    [ReadOnly]
    [Tooltip("Current speed of the rotor in RPM")]
    [SerializeField] float currentSpeedRPM = 0f;
    [ReadOnly]
    [Tooltip("Current generated thrust by the rotor in Kg.")]
    [SerializeField] float currentThrustKg = 0f;
    [Tooltip("Current generated thrust by the rotor in Newton.")]
    [SerializeField] float currentThrustN = 0f;
    [ReadOnly]
    [SerializeField] bool idle = false;
    [ReadOnly]
    [SerializeField] bool accelerating = false;
    [ReadOnly]
    [SerializeField] bool decelerating = false;

    [ReadOnly]
    [SerializeField] private Rigidbody rigidBody;
    private float currentSpoolTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only update rotor speed if it hasn't reached the target RPM yet
        if (currentSpeedRPM != targetSpeedRPM)
        {
            UpdateRotorSpeed();
            CalculateThrust();
        }
        else
        {
            // Idling
            decelerating = false;
            accelerating = false;
            idle = true;
        }
        // Apply the rotor speed to the mesh (or rotor animation) if the rotor is moving
        if (currentSpeedRPM > 0)
        {
            ApplyRotorSpeedToMesh();
        }

        ApplyThrust();
    }

    /// <summary>
    /// Update the rotor's speed based on whether it's accelerating from standstill or mid-flight.
    /// </summary>
    private void UpdateRotorSpeed()
    {
        // Calculate the difference between the target and current speed
        float speedDifference = targetSpeedRPM - currentSpeedRPM;

        // Determine the acceleration/deceleration factor
        float accelerationFactor = (currentSpeedRPM == 0f) ? 1f : midFlightAccelerationFactor;

        // Define separate acceleration and deceleration times for more control
        float effectiveSpoolTime = speedDifference > 0 ? spoolupTime : spoolupTime * 2f;

        // Calculate the speed change rate
        float speedChangeRate = Mathf.Abs(speedDifference) / effectiveSpoolTime * accelerationFactor;

        // Limits the speed Change Rate to a specified value so that the rate doesn't go to the infinitly small (threashold)
        if (speedChangeRate <= 1f)
        {
            speedChangeRate = 1f;
        }

        // Smoothly update the current speed
        if (speedDifference > 0f)
        {
            // Accelerating
            accelerating = true;
            decelerating = false;
            idle = false;
            currentSpeedRPM = Mathf.MoveTowards(
                currentSpeedRPM,
                targetSpeedRPM,
                speedChangeRate * Time.deltaTime
            );
        }
        else if (speedDifference < 0f)
        {
            // Decelerating
            decelerating = true;
            accelerating = false;
            idle = false;
            currentSpeedRPM = Mathf.MoveTowards(
                currentSpeedRPM,
                targetSpeedRPM,
                speedChangeRate * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Apply the current speed to the rotor mesh for visual representation.
    /// </summary>
    private void ApplyRotorSpeedToMesh()
    {
        if (rotorMesh != null)
        {
            // Rotate the rotor mesh based on current speed (scaled by RPM)
            float rotationSpeed = currentSpeedRPM * Time.deltaTime;
            rotorMesh.Rotate(Vector3.up * rotationSpeed);  // Rotates around the Y-axis (up direction)
        }
    }

    private void CalculateThrust()
    {
        float rotorDiskArea;
        float rps;

        rotorSetup.rotorSpeed = currentSpeedRPM;

        // Calculate rotor disk area (A = π * r²)
        rotorDiskArea = Mathf.PI * Mathf.Pow(rotorSetup.rotorDiscRadius, 2);

        // Convert RPM to RPS (Rotations per Second)
        rps = currentSpeedRPM / 60.0f;

        // Thrust Formula (T = ρ * A * (RPS)² * Ct) in N
        currentThrustN = (rotorSetup.airDensity * rotorDiskArea * Mathf.Pow(rps, 2) * rotorSetup.thrustCoefficient);
        // Converts thrust in N to thrust in Kg
        currentThrustKg = currentThrustN / 9.81f;
    }

    private void ApplyThrust()
    {
        // Apply thrust force to the Rigidbody in the up direction (or any direction your rotor is facing)
        rigidBody.AddForce(transform.up * currentThrustN * Time.fixedDeltaTime, ForceMode.Force);
    }
}

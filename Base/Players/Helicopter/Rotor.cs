using UnityEngine;

public class Rotor : MonoBehaviour
{
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
    [SerializeField] float currentSpeedRPM = 0f;
    [ReadOnly]
    [SerializeField] bool idle = false;
    [ReadOnly]
    [SerializeField] bool accelerating = false;
    [ReadOnly]
    [SerializeField] bool decelerating = false;

    private Rigidbody rigidBody;
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

        // Limits the speed Change Rate to a specified value so that the rate doesn't go to the infinitly small
        if (speedChangeRate <= 1f)
        {
            speedChangeRate = 1f;
        }
        Debug.Log(speedChangeRate);

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
}

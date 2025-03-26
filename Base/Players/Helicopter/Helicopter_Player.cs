using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using TechArtUtility;

public class Helicopter_Player : MonoBehaviour
{
    #region Variables
    [SerializeField] Rotor mainRotor;
    [SerializeField] Rotor tailRotor;
    [SerializeField] Rigidbody rigidBody; // Distance from center of mass to tail rotor
    [ReadOnly][SerializeField] float tailRotorDistance; // Distance from center of mass to tail rotor
    [ReadOnly][SerializeField] float requiredThrust; // Distance from center of mass to tail rotor
    [ReadOnly] public Vector3 targetDirection;
    [ReadOnly] public float directionDifference = 0f;
    [ReadOnly] public float altitude = 0f;
    public bool engineOnOff = false;

    [SerializeField] private float Kp = 0.1f; // Proportional gain (adjust as needed)
    [SerializeField] private float Kd = 0.05f; // Derivative gain (damping)

    [SerializeField] private float maxCorrection = 0.5f; // Max allowed correction to prevent excessive turning


    // Store last frame's directionDifference to calculate derivative term
    private float lastDirectionDifference = 0f;

    //Debug
    [NonSerialized] public bool debugMode;
    Color baseColor = new(255, 255, 255, 0.5f);

#if ENABLE_INPUT_SYSTEM
    private InputSystem_Actions controls;
    private InputAction fullThrottleAction;
    private InputAction thrustAction;
    private InputAction yawAction;
    private InputAction tiltAction;
#endif

    #endregion
    void Initialize()
    {
#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new ();
        controls.Enable();
        controls.bindingMask = new InputBinding { groups = "QwertyKeyboard" };// Masks ou the qwerty control scheme inputs
        thrustAction = controls.Helicopter_Player.Thrust;
        yawAction = controls.Helicopter_Player.Yaw;
        tiltAction = controls.Helicopter_Player.Tilt;
#endif
        targetDirection = -transform.forward;
    }

    private void Start()
    {
        Initialize();
    }

    void Update()
    {
        UpdateData();
        TorqueCompensation();


        HandleVerticalMovement();
        HandleTiltMovement();



#if ENABLE_INPUT_SYSTEM
        HandleNewInputSystem();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        // Read inputs
        Vector2 tilt = tiltAction.ReadValue<Vector2>();
        float turn = yawAction.ReadValue<float>();
        float turnSpeed = 100f; // Adjust for sensitivity

        // Apply rotation to direction vector
        Quaternion rotation = Quaternion.Euler(0, turn * turnSpeed * Time.deltaTime, 0);
        targetDirection = rotation * targetDirection;
    }
#endif

    private void HandleTiltMovement()
    {
        if (!engineOnOff || mainRotor == null)
            return; // Exit if the engine is off or main rotor is missing
        
        // Read player input
        Vector2 tiltInput = tiltAction.ReadValue<Vector2>(); // Pitch input

        // Define max tilt angles and smoothing
        float maxTiltAngle = 50f; // Maximum cyclic tilt angle (degrees)
        float tiltSpeed = 5f; // Smoothing speed

        //Vector2 tilt = tiltInput * maxTiltAngle;
        Vector2 tilt = new Vector2(tailRotor.CalculateMainRotorTiltAdjusted(mainRotor.currentThrustN), 0f);

        // Smoothly interpolate to the target tilt using Lerp
        float smoothedPitch = Mathf.LerpAngle(mainRotor.transform.localRotation.eulerAngles.x, tilt.x, Time.deltaTime * tiltSpeed);
        float smoothedRoll = Mathf.LerpAngle(mainRotor.transform.localRotation.eulerAngles.z, tilt.y, Time.deltaTime * tiltSpeed);
        //tilt = new Vector2(smoothedPitch, smoothedRoll);

        // Apply new rotation to the main rotor
        //mainRotor.transform.localRotation = Quaternion.Euler(smoothedPitch, mainRotor.transform.localEulerAngles.y, smoothedRoll);
        mainRotor.tiltInput = tilt;
        
        Debug.Log(tailRotor.CalculateMainRotorTiltAdjusted(mainRotor.currentThrustN));
        
    }


    private void HandleVerticalMovement()
    {
        if (engineOnOff)
        {
            float thrust = thrustAction.ReadValue<float>();

            if (thrust > 0f)
            {
                // Increase rotor speed to ascend
                mainRotor.SetTargetRPM(mainRotor.hoverRPM * 1.2f);
            }
            else if (thrust < 0f)
            {
                // Decrease rotor speed to descend
                mainRotor.SetTargetRPM(mainRotor.hoverRPM * 0.8f);
            }
            else
            {
                // Maintain hover RPM
                mainRotor.SetTargetRPM(mainRotor.hoverRPM);
            }
        } else
        {
            mainRotor.SetTargetRPM(0f);
        }
    }

    // Add a new serialized gain for the integral term
    [SerializeField] private float Ki = 0.01f; // Integral gain (adjust as needed)
                                               // And a new variable to accumulate error over time
    private float integralError = 0f;

private void TorqueCompensation() 
{
    if (mainRotor != null && tailRotor != null)
    {
        // Calculate the base required thrust ignoring main rotor tilt
        float baseRequiredThrust = mainRotor.CalculateCounterThrust(tailRotorDistance);

        // Get the main rotor tilt angle that already compensates for drift
        float mainRotorTilt = mainRotor.CalculateMainRotorTiltAdjusted(mainRotor.currentThrustN);

        // Convert tilt angle to a thrust reduction factor
        float tiltCompensationFactor = Mathf.Cos(mainRotorTilt * Mathf.Deg2Rad); // Reduce thrust if tilted

        // Apply the compensation factor
        requiredThrust = baseRequiredThrust * tiltCompensationFactor;

        // Update the accumulated error (integral term)
        integralError += directionDifference * Time.deltaTime;

        // Proportional term: helps correct the angle
        float proportional = Kp * directionDifference;

        // Derivative term: helps reduce overshooting
        float derivative = Kd * (directionDifference - lastDirectionDifference) / Time.deltaTime;

        // Integral term: reduces steady-state error
        float integral = Ki * integralError;

        // Compute final correction combining all three terms
        float correction = proportional + derivative + integral;

        // Clamp correction to prevent excessive turning
        correction = Mathf.Clamp(correction, -maxCorrection, maxCorrection);

        // Apply the correction to the required thrust
        requiredThrust += correction;
        tailRotor.SetTargetThrust(requiredThrust);

        // Store current direction difference for the next frame
        lastDirectionDifference = directionDifference;
    }
}

    private void UpdateData()
    {
        directionDifference = Vector3.SignedAngle(-transform.forward, targetDirection, transform.up);
        tailRotorDistance = CalculateTailRotorDistance(tailRotor.transform);
        altitude = transform.position.y;
    }

    public float CalculateTailRotorDistance(Transform tailRotorTransform)
    {
        if (rigidBody == null)
        {
            Debug.LogError("Rigidbody is not assigned.");
            return 0f;
        }

        Vector3 centerOfMass = rigidBody.worldCenterOfMass; // Get the COM position
        Vector3 tailRotorPosition = tailRotorTransform.position; // Get tail rotor world position

        float distance = Vector3.Distance(centerOfMass, tailRotorPosition);
        return distance;
    }


    #region Debug
    private void OnDrawGizmos()
    {
        UpdateDebugVisuals();
    }

    void UpdateDebugVisuals()
    {
        if (!debugMode) { return; }
        // Create a 1x1 texture
        Texture2D backgroundColoredTexture = new(1, 1);
        // Set the color with alpha (e.g., semi-transparent red)
        backgroundColoredTexture.SetPixel(0, 0, Color.white); // RGBA, where 0.5f is 50% transparency
        backgroundColoredTexture.Apply();
        GUIStyle style = new()
        {
            fontSize = 13,
            normal = new GUIStyleState()
        };
        style.normal.textColor = Color.black;
        style.normal.background = backgroundColoredTexture;
        //  Setup + Var declaration
        Handles.color = baseColor;

        //  Draws a single wedge for the two front wheels turn angle (should check if the two front wheels are turnable)
        DebugUtility.DrawWedgeGizmo(rigidBody.worldCenterOfMass, transform.up, -transform.forward, directionDifference, 0f, 6f, Color.red);        // Draw front wheel angle


        Handles.color = Color.green;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + -transform.forward * 5), 6);//   Draws a green line that indicates the direction of the aircraft
        DebugUtility.DrawFilledCone((rigidBody.worldCenterOfMass + (-transform.forward * 5.5f)), transform.forward, 20f, 0.5f, 32, Color.green);
        Handles.Label((rigidBody.worldCenterOfMass + (-transform.forward * 5.5f)), $"Heli angle : {Mathf.Round(Quaternion.FromToRotation(-transform.forward, Vector3.forward).eulerAngles.y)}", style);
        Handles.color = Color.red;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + targetDirection * 5), 6);//   Draws an orange ln
        DebugUtility.DrawFilledCone((rigidBody.worldCenterOfMass + targetDirection * 5.5f), -targetDirection, 20f, 0.5f, 32, Color.red);
        Handles.Label((rigidBody.worldCenterOfMass + targetDirection * 5.5f), $"Target angle : {Mathf.Round(Quaternion.FromToRotation(targetDirection, Vector3.forward).eulerAngles.y)}", style);

    }
    #endregion
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Helicopter_Player))]
public class CustomEditorHelicopter_Player : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Helicopter_Player playerScript = (Helicopter_Player)target;

        playerScript.debugMode = GUILayout.Toggle(playerScript.debugMode, playerScript.debugMode ? "Disable debug tools" : "Enable debug tools");
        if (playerScript.debugMode)
        {

        }

        GUILayout.Space(10);



       
    }
}
#endif
#endregion
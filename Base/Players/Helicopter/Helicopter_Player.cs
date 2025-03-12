using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private Helicopter_Controls controls;
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction tiltRightAction;
    private InputAction tiltLeftAction;
    private InputAction fullThrottleAction;
    private InputAction upAction;
    private InputAction downAction;
#endif

    #endregion
    void Initialize()
    {
#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new Helicopter_Controls();
        controls.Enable();
        forwardAction = controls.Helicopter_Action_Map.Forward;
        backwardAction = controls.Helicopter_Action_Map.Backwards;
        rightAction = controls.Helicopter_Action_Map.Right;
        leftAction = controls.Helicopter_Action_Map.Left;
        tiltRightAction = controls.Helicopter_Action_Map.TiltRight;
        tiltLeftAction = controls.Helicopter_Action_Map.TiltLeft;
        fullThrottleAction = controls.Helicopter_Action_Map.FullThrottle;
        upAction = controls.Helicopter_Action_Map.Up;
        downAction = controls.Helicopter_Action_Map.Down;
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
        float forward = forwardAction.ReadValue<float>();
        float backward = backwardAction.ReadValue<float>();
        float turn = rightAction.ReadValue<float>() - leftAction.ReadValue<float>();
        bool isFullThrottle = fullThrottleAction.IsPressed();
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
        float forward = forwardAction.ReadValue<float>() - backwardAction.ReadValue<float>(); // Pitch input
        float sideways = tiltRightAction.ReadValue<float>() - tiltLeftAction.ReadValue<float>(); // Roll input

        // Define max tilt angles and smoothing
        float maxTiltAngle = 10f; // Maximum cyclic tilt angle (degrees)
        float tiltSpeed = 5f; // Smoothing speed

        // Calculate target tilt angles based on input
        float targetPitch = forward * maxTiltAngle; // Forward/backward tilt
        float targetRoll = sideways * maxTiltAngle; // Left/right tilt

        // Smoothly interpolate to the target tilt using Lerp
        float smoothedPitch = Mathf.LerpAngle(mainRotor.transform.localRotation.eulerAngles.x, targetPitch, Time.deltaTime * tiltSpeed);
        float smoothedRoll = Mathf.LerpAngle(mainRotor.transform.localRotation.eulerAngles.z, targetRoll, Time.deltaTime * tiltSpeed);

        // Apply new rotation to the main rotor
        mainRotor.transform.localRotation = Quaternion.Euler(smoothedPitch, 0, smoothedRoll);
    }


    private void HandleVerticalMovement()
    {
        if (engineOnOff)
        {
            float up = upAction.ReadValue<float>();
            float down = downAction.ReadValue<float>();

            if (up > 0f)
            {
                // Increase rotor speed to ascend
                mainRotor.SetTargetRPM(mainRotor.hoverRPM * 1.2f);
            }
            else if (down > 0f)
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

    private void TorqueCompensation()
    {
        if (mainRotor != null && tailRotor != null)
        {
            // Calculate the required tail rotor thrust
            requiredThrust = mainRotor.CalculateCounterThrust(tailRotorDistance);

            // Proportional term (helps correct the angle)
            float proportional = Kp * directionDifference;

            // Derivative term (helps reduce overshooting)
            float derivative = Kd * (directionDifference - lastDirectionDifference) / Time.deltaTime;

            // Compute final correction
            float correction = proportional + derivative;

            // Clamp correction to prevent extreme overcorrection
            correction = Mathf.Clamp(correction, -maxCorrection, maxCorrection);

            // Apply limited correction
            requiredThrust += correction;

            // Apply thrust to the tail rotor
            tailRotor.SetTargetThrust(requiredThrust);

            // Store current direction difference for next frame
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
        //  Setup + Var declaration
        Handles.color = baseColor;

        //  Draws a single wedge for the two front wheels turn angle (should check if the two front wheels are turnable)
        DebugUtility.DrawFilledWedgeGizmo(rigidBody.worldCenterOfMass, transform.up, transform.forward, 360f, 0f, 4f,6f, baseColor);        // Draw front wheel angle


        Handles.color = Color.green;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + -transform.forward * 5), 6);//   Draws a green line that indicates the direction of the aircraft
        Handles.color = Color.red;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + targetDirection * 5), 6);//   Draws an orange ln
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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Vehicle_Player : MonoBehaviour
{
#region Variables
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
    private bool isHandbraking = false; // Tracks handbrake state
    private float currentSteeringAngle = 0f; // Store the current steering angle for gradual changes
    private readonly float steeringSpeed = 5f; // Speed of steering transition (higher value means faster)
    [NonSerialized] public bool debugMode = false;
#endregion

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

    private void OnDrawGizmos()
    {
        UpdateDebugVisuals();
    }

    private void UpdateDebugVisuals()
    {
        if (!debugMode) { return; }
        //  Setup + Var declaration
        Handles.color = new(255, 255, 255, 0.5f);
        float customSteeringAngle = steeringAngle + 20;//   Investigate why an offset is needed to get the correct wheels rotation
        #region Average Wheel Height Calculation
        float averageWheelsHeight = 0;
        // Calculate the average wheel height for the best wheel angle debug placement
        foreach (var wheel in wheels)
        {
            averageWheelsHeight += wheel.transform.position.y;
        }
        averageWheelsHeight = averageWheelsHeight/wheels.Length;
        #endregion

        //  Draw wheel turn angle for eahch tunable wheel
        foreach (var wheel in wheels)
        {
            if(wheel.wheelData.isSteering)
                Handles.DrawSolidArc(new Vector3(wheel.transform.position.x, averageWheelsHeight, wheel.transform.position.z), transform.up, Quaternion.Euler(0, -customSteeringAngle / 2, 0) * transform.forward, customSteeringAngle, 2f);
        }

        #region Single Wedge Wheel Turn Angle Debug
        //  Draws a single wedge for the two front wheels turn angle (should check if the two front wheels are turnable)
        DrawFilledWedgeGizmo(new Vector3(CalculateC(wheels[0].transform.position, wheels[1].transform.position, customSteeringAngle).x, averageWheelsHeight, CalculateC(wheels[0].transform.position, wheels[1].transform.position, customSteeringAngle).z), Vector3.up, Quaternion.Euler(0, -customSteeringAngle / 2, 0) * transform.forward, customSteeringAngle, 0f, 4f, 6f, new(255, 255, 255, 0.5f));        // Draw front wheel angle
        //  Draws the center of the two front wheel angle meetup point 
        Handles.color = Color.blue;
        Handles.DrawWireCube(CalculateC(wheels[0].transform.position, wheels[1].transform.position, customSteeringAngle), new Vector3(0.1f, 0.1f, 0.1f));
        #endregion

        Handles.color = Color.green;
        Handles.DrawLine(new Vector3(transform.position.x, averageWheelsHeight, transform.position.z), new Vector3(transform.position.x, averageWheelsHeight, transform.position.z) + (Quaternion.Euler(0, currentSteeringAngle, 0)*transform.forward*10));
    }

    void DrawFilledWedgeGizmo(Vector3 center, Vector3 normal, Vector3 from, float angle, float height, float innerRadius, float outerRadius, Color color)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Vector3 top = center + normal * height;
        int segments = 32;
        float angleStep = angle / segments;

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);

        // Fill the curved surfaces
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;

            Vector3 currentOuter = center + rotation * (Quaternion.AngleAxis(currentAngle, Vector3.up) * from) * outerRadius;
            Vector3 nextOuter = center + rotation * (Quaternion.AngleAxis(nextAngle, Vector3.up) * from) * outerRadius;
            Vector3 currentInner = center + rotation * (Quaternion.AngleAxis(currentAngle, Vector3.up) * from) * innerRadius;
            Vector3 nextInner = center + rotation * (Quaternion.AngleAxis(nextAngle, Vector3.up) * from) * innerRadius;

            // Bottom surface
            GL.Vertex(currentInner);
            GL.Vertex(currentOuter);
            GL.Vertex(nextOuter);

            GL.Vertex(currentInner);
            GL.Vertex(nextOuter);
            GL.Vertex(nextInner);

            // Top surface
            GL.Vertex(currentInner + normal * height);
            GL.Vertex(nextOuter + normal * height);
            GL.Vertex(currentOuter + normal * height);

            GL.Vertex(currentInner + normal * height);
            GL.Vertex(nextInner + normal * height);
            GL.Vertex(nextOuter + normal * height);

            // Side surfaces
            GL.Vertex(currentInner);
            GL.Vertex(currentInner + normal * height);
            GL.Vertex(currentOuter + normal * height);

            GL.Vertex(currentInner);
            GL.Vertex(currentOuter + normal * height);
            GL.Vertex(currentOuter);
        }

        // Fill the end caps
        Vector3 startOuter = center + rotation * from * outerRadius;
        Vector3 startInner = center + rotation * from * innerRadius;
        Vector3 endOuter = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * outerRadius;
        Vector3 endInner = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * innerRadius;

        // Start cap
        GL.Vertex(startInner);
        GL.Vertex(startOuter);
        GL.Vertex(startOuter + normal * height);

        GL.Vertex(startInner);
        GL.Vertex(startOuter + normal * height);
        GL.Vertex(startInner + normal * height);

        // End cap
        GL.Vertex(endInner);
        GL.Vertex(endOuter + normal * height);
        GL.Vertex(endOuter);

        GL.Vertex(endInner);
        GL.Vertex(endInner + normal * height);
        GL.Vertex(endOuter + normal * height);

        GL.End();
        GL.PopMatrix();

        // Draw wireframe arcs
        Handles.DrawWireArc(center, normal, from, angle, outerRadius);
        Handles.DrawWireArc(center, normal, from, angle, innerRadius);
        Handles.DrawWireArc(top, normal, from, angle, outerRadius);
        Handles.DrawWireArc(top, normal, from, angle, innerRadius);

        // Draw lines for edges
        Vector3[] corners = new Vector3[]
        {
        startOuter, startInner, endOuter, endInner,
        startOuter + normal * height, startInner + normal * height,
        endOuter + normal * height, endInner + normal * height
        };

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[4], corners[5]);
        Gizmos.DrawLine(corners[6], corners[7]);

        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[i + 4]);
    }


    void DrawWedgeGizmo(Vector3 center, Vector3 normal, Vector3 from, float angle, float height, float innerRadius, float outerRadius)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Vector3 top = center + normal * height;

        // Draw arcs at bottom and top
        Handles.DrawWireArc(center, normal, from, angle, outerRadius);
        Handles.DrawWireArc(center, normal, from, angle, innerRadius);
        Handles.DrawWireArc(top, normal, from, angle, outerRadius);
        Handles.DrawWireArc(top, normal, from, angle, innerRadius);

        // Calculate corner points
        Vector3 startOuter = center + rotation * from * outerRadius;
        Vector3 endOuter = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * outerRadius;
        Vector3 startInner = center + rotation * from * innerRadius;
        Vector3 endInner = center + rotation * (Quaternion.AngleAxis(angle, Vector3.up) * from) * innerRadius;

        // Draw vertical lines at edges
        Gizmos.DrawLine(startOuter, startOuter + normal * height);
        Gizmos.DrawLine(endOuter, endOuter + normal * height);
        Gizmos.DrawLine(startInner, startInner + normal * height);
        Gizmos.DrawLine(endInner, endInner + normal * height);

        // Draw lines connecting inner and outer arcs
        Gizmos.DrawLine(startInner, startOuter);
        Gizmos.DrawLine(endInner, endOuter);
        Gizmos.DrawLine(startInner + normal * height, startOuter + normal * height);
        Gizmos.DrawLine(endInner + normal * height, endOuter + normal * height);
    }

    public Vector3 CalculateC(Vector3 A, Vector3 B, float angle)
    {
        // Convert angle to radians
        float angleRad = angle * Mathf.Deg2Rad;

        // Compute direction vectors from A and B
        Vector3 dirAB = (B - A).normalized;

        // Compute perpendicular direction in the plane (assuming a 2D-like setup)
        Vector3 perpDir = Vector3.Cross(dirAB, Vector3.up).normalized;

        // Compute intersection point (C) using the given angle
        float distance = (B - A).magnitude / (2 * Mathf.Tan(angleRad / 2));
        Vector3 C = (A + B) / 2 - perpDir * distance;

        return C;
    }

}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Vehicle_Player))]
public class CustomEditorVehicle_Player : Editor
{
    bool debugMode = false;// Tracks if in debug mode or not
    string eventToExecute;//  Stores the string of the event the user wants to execute

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Vehicle_Player playerScript = (Vehicle_Player)target;

        GUILayout.Space(10);
        debugMode = playerScript.debugMode;
        playerScript.debugMode = GUILayout.Toggle(debugMode, debugMode ? "Disable debug tools" : "Enable debug tools");
        if (debugMode)
        {

            eventToExecute = EditorGUILayout.TextField(new GUIContent("Event to execute", "Enter the event name to execute here"), eventToExecute);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Execute", "Be careful executing events manually especially in editor out of play mode !")))
            {
                //script.TriggerEvent(eventToExecute);
            }
            EditorGUILayout.EndHorizontal();

        }
    }
}
#endif
#endregion
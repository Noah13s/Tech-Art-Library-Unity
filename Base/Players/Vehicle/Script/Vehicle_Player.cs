using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Vehicle_Player : MonoBehaviour
{
#region Variables
    [Tooltip("Array of wheel data for all wheels on the vehicle.")]
    public Vehicle_Wheel[] wheels = new Vehicle_Wheel[0];

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

    [field: Header("Vehicle Throttle")]
    [Tooltip("Current throttle of the vehicle in 0 to 1.")]
    public float currentThrottle { get; private set; }

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
    //  Debug
    [NonSerialized] public bool debugMode = false;
    [NonSerialized] public bool debugGlobalLabel = false;
    [NonSerialized] public bool debugWheelsLabels = false;
    [NonSerialized] public int debugLabelsSize = 10;
    [NonSerialized] public Color debugLabelsTextColor = Color.black;
    [NonSerialized] public Color debugLabelsBgColor = Color.white;
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
        currentThrottle = throttle;//   Set global current throttle to actual value

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

    public void SetDrive(float forwardInput, float turnInput, bool handbrake, bool isFullThrottle)
    {
        Drive(forwardInput, turnInput, handbrake, isFullThrottle);
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
            mesh.SetPositionAndRotation(position, rotation);
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
                wheeldata.wheelCollider.GetGroundHit(out WheelHit hit);

                // Simple check for skidding (based on wheel slip)
                if (Mathf.Abs(hit.sidewaysSlip) > 0.5f || Mathf.Abs(hit.forwardSlip) > 0.5f)
                {
                    wheel.isSkidding = true;
                    // Enable the skid mark by enabling the TrailRenderer
                    if (wheeldata.skidTrail != null)
                    {
                        wheeldata.skidTrail.emitting = true;
                    }
                }
                else
                {
                    wheel.isSkidding = false;
                    // Disable the skid mark when not skidding
                    if (wheeldata.skidTrail != null)
                    {
                        wheeldata.skidTrail.emitting = false;
                    }
                }
            }
            else
            {
                wheel.isSkidding = false;
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


    Color baseColor = new(255, 255, 255, 0.5f);

    private void UpdateDebugVisuals()
    {
        if (!debugMode) { return; }
        //  Setup + Var declaration
        Handles.color = baseColor;

        // Create a 1x1 texture
        Texture2D backgroundColoredTexture = new(1, 1);
        // Set the color with alpha (e.g., semi-transparent red)
        backgroundColoredTexture.SetPixel(0, 0, debugLabelsBgColor); // RGBA, where 0.5f is 50% transparency
        backgroundColoredTexture.Apply();

        GUIStyle style = new()
        {
            fontSize = debugLabelsSize,
            normal = new GUIStyleState()
        };
        style.normal.textColor = debugLabelsTextColor;
        style.normal.background = backgroundColoredTexture;
        string globalLabel = $"Speed: {speedKMH} Km/h\nSteering angle: {currentSteeringAngle}°\nThrottle: {currentThrottle}";

        #region Average Wheel Height Calculation
        float averageWheelsHeight = 0;
        // Calculate the average wheel height for the best wheel angle debug placement
        foreach (var wheel in wheels) { averageWheelsHeight += wheel.transform.position.y; }
        averageWheelsHeight /= wheels.Length;
        #endregion

        //  Draw debug tools for each wheels
        foreach (var wheel in wheels)
        {
            //  Draws a label for each wheels
            if (debugWheelsLabels)
            {
                string wheelLabel = $"Speed: {wheel.wheelData.wheelCollider.rpm} RPM\nSteering angle: {wheel.wheelData.wheelCollider.steerAngle}°\nSkidding: {wheel.isSkidding}";
                Handles.Label(new Vector3(wheel.transform.position.x, wheel.wheelData.wheelCollider.bounds.max.y, wheel.transform.position.z), wheelLabel, style);
            }


            if (wheel.wheelData.isSteering)
            {
                Handles.color = baseColor;
                //  Draws debug for steerable wheels
                Handles.DrawSolidArc(new Vector3(wheel.transform.position.x, averageWheelsHeight, wheel.transform.position.z), transform.up, Quaternion.Euler(0, -steeringAngle, 0) * transform.forward, steeringAngle*2, 1f);
                Handles.color = Color.green;
                Handles.DrawLine(new Vector3(wheel.transform.position.x, averageWheelsHeight, wheel.transform.position.z), new Vector3(wheel.transform.position.x, averageWheelsHeight, wheel.transform.position.z) + (Quaternion.Euler(0, currentSteeringAngle, 0) * transform.forward * 1f),5f);
            }
            else
            {
            }
        }

        foreach (var points in PredictTrajectory(wheels[2].transform.position, transform.forward, -currentSteeringAngle, Vector3.Distance(wheels[2].transform.position, wheels[3].transform.position), 20, -0.5f))
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(points, new Vector3(0.1f, 0.1f, 0.1f));
        }

        foreach (var points in PredictTrajectory(wheels[3].transform.position, transform.forward, -currentSteeringAngle, Vector3.Distance(wheels[2].transform.position, wheels[3].transform.position), 20, -0.5f))
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(points, new Vector3(0.1f, 0.1f, 0.1f));
        }

        #region Single Wedge for Wheel Turn Angle Debug
        Handles.color = baseColor;
        //  Draws a single wedge for the two front wheels turn angle (should check if the two front wheels are turnable)
        DrawFilledWedgeGizmo(new Vector3(CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle*2).x, averageWheelsHeight, CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle * 2).z), Vector3.up, Quaternion.Euler(0, -steeringAngle, 0) * transform.forward, steeringAngle*2, 0f, 4f, 6f, baseColor);        // Draw front wheel angle
        //  Draws the center of the two front wheel angle meetup point 
        Handles.color = Color.blue;
        Handles.DrawWireCube(CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle*2), new Vector3(0.1f, 0.1f, 0.1f));
        //  Draws the vehicle direction
        Handles.color = Color.green;
        Vector3 p1 = new Vector3(CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle * 2).x, averageWheelsHeight, CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle * 2).z);
        Vector3 p2 = new Vector3(CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle * 2).x, averageWheelsHeight, CalculateC(wheels[0].transform.position, wheels[1].transform.position, steeringAngle * 2).z) + (Quaternion.Euler(0, currentSteeringAngle, 0) * transform.forward * 6);
        Vector3 p3 = p2 - p1;// p3 is p1 but adjusted with an offset
        p3 = (p1 + (p3.normalized * 4));//  (A + ({offset} * AB))
        Handles.DrawLine(p3, p2,5);//   Draws a green line on the wedge to indicate the direction
        #endregion
        
        if (debugGlobalLabel)
            Handles.Label(new Vector3(transform.position.x, GetComponentInChildren<MeshCollider>().bounds.max.y, transform.position.z), globalLabel, style);

        #region Vehicle turning pivot debug
        // Get front and rear axle midpoints correctly
        Vector3 frontAxleMiddle = (wheels[0].transform.position + wheels[1].transform.position) * 0.5f;
        Vector3 rearAxleMiddle = (wheels[2].transform.position + wheels[3].transform.position) * 0.5f;

        // Calculate the vehicle's forward direction
        Vector3 vehicleForward = (frontAxleMiddle - rearAxleMiddle).normalized;

        // Wheelbase length
        float wheelBase = Vector3.Distance(frontAxleMiddle, rearAxleMiddle);

        // Get the steering pivot point
        Vector3 pivot = CalculateSteeringPivot(rearAxleMiddle, vehicleForward, wheelBase, -currentSteeringAngle);

        // Draw the pivot point
        Handles.color = Color.yellow;
        Handles.DrawWireCube(pivot, new Vector3(0.1f, 0.1f, 0.1f));
        #endregion

        float trackWidth = Vector3.Distance(wheels[2].transform.position, wheels[3].transform.position);


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

    // Updated function to consider vehicle orientation
    Vector3 CalculateSteeringPivot(Vector3 rearAxleMidpoint, Vector3 vehicleForward, float wheelbase, float steeringAngle)
    {
        if (Mathf.Abs(steeringAngle) < 0.001f)
            return Vector3.positiveInfinity; // No pivot when steering is 0

        float turningRadius = wheelbase / Mathf.Tan(steeringAngle * Mathf.Deg2Rad);

        // Pivot offset is perpendicular to the vehicle's forward direction
        Vector3 pivotOffset = Quaternion.AngleAxis(90, Vector3.up) * vehicleForward * turningRadius;

        return rearAxleMidpoint - pivotOffset;
    }

    public List<Vector3> PredictTrajectory(Vector3 rearWheelPosition, Vector3 forwardDirection, float steeringAngle, float wheelbase, int numPoints, float stepDistance)
    {
        List<Vector3> trajectory = new();
        trajectory.Add(rearWheelPosition); // Start trajectory from the rear wheel position

        float angleRad = steeringAngle * Mathf.Deg2Rad;

        // Straight movement case
        if (Mathf.Abs(angleRad) < 0.001f)
        {
            for (int i = 1; i < numPoints; i++)
            {
                rearWheelPosition -= forwardDirection.normalized * stepDistance;
                trajectory.Add(rearWheelPosition);
            }
            return trajectory;
        }

        // Compute turning radius
        float turningRadius = wheelbase / Mathf.Tan(angleRad);

        // Find center of rotation
        Vector3 rotationCenter = rearWheelPosition - Quaternion.Euler(0, 90, 0) * forwardDirection.normalized * turningRadius;

        float currentAngle = 0f;
        float stepAngle = stepDistance / turningRadius;

        for (int i = 1; i < numPoints; i++)
        {
            currentAngle += stepAngle;
            Vector3 offset = new Vector3(
                Mathf.Sin(currentAngle) * turningRadius,
                0,
                Mathf.Cos(currentAngle) * turningRadius
            );

            // Compute next point by rotating around the center
            Vector3 nextPoint = rotationCenter + Quaternion.Euler(0, Mathf.Rad2Deg * currentAngle, 0) * (rearWheelPosition - rotationCenter);
            trajectory.Add(nextPoint);
        }

        return trajectory;
    }
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Vehicle_Player))]
public class CustomEditorVehicle_Player : Editor
{

    public override void OnInspectorGUI()
    {
        ValidateVehicleSetup();
        base.OnInspectorGUI();
        Vehicle_Player playerScript = (Vehicle_Player)target;

        GUILayout.Space(10);


        playerScript.debugMode = GUILayout.Toggle(playerScript.debugMode, playerScript.debugMode ? "Disable debug tools" : "Enable debug tools");
        if (playerScript.debugMode)
        {
            playerScript.debugLabelsSize = EditorGUILayout.IntField("Debug labels size: ", playerScript.debugLabelsSize);
            playerScript.debugLabelsTextColor = EditorGUILayout.ColorField("Labels text color", playerScript.debugLabelsTextColor);
            playerScript.debugLabelsBgColor = EditorGUILayout.ColorField("Labels background color", playerScript.debugLabelsBgColor);
            EditorGUILayout.BeginHorizontal();
            playerScript.debugGlobalLabel = GUILayout.Toggle(playerScript.debugGlobalLabel, playerScript.debugGlobalLabel ? "Disable global debug labels" : "Enable global debug labels");
            playerScript.debugWheelsLabels = GUILayout.Toggle(playerScript.debugWheelsLabels, playerScript.debugWheelsLabels ? "Disable wheels debug labels" : "Enable wheels debug labels");
            EditorGUILayout.EndHorizontal();
        }
    }

    void ValidateVehicleSetup()
    {
        Vehicle_Player playerScript = (Vehicle_Player)target;
        bool noWheelsCanBrake = true;
        bool noWheelsReferenced = true;
        bool noWheelsCanSteer = true;
        bool noWheelsCanDrive = true;

        if (playerScript.wheels.Length <= 0) 
            return;

        if (playerScript.wheels.Length > 0 && playerScript.wheels.Length%2==1)
        {
            EditorGUILayout.HelpBox("An odd number of wheels has been setup.", MessageType.Warning);
        }

        foreach (var wheel in playerScript.wheels)
        {
            if (wheel)//  If wheel script is referenced 
            {
                noWheelsReferenced = false;
                if (!wheel.wheelData.wheelCollider | !wheel.wheelData.wheelMesh)
                {
                    EditorGUILayout.HelpBox($"This wheel: ({wheel.name}) has a wrong setup.", MessageType.Error);
                }
                if (wheel.wheelData.canBrake)
                {
                    noWheelsCanBrake = false;
                }
                if (wheel.wheelData.isSteering)
                {
                    noWheelsCanSteer = false;
                }
                if (wheel.wheelData.isDrive)
                {
                    noWheelsCanDrive = false;
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"A wheel entry has no reference!", MessageType.Error);
            }
        }
        if (noWheelsReferenced)//  If no wheel script is referenced there is no use displaying all the other messages so return
        {
            EditorGUILayout.HelpBox("No wheels scripts have been referenced!", MessageType.Error);
            return;
        }


        if (noWheelsCanBrake)//  No wheels can brake
        {
            EditorGUILayout.HelpBox("No wheels have braking enabled!", MessageType.Warning);
        }

        if (noWheelsCanSteer)//  No wheels can steer
        {
            EditorGUILayout.HelpBox("No wheels have steering enabled!", MessageType.Warning);
        }

        if (noWheelsCanDrive)//  No wheels can drive
        {
            EditorGUILayout.HelpBox("No wheels have drive enabled!", MessageType.Warning);
        }
    }
}
#endif
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Vehicle_AI : MonoBehaviour
{
    [SerializeField] Vehicle_Player targetVehicle;
    [SerializeField] float radius = 5f;
    [SerializeField] float step = 1f;
    [NonSerialized] public bool shouldGeneratePath = false;
    [NonSerialized] public bool isRealTime = false;
    [NonSerialized] public bool controlVehicle = false;
    [NonSerialized] public bool displayAiTarget = false;

    [NonSerialized] public Vector3 cachedStartPostion = Vector3.zero;
    [NonSerialized] public Vector3 cachedStartDirection = Vector3.zero;
    [NonSerialized] public List<Vector3> cachedPath = null;

    [Header("AI parameters")]
    [SerializeField] float maxSpeedKMH = 10f; // Maximum speed in km/h
    [SerializeField] float waypointThreshold = 1.0f; // Distance threshold to consider when reaching a point
    [SerializeField] float smoothingSpeed = 5f; // Control smoothing speed

    private int currentPathIndex = 0; // Index to track the current target point on the path
    private float turnInput;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isRealTime)
        {
            GeneratePath();
        }

    }


    private void OnDrawGizmos()
    {
        Handles.color = Color.gray;
        Handles.DrawWireCube(transform.position, transform.localScale);
        Handles.DrawDottedLine(transform.position, transform.position + (transform.forward * 5f), 5f);
        if (isRealTime)
        {
            UpdateStartPosition();
            GeneratePath();
        }
        DisplayPath();
        VehicleControl();

    }

    public static float GetDirectionalOffset(Vector3 objectForward, Vector3 objectPosition, Vector3 targetPosition)
    {
        // Get the direction to the target
        Vector3 toTarget = (targetPosition - objectPosition).normalized;

        // Project to horizontal plane (assuming Y-up world)
        objectForward.y = 0;
        toTarget.y = 0;
        objectForward.Normalize();
        toTarget.Normalize();

        // Get the signed angle between forward and target direction
        float angle = Vector3.SignedAngle(objectForward, toTarget, Vector3.up);

        // Normalize the value to be in range [-1, 1]
        return Mathf.Clamp(angle / 30f, -1f, 1f);
    }

    public void UpdateStartPosition()
    {
        cachedStartPostion = targetVehicle.transform.position;
        cachedStartDirection = targetVehicle.transform.forward;
    }

    public void GeneratePath()
    {
        float startAngleRadians = Mathf.Atan2(cachedStartDirection.z, cachedStartDirection.x);
        float startAngleDegrees = startAngleRadians * Mathf.Rad2Deg;

        float endAngleRadians = Mathf.Atan2(transform.forward.z, transform.forward.x);
        float endAngleDegrees = endAngleRadians * Mathf.Rad2Deg;

        // Get front and rear axle midpoints correctly
        Vector3 frontAxleMiddle = (targetVehicle.wheels[0].transform.position + targetVehicle.wheels[1].transform.position) * 0.5f;
        Vector3 rearAxleMiddle = (targetVehicle.wheels[2].transform.position + targetVehicle.wheels[3].transform.position) * 0.5f;

        // Wheelbase length
        float wheelBase = Vector3.Distance(frontAxleMiddle, rearAxleMiddle);

        float maxSteeringRad = targetVehicle.steeringAngle * Mathf.Deg2Rad; // Convert degrees to radians
        float calculatedRadius = wheelBase / Mathf.Tan(maxSteeringRad);

        cachedPath = DubinsPath.GetPathPoints(
            start: new Vector2(cachedStartPostion.x, cachedStartPostion.z),
            startAngle: startAngleRadians,
            end: new Vector2(transform.position.x, transform.position.z),
            endAngle: endAngleRadians,
            radius: radius,
            stepSize: step);
    }

    public void DisplayPath()
    {
        if (shouldGeneratePath && cachedPath != null)
        {
            Handles.color = Color.green;
            foreach (var point in cachedPath)
            {
                Handles.DrawWireCube(point, new Vector3(0.1f, 0.1f, 0.1f));
            }
        }
    }

    public void VehicleControl()
    {
        if (!controlVehicle || cachedPath.Count == 0) return;

        float forwardInput;
        bool handbrake;
        // Find the first waypoint that is far enough from the vehicle
        currentPathIndex = cachedPath.FindIndex(point =>
            Vector3.Distance(targetVehicle.transform.position, point) > waypointThreshold);

        // If no suitable point found, use the last point
        if (currentPathIndex == -1)
            currentPathIndex = cachedPath.Count - 1;

        // Remove points before the current index
        if (currentPathIndex > 0)
        {
            cachedPath.RemoveRange(0, currentPathIndex);
            currentPathIndex = 0;
        }

        // Get the current target position
        Vector3 currentTarget = cachedPath[0]; // Always use the first remaining point

        // Display AI target if enabled
        if (displayAiTarget)
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(currentTarget, Vector3.one * 0.2f);
        }
        if (cachedPath.Count <= 1)
        {
            forwardInput = 0f;
            handbrake = true;
        } else
        {
            // Determine forward input based on speed
            forwardInput = (targetVehicle.speedKMH > maxSpeedKMH) ? 0f : 1f;
            handbrake = false;
        }

        // Compute axle positions
        Vector3 frontAxleMiddle = (targetVehicle.wheels[0].transform.position + targetVehicle.wheels[1].transform.position) * 0.5f;
        Vector3 rearAxleMiddle = (targetVehicle.wheels[2].transform.position + targetVehicle.wheels[3].transform.position) * 0.5f;

        // Compute vehicle's forward direction
        Vector3 vehicleForward = (frontAxleMiddle - rearAxleMiddle).normalized;

        // Calculate turn input
        float turnInput = GetDirectionalOffset(targetVehicle.transform.forward, targetVehicle.transform.position, currentTarget);

        Debug.Log($"Index: {currentPathIndex} Length: {cachedPath.Count}");

        // Apply vehicle controls
        targetVehicle.SetDrive(forwardInput, turnInput, handbrake, false);
    }


    public Vector3 GetClosestPointOnPath(Vector3 targetPosition, List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            return targetPosition; // Return original position if path is invalid
        }

        Vector3 closestPoint = path[0];
        float minDistance = Vector3.Distance(targetPosition, closestPoint);

        // Iterate over all points on the path
        foreach (Vector3 pathPoint in path)
        {
            float distance = Vector3.Distance(targetPosition, pathPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = pathPoint;
            }
        }

        return closestPoint;
    }
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Vehicle_AI))]
public class CustomEditorVehicle_AI : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Vehicle_AI playerScript = (Vehicle_AI)target;

        GUILayout.Space(10);
        if (playerScript.controlVehicle = GUILayout.Toggle(playerScript.controlVehicle, playerScript.controlVehicle ? "Disable ai" : "Enable ai"))
        {
            playerScript.shouldGeneratePath = true;
            playerScript.UpdateStartPosition();
            playerScript.GeneratePath();
        }
        if (playerScript.shouldGeneratePath = GUILayout.Toggle(playerScript.shouldGeneratePath, playerScript.shouldGeneratePath ? "Disable path generation" : "Enable path generation"))
        {
            if (playerScript.isRealTime = GUILayout.Toggle(playerScript.isRealTime, playerScript.isRealTime ? "Disable realtime generation" : "Enable realtime generation"))
            {
                
            } else
            {
                if (GUILayout.Button(new GUIContent("Generate path", "Be careful executing events manually especially in editor out of play mode !")))
                {
                    playerScript.GeneratePath();
                }
            }

        }
        
        if (GUILayout.Button(new GUIContent("Update start position", "Be careful executing events manually especially in editor out of play mode !")))
        {
            playerScript.UpdateStartPosition();

        }
        GUILayout.Space(10);
        EditorGUILayout.LabelField($"Stored position: {playerScript.cachedStartPostion}");
        EditorGUILayout.LabelField($"Stored direction: {playerScript.cachedStartDirection}");
        GUILayout.Space(10);
        if (playerScript.displayAiTarget = GUILayout.Toggle(playerScript.displayAiTarget, playerScript.displayAiTarget ? "Disable ai target display" : "Enable ai target display"))
        {
            playerScript.shouldGeneratePath = true;

        }
    }
}
#endif
#endregion
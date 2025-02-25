using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Vehicle_AI : MonoBehaviour
{
    [SerializeField] Vehicle_Player targetVehicle;
    [SerializeField] float radius = 5f;
    [SerializeField] float step = 1f;
    [NonSerialized] public bool shouldGeneratePath = false;
    [NonSerialized] public bool isRealTime = false;

    [NonSerialized] public Vector3 cachedStartPostion = Vector3.zero;
    [NonSerialized] public Vector3 cachedStartDirection = Vector3.zero;
    [NonSerialized] public List<Vector3> cachedPath = null;

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
    }

    public void GeneratePath(Vector3 startPosition, Vector3 targetPosition, Vector3 startDirection, Vector3 endDirection, float maxAngle, float pointSpacing)
    {
        if (shouldGeneratePath)
        {
            #region Layout + Variables
            Texture2D backgroundColoredTexture = new(1, 1);
            backgroundColoredTexture.SetPixel(0, 0, Color.grey);
            backgroundColoredTexture.Apply();
            GUIStyle style = new()
            {
                fontSize = 10,
                normal = new GUIStyleState()
            };
            style.normal.textColor = Color.black;
            style.normal.background = backgroundColoredTexture;
            startPosition.y = targetPosition.y; // Keep same height
            float distance = Vector3.Distance(startPosition, targetPosition);
            #endregion

            List<Vector3> pathPoints = new() { startPosition };
            Vector3 currentDirection = startDirection.normalized;
            Vector3 currentPosition = startPosition;
            Vector3 toTarget = (targetPosition - currentPosition).normalized;

            // Calculate minimum required steps based on spacing
            int minSteps = Mathf.CeilToInt(distance / pointSpacing);

            // Calculate angles for direction constraints
            float startToEndAngle = Vector3.Angle(startDirection.normalized, endDirection.normalized);
            float initialToTargetAngle = Vector3.Angle(startDirection.normalized, toTarget);

            // Steps needed to rotate from start to end direction in the last 30% of the path
            int stepsNeededForEndRotation = Mathf.CeilToInt(startToEndAngle / (0.3f * maxAngle));
            int angleSteps = Mathf.CeilToInt(Mathf.Max(initialToTargetAngle, startToEndAngle) / maxAngle);

            // Determine the number of steps ensuring all constraints
            int steps = Mathf.Max(minSteps, stepsNeededForEndRotation, angleSteps * 2);

            float blendParameter = 1.0f / steps;

            for (int i = 0; i < steps; i++)
            {
                float t = (i + 1) / (float)steps;
                toTarget = (targetPosition - currentPosition).normalized;

                // Determine target direction based on current progress
                Vector3 targetDirection;
                if (t < 0.7f)
                {
                    targetDirection = toTarget;
                }
                else
                {
                    // Directly target end direction in the last 30% of steps
                    targetDirection = endDirection.normalized;
                }

                // Rotate towards the target direction within max angle
                Vector3 newDirection = Vector3.RotateTowards(currentDirection, targetDirection, Mathf.Deg2Rad * maxAngle, 0f).normalized;

                // Calculate remaining distance and adjust step distance
                float remainingDistance = Vector3.Distance(currentPosition, targetPosition);
                float moveDistance = pointSpacing;
                if (remainingDistance < moveDistance)
                    moveDistance = remainingDistance;

                // Update position and direction
                currentPosition += newDirection * moveDistance;
                pathPoints.Add(currentPosition);
                currentDirection = newDirection;

                // Exit loop if reached target
                if (remainingDistance <= Mathf.Epsilon)
                    break;
            }

            // Ensure the last point is exactly at the target position
            pathPoints[pathPoints.Count - 1] = targetPosition;

            // Draw path and directions
            Handles.color = Color.green;
            foreach (var point in pathPoints)
            {
                Handles.DrawWireCube(point, new Vector3(0.1f, 0.1f, 0.1f));
            }

            Handles.color = Color.blue;
            Handles.DrawLine(startPosition, startPosition + startDirection.normalized * 0.5f);
            Handles.DrawLine(targetPosition, targetPosition + endDirection.normalized * 0.5f);

            Handles.Label(Vector3.Lerp(startPosition, targetPosition, 0.5f) + Vector3.up * 0.2f,
                        $"Distance: {distance}\nSteps: {steps}", style);
        }
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
            radius: calculatedRadius,
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

    }
}
#endif
#endregion
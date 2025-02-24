using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Vehicle_AI : MonoBehaviour
{
    [SerializeField] Vehicle_Player targetVehicle;
    [SerializeField] float maxAngle = 30f;
    [SerializeField] float spacing = 10f;
    [NonSerialized] public bool shouldGeneratePath = false;
    [NonSerialized] public Vector3 cachedStartPostion = Vector3.zero;
    [NonSerialized] public Vector3 cachedStartDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnDrawGizmos()
    {
        Handles.color = Color.gray;
        Handles.DrawWireCube(transform.position, transform.localScale);
        Handles.DrawDottedLine(transform.position, transform.position + (transform.forward * 5f), 5f);
        GeneratePath(cachedStartPostion, transform.position, cachedStartDirection, transform.forward, maxAngle, spacing);
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
        playerScript.shouldGeneratePath = GUILayout.Toggle(playerScript.shouldGeneratePath, playerScript.shouldGeneratePath ? "Disable path generation" : "Enable path generation");
        if (GUILayout.Button(new GUIContent("Update start position", "Be careful executing events manually especially in editor out of play mode !")))
        {
            playerScript.UpdateStartPosition();
        }
    }
}
#endif
#endregion
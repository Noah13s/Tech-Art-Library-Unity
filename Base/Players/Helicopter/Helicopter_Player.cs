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

    //Debug
    [NonSerialized] public bool debugMode;
    Color baseColor = new(255, 255, 255, 0.5f);

#if ENABLE_INPUT_SYSTEM
    private Helicopter_Controls controls;
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction fullThrottleAction;
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
        fullThrottleAction = controls.Helicopter_Action_Map.FullThrottle;
#endif
        targetDirection = -transform.forward;
    }

    private void Start()
    {
        Initialize();
    }

    void Update()
    {

        tailRotorDistance = CalculateTailRotorDistance(tailRotor.transform);
        
        if (mainRotor != null && tailRotor != null)
        {
            // Calculate the required tail rotor thrust
            requiredThrust = mainRotor.CalculateCounterThrust(tailRotorDistance);

            // Apply thrust to the tail rotor
            tailRotor.SetTargetThrust(requiredThrust);
        }

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
        Debug.Log(targetDirection);
        targetDirection = rotation * targetDirection;
    }
#endif

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
        DrawFilledWedgeGizmo(rigidBody.worldCenterOfMass, transform.up, transform.forward, 360f, 0f, 4f,6f, baseColor);        // Draw front wheel angle


        Handles.color = Color.green;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + -transform.forward * 5), 6);//   Draws a green line that indicates the direction of the aircraft
        Handles.color = Color.red;
        Handles.DrawLine(rigidBody.worldCenterOfMass, (rigidBody.worldCenterOfMass + targetDirection * 5), 6);//   Draws an orange ln
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
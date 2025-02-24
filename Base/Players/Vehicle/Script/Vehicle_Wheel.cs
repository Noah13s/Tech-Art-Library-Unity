using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(WheelCollider))]
public class Vehicle_Wheel : MonoBehaviour
{

    [System.Serializable]
    public struct WheelData
    {
        [Tooltip("The Transform representing the visual model of the wheel.")]
        public Transform wheelMesh;

        [Tooltip("The WheelCollider handling the physics of the wheel.")]
        public WheelCollider wheelCollider;

        [Tooltip("If true, this wheel will be used for steering.")]
        public bool isSteering;

        [Tooltip("If true, this wheel will be used for applying motor force.")]
        public bool isDrive;

        [Tooltip("If true, the steering direction for this wheel is inverted.")]
        public bool invertSteering;

        [Tooltip("If true, this wheel is capable of applying braking force.")]
        public bool canBrake;

        [Tooltip("The maximum braking force applied by this wheel (in Nm).")]
        public float brakeForce;

        [Tooltip("The TrailRenderer used to show skidmarks for this wheel.")]
        public TrailRenderer skidTrail;        
    }

    public WheelData wheelData = new()
    {
    };
    [Tooltip("If true, this wheel is skidding.")]
    [NonSerialized] public bool isSkidding;
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(Vehicle_Wheel))]
public class CustomEditorVehicle_Wheel : Editor
{

    public override void OnInspectorGUI()
    {
        ValidateWheelSetup();
        base.OnInspectorGUI();
        Vehicle_Wheel playerScript = (Vehicle_Wheel)target;

        GUILayout.Space(10);

    }

    void ValidateWheelSetup()
    {
        Vehicle_Wheel playerScript = (Vehicle_Wheel)target;

        if (!playerScript.wheelData.wheelCollider | !playerScript.wheelData.wheelMesh)
        {
            EditorGUILayout.HelpBox($"This wheel has a wrong setup.", MessageType.Error);
        }
    }
}
#endif
#endregion
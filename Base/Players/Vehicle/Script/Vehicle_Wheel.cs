using System.Collections;
using System.Collections.Generic;
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

    public WheelData wheelData;
}

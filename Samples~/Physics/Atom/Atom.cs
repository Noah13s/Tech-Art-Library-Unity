using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;



[Serializable]
    public struct AtomData
    {
        [Serializable]
        public enum DecayMode
        {
            Stable = 0,
            Beta,
            Alpha,
            Gamma
        }
        [Serializable]
        public struct Nucleus
        {
            [Range(0, 98)]
            [SerializeField]
            [Tooltip("Number of protons in the nuclei")]
            public int atomicNumber;

            [Range(0, 98)]
            [SerializeField]
            [Tooltip("Number of neutrons in the nuclei")]
            public int neutronNumber;
#if UNITY_EDITOR
            [ReadOnly]
            [SerializeField]
            [Tooltip("The total number of protons and neutrons in the nucleus. It's often found written next to the element symbol")]
#endif
            public int massNumber;
        }
        [SerializeField]
        [Tooltip("")]
        public Nucleus nucleus;
#if UNITY_EDITOR
        [MinMax(min:0)]
        [SerializeField]
        [Tooltip("When the charge state of an atom is neutral(ground) then the number of electrons equals the number of protons (atomic number)")]
#endif
        public int numberOfElectrons;

        [Serializable]
        public enum Orbitals
        {
            S1,
            S2,
            P1,
            P2,
            P3,
            P4,
            P5,
            P6,
            D1,
            D2,
            D3,
            D4,
            D5,
            D6,
            D7,
            D8,
            D9,
            D10,
            F1,
            F2,
            F3,
            F4,
            F5,
            F6,
            F7,
            F8,
            F9,
            F10,
            F11,
            F12,
            F13,
            F14
        }

        [SerializeField]
        [Tooltip("Charge state of the atom neutral='ground', positive='cation' and negative='anion'")]
        public ChargeState chargeState;

        [Header("Decay")]
        [SerializeField]
        [Tooltip("Disregard decay of atom")]
        public bool disregardDecay;
        
        [SerializeField]
        public DecayMode decayMode;
#if UNITY_EDITOR
        [ConditionalVisibility("disregardDecay", true)]
        [SerializeField]
        [Tooltip("Half decay life time of the atom")]
        [MinMax(min:0)]
#endif
        public float halfLife;
#if UNITY_EDITOR
        [ConditionalVisibility("disregardDecay", true)]
        [SerializeField]
        [MinMax(min:0)]
        [Tooltip("Elapsed decay time")]
#endif
        public float decayTime;

        [SerializeField]
        [Tooltip("Kinetic energy of the atom in electron volts (eV) which is perceived as heat")]
        public float kineticEnergyInEV;

        [SerializeField]
        [Tooltip("Kinetic energy of the atom in joules (J) which is perceived as heat")]
        public float kineticEnergyInJoules;
        // New field to store the start time of the countdown
        [NonSerialized]
            public float startTime;
        }
[ExecuteAlways]
public class Atom : MonoBehaviour
{
    [SerializeField]
    public AtomData atomStartData;
#if UNITY_EDITOR
    [ReadOnly]
#endif
    [SerializeField]
    public AtomData atomStatus;

    public delegate void StartDataChangedEvent();
    public StartDataChangedEvent startDataChanged;

    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }

    private void OnValidate()
    {
        Setup();
        if (startDataChanged != null)
        {
            startDataChanged.Invoke();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Setup()
    {
        atomStatus = atomStartData;

        atomStartData.nucleus.massNumber = atomStartData.nucleus.neutronNumber + atomStartData.nucleus.atomicNumber;

        // Ensure decay time does not exceed half-life
        if (atomStatus.decayTime > atomStatus.halfLife)
        {
            atomStatus.decayTime = atomStatus.halfLife;
            atomStartData.decayTime = atomStartData.halfLife;
        }

        // Ensure kinetic energy consistency
        if (atomStartData.kineticEnergyInEV > 0)
        {
            atomStartData.kineticEnergyInJoules = atomStartData.kineticEnergyInEV * 1.602e-19f;
        }
        else if (atomStartData.kineticEnergyInJoules > 0)
        {
            atomStartData.kineticEnergyInEV = atomStartData.kineticEnergyInJoules / 1.602e-19f;
        }
    }


    [ContextMenu("Check atom validity")]
    private void CheckAtomValidity()
    {
        Debug.Log("Checking atom settings validity");
    }

    private void ResetDecayTime()
    {
        atomStartData.decayTime = atomStartData.halfLife;
    }

    private void ResetNumberOfEletrons()
    {
        atomStartData.numberOfElectrons = atomStartData.nucleus.atomicNumber;
    }

    // Custom editor class within the same script
#if UNITY_EDITOR
    [CustomEditor(typeof(Atom))]
    public class MyComponentEditor : Editor
    {
        SerializedProperty disregardDecayProperty;
        SerializedProperty testListProperty;

        void OnEnable()
        {
            disregardDecayProperty = serializedObject.FindProperty("atomStartData.disregardDecay");
            testListProperty = serializedObject.FindProperty("atomStartData.test");
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object's representation
            serializedObject.Update();

            DrawDefaultInspector();

            // Apply any changes to the serializedObject
            serializedObject.ApplyModifiedProperties();

            // Get a reference to the target object
            Atom myComponent = (Atom)target;

            // Add additional buttons or functionality as needed
            if (GUILayout.Button("Check settings validity"))
            {
                myComponent.CheckAtomValidity();
            }

            if (GUILayout.Button("Reset decay time"))
            {
                myComponent.ResetDecayTime();
            }

            if (GUILayout.Button("Reset number of electrons"))
            {
                myComponent.ResetNumberOfEletrons();
            }
        }
    }
#endif
}
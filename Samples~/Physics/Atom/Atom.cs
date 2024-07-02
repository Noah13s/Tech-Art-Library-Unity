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

            [ReadOnly]
            [SerializeField]
            [Tooltip("The total number of protons and neutrons in the nucleus. It's often found written next to the element symbol")]
            public int massNumber;
        }
        [SerializeField]
        [Tooltip("")]
        public Nucleus nucleus;

        [MinMax(min:0)]
        [SerializeField]
        [Tooltip("When the charge state of an atom is neutral(ground) then the number of electrons equals the number of protons (atomic number)")]
        public int numberOfElectrons;


        [Serializable]
        public enum Orbitals
        {
            sOrbital = 0,
            pOrbital,
            dOrbital,
            fOrbital
        }

        [SerializeField]
        [Tooltip("These orbitals define the probability of finding an electron at a particular location. There are different types of orbitals with varying shapes and energies, and the specific type of orbital an electron occupies influences its energy level and behavior.")]

        public Orbitals orbitals;

        [SerializeField]
        [Tooltip("Charge state of the atom neutral='ground', positive='cation' and negative='anion'")]
        public ChargeState chargeState;

        [Header("Decay")]
        [SerializeField]
        [Tooltip("Disregard decay of atom")]
        public bool disregardDecay;
        
        [SerializeField]
        public DecayMode decayMode;

        [ConditionalVisibility("disregardDecay", true)]
        [SerializeField]
        [Tooltip("Half decay life time of the atom")]
        [MinMax(min:0)]
        public float halfLife;
        
        [ConditionalVisibility("disregardDecay", true)]
        [SerializeField]
        [Tooltip("Elapsed decay time")]
        [MinMax(min:0)]
        public float decayTime;

        [Header("")]
        [SerializeField]
        [Tooltip("Kinetic energy of the atom in Joules which is perceived as heat")]
        public float kineticEnergy;

        // New field to store the start time of the countdown
        [NonSerialized]
        public float startTime;
    }
[ExecuteAlways]
public class Atom : MonoBehaviour
{
    [SerializeField]
    private AtomData atomStartData;
    [ReadOnly]
    [SerializeField]
    public AtomData atomStatus;

    public delegate void StartDataChangedEvent();
    public StartDataChangedEvent startDataChanged;

    // Start is called before the first frame update
    void Start()
    {
        Setup();
        StartCoroutine(CountDownDecayTime());
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
    }

    // Coroutine to count down decay time
    private IEnumerator CountDownDecayTime()
    {
        atomStatus.startTime = Time.time;

        while (atomStatus.decayTime > 0)
        {
            if (!atomStartData.disregardDecay && atomStatus.decayMode != AtomData.DecayMode.Stable)
            {
                // Calculate elapsed time in milliseconds
                float elapsedTime = (Time.time - atomStatus.startTime) * 1000f;

                // Calculate remaining time
                atomStatus.decayTime = Mathf.Max(0, (int)(atomStartData.decayTime - elapsedTime));
            }
            yield return null;
        }

        // Handle decay completion (e.g., destroy object, apply effects)
        Debug.Log("Decay completed!");
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
        }
    }
#endif
}
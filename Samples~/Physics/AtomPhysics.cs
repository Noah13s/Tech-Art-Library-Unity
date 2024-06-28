using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;



[ExecuteAlways]
public class AtomPhysics : MonoBehaviour
{
    [Serializable]
    private struct AtomData
    {
        [Serializable]
        public enum DecayMode
        {
            Beta = 0,
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

        [SerializeField]
        [Tooltip("When the charge state of an atom is neutral(ground) then the number of electrons equals the number of protons (atomic number)")]
        public int numberOfElectrons;

        [SerializeField]
        [Tooltip("Charge state of the atom neutral='ground', positive='cation' and negative='anion'")]
        public ChargeState chargeState;

        [SerializeField]
        [Tooltip("Half decay life time of the atom")]
        public float halfLife;

        [SerializeField]
        [Tooltip("Elapsed decay time")]
        public float decayTime;

        [SerializeField]
        public DecayMode decayMode;

        [SerializeField]
        [Tooltip("KineticEnergy of the atom in Joules which is perceived as heat")]
        public float kineticEnergy;

        // New field to store the start time of the countdown
        [NonSerialized]
        public float startTime;
    }
    [SerializeField]
    private AtomData atomStartData;
    [ReadOnly]
    [SerializeField]
    private AtomData atomStatus;
    // Start is called before the first frame update
    void Start()
    {
        Setup();
        StartCoroutine(CountDownDecayTime());
    }

    private void OnValidate()
    {
        Setup();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Setup()
    {
        atomStatus = atomStartData;

        // Ensure the number of electrons is correct based on the charge state
        switch (atomStatus.chargeState)
        {
            case ChargeState.Neutral:
                atomStatus.numberOfElectrons = atomStatus.nucleus.atomicNumber;
                break;
            case ChargeState.Positive:
                atomStatus.numberOfElectrons = atomStatus.nucleus.atomicNumber - 1; // Example for single positive charge
                break;
            case ChargeState.Negative:
                atomStatus.numberOfElectrons = atomStatus.nucleus.atomicNumber + 1; // Example for single negative charge
                break;
        }

        atomStartData.nucleus.massNumber = atomStartData.nucleus.neutronNumber + atomStartData.nucleus.atomicNumber;

        // Ensure decay time does not exceed half-life
        if (atomStatus.decayTime > atomStatus.halfLife)
        {
            atomStatus.decayTime = atomStatus.halfLife;
            atomStartData.decayTime = atomStartData.halfLife;
        }

        // Example of setting kinetic energy based on decay mode (hypothetical logic)
        switch (atomStatus.decayMode)
        {
            case AtomData.DecayMode.Beta:
                atomStatus.kineticEnergy = 0.5f; // Placeholder value
                break;
            case AtomData.DecayMode.Alpha:
                atomStatus.kineticEnergy = 1.0f; // Placeholder value
                break;
            case AtomData.DecayMode.Gamma:
                atomStatus.kineticEnergy = 1.5f; // Placeholder value
                break;
        }
    }

    // Coroutine to count down decay time
    private IEnumerator CountDownDecayTime()
    {
        atomStatus.startTime = Time.time;

        while (atomStatus.decayTime > 0)
        {
            // Calculate elapsed time in milliseconds
            float elapsedTime = (Time.time - atomStatus.startTime) * 1000f;

            // Calculate remaining time
            atomStatus.decayTime = Mathf.Max(0, (int)(atomStartData.decayTime - elapsedTime));

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
    [CustomEditor(typeof(AtomPhysics))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Atom Data", EditorStyles.boldLabel);
            // Draw the default inspector
            DrawDefaultInspector();


            // Get a reference to the target object
            AtomPhysics myComponent = (AtomPhysics)target;

            // Add a button and handle its click event
            if (GUILayout.Button("Check settings validity"))
            {
                myComponent.CheckAtomValidity();
            }

            // Add a button and handle its click event
            if (GUILayout.Button("Reset decay time"))
            {
                myComponent.ResetDecayTime();
            }
        }
    }
#endif
}
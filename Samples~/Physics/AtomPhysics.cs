using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;



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
        [SerializeField]
        [Tooltip("Number of protons in the nuclei")]
        public int atomicNumber;

        [SerializeField]
        [Tooltip("Number of protons in the nuclei")]
        public int neutronNumber;

        [SerializeField]
        [Tooltip("The total number of protons and neutrons in the nucleus. It's often found written next to the element symbol")]
        public int massNumber;

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
    }
    [SerializeField]
    private AtomData atomStartData;
    private AtomData atomStatus;
    // Start is called before the first frame update
    void Start()
    {
        Setup();
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
                atomStatus.numberOfElectrons = atomStatus.atomicNumber;
                break;
            case ChargeState.Positive:
                atomStatus.numberOfElectrons = atomStatus.atomicNumber - 1; // Example for single positive charge
                break;
            case ChargeState.Negative:
                atomStatus.numberOfElectrons = atomStatus.atomicNumber + 1; // Example for single negative charge
                break;
        }

        // Example of further setup linking other parameters
        // Ensure decay time does not exceed half-life
        if (atomStatus.decayTime > atomStatus.halfLife)
        {
            atomStatus.decayTime = atomStatus.halfLife;
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

    [ContextMenu("test")]
    private void CheckAtomValidity()
    {
        Debug.Log("Checking atom settings validity");
    }

    // Custom editor class within the same script
#if UNITY_EDITOR
    [CustomEditor(typeof(AtomPhysics))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Get a reference to the target object
            AtomPhysics myComponent = (AtomPhysics)target;

            // Add a button and handle its click event
            if (GUILayout.Button("Check settings validity"))
            {
                myComponent.CheckAtomValidity();
            }
        }
    }
#endif
}

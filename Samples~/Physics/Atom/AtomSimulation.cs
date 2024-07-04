using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Atom))]
[RequireComponent(typeof(ElementKnowledge))]
[RequireComponent(typeof(Visualiser))]
public class AtomSimulation : MonoBehaviour
{
    private Atom atom;
    private ElementKnowledge elementKnowledge;
    private List<GameObject> electronGameObjects;

    private void Init()
    {
        atom = GetComponent<Atom>();
        elementKnowledge = GetComponent<ElementKnowledge>();
        electronGameObjects = new List<GameObject>();
        foreach (Transform electron in transform.Find("Electrons"))
        {
            electronGameObjects.Add(electron.gameObject);
        }
    }

    void Start()
    {
        SetElectronProperties();
        StartCoroutine(CountDownDecayTime());
    }

    // Coroutine to count down decay time
    private IEnumerator CountDownDecayTime()
    {
        atom.atomStatus.startTime = Time.time;

        while (atom.atomStatus.decayTime > 0)
        {
            if (!atom.atomStartData.disregardDecay && atom.atomStatus.decayMode != AtomData.DecayMode.Stable)
            {
                // Calculate elapsed time in milliseconds
                float elapsedTime = (Time.time - atom.atomStatus.startTime) * 1000f;

                // Calculate remaining time
                atom.atomStatus.decayTime = Mathf.Max(0, (int)(atom.atomStartData.decayTime - elapsedTime));
            }
            yield return null;
        }

        // Handle decay completion (e.g., destroy object, apply effects)
        Debug.Log("Decay completed!");
    }

    public void SetElectronProperties()
    {
        Init();
        Dictionary<string, int> electronConfiguration = QuantumCalculator.CalculateElectronConfiguration(atom.atomStartData.nucleus.atomicNumber);
        List<float> electronVelocities = QuantumCalculator.CalculateElectronVelocities(atom.atomStartData.numberOfElectrons, atom.atomStartData.kineticEnergyInEV);

        int electronIndex = 0;
        foreach (KeyValuePair<string, int> orbital in electronConfiguration)
        {
            for (int i = 0; i < orbital.Value; i++)
            {
                if (electronIndex < electronGameObjects.Count)
                {
                    Electron electronScript = electronGameObjects[electronIndex].GetComponent<Electron>();
                    string orbitalType = GetOrbitalEnumName(orbital.Key);
                    if (Enum.TryParse(orbitalType, out AtomData.Orbitals parsedOrbital))
                    {
                        electronScript.orbitals = parsedOrbital;
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse orbital type: {orbitalType}");
                        continue;
                    }

                    electronScript.speed = electronVelocities[electronIndex];

                    // Calculate energy contributions for each electron based on their orbital and speed
                    float energyInEV = QuantumCalculator.CalculateElectronEnergyContribution(orbital.Key, electronScript.speed, atom.atomStartData.kineticEnergyInEV);
                    float energyInJoules = energyInEV * QuantumCalculator.ElectronCharge; // Convert eV to Joules

                    // Update the electron's energy contributions
                    electronScript.UpdateEnergyContributions(energyInEV, energyInJoules);

                    electronIndex++;
                }
            }
        }
    }

    private string GetOrbitalEnumName(string orbital)
    {
        // Convert orbital string (e.g., "1s2", "2p6") to enum name format (e.g., "S1", "P2")
        char subshellType = orbital[1];
        string subshellTypeName = subshellType.ToString().ToUpper(); // Convert 's', 'p', 'd', 'f' to 'S', 'P', 'D', 'F'
        string principalQuantumNumber = orbital[0].ToString(); // Extract principal quantum number

        return subshellTypeName + principalQuantumNumber; // Combine for enum name
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AtomSimulation))]
    public class MyComponentEditor : UnityEditor.Editor
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
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            AtomSimulation myComponent = (AtomSimulation)target;

            if (GUILayout.Button("Calculate electron composition"))
            {
                myComponent.SetElectronProperties();
            }
        }
    }
#endif
}

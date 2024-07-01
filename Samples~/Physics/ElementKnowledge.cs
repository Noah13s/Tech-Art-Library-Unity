using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Text;

[RequireComponent(typeof(AtomPhysics))]
public class ElementKnowledge : MonoBehaviour
{
    public bool autoIdentify;
    public TextAsset csvFile;  // CSV file to be imported
    private AtomPhysics atom;
    private List<ElementAsset> elements;

    private void OnEnable()
    {
        Init();
        
        if (autoIdentify)
            atom.startDataChanged += IdentifyElement;
    }

    private void OnDisable()
    {
        Init();
        if (autoIdentify)
            atom.startDataChanged -= IdentifyElement;
    }

    private void OnValidate()
    {
        Init();
        atom.startDataChanged -= IdentifyElement;
        if (autoIdentify)
        {
            atom.startDataChanged += IdentifyElement;
        }
    }

    private void Init()
    {
        atom = GetComponent<AtomPhysics>();
        LoadElementsFromCSV();
    }

    private void LoadElementsFromCSV()
    {
        elements = new List<ElementAsset>();

        if (csvFile == null)
        {
            Debug.LogError("CSV file not assigned");
            return;
        }

        using (StringReader reader = new StringReader(csvFile.text))
        {
            string line;
            bool isFirstLine = true;

            while ((line = reader.ReadLine()) != null)
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;  // Skip the header line
                }

                string[] values = SplitCsvLine(line);
                if (values.Length >= 3)
                {
                    int atomicNumber;
                    if (int.TryParse(values[0], out atomicNumber))
                    {
                        ElementAsset element = ScriptableObject.CreateInstance<ElementAsset>();
                        element.atomicNumber = atomicNumber;
                        element.elementName = values[1];

                        string stableIsotopesString = values[2].Trim('"'); // Remove surrounding quotes
                        string[] isotopes = stableIsotopesString.Split(new[] { ", " }, StringSplitOptions.None);

                        element.stableIsotopes = new int[isotopes.Length];
                        for (int i = 0; i < isotopes.Length; i++)
                        {
                            int neutrons;
                            if (int.TryParse(isotopes[i], out neutrons))
                            {
                                element.stableIsotopes[i] = neutrons;
                            }
                            else
                            {
                                //This is fired when an element has no stable neutron value
                                //Debug.LogError($"Failed to parse stable isotope '{isotopes[i]}' for element '{element.elementName}'");
                            }
                        }

                        elements.Add(element);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse atomic number: " + values[0]);
                    }
                }
            }
        }
    }

    private string[] SplitCsvLine(string line)
    {
        // Handle cases where commas might be inside quotation marks
        List<string> values = new List<string>();
        bool inQuotes = false;
        StringBuilder currentValue = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString()); // Add the last value
        return values.ToArray();
    }

    private void IdentifyElement()
    {
        if (elements == null || elements.Count == 0)
        {
            Debug.LogError("Element list is empty or not loaded");
            return;
        }

        foreach (ElementAsset element in elements)
        {
            if (atom.atomStatus.nucleus.atomicNumber == element.atomicNumber)
            {
                string isotopesString = string.Join(", ", element.stableIsotopes.Select(n => n.ToString()).ToArray());
                Debug.Log(element.elementName + " Stable with: " + isotopesString);
                break;
            }
        }
    }

    private void CheckStability()
    {
        if (elements == null || elements.Count == 0)
        {
            Debug.LogError("Element list is empty or not loaded");
            return;
        }

        int atomicNumber = atom.atomStatus.nucleus.atomicNumber;
        int neutronNumber = atom.atomStatus.nucleus.neutronNumber;

        foreach (ElementAsset element in elements)
        {
            if (element.atomicNumber == atomicNumber)
            {
                if (element.stableIsotopes.Contains(neutronNumber))
                {
                    Debug.Log($"The isotope of {element.elementName} with {neutronNumber} neutrons is stable.");
                }
                else
                {
                    Debug.Log($"The isotope of {element.elementName} with {neutronNumber} neutrons is not stable.");
                }
                return;
            }
        }

        Debug.LogError($"No element found with atomic number {atomicNumber}");
    }

    // Custom editor class within the same script
#if UNITY_EDITOR
    [CustomEditor(typeof(ElementKnowledge))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Atom Data", EditorStyles.boldLabel);
            // Draw the default inspector
            DrawDefaultInspector();

            // Get a reference to the target object
            ElementKnowledge myComponent = (ElementKnowledge)target;

            // Add a button and handle its click event
            if (GUILayout.Button("Identify Element"))
            {
                myComponent.IdentifyElement();
            }
            if (GUILayout.Button("Check Stability"))
            {
                myComponent.CheckStability();
            }
        }
    }
#endif
}

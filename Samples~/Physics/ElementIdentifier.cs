using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


[RequireComponent(typeof(AtomPhysics))]
public class ElementIdentifier : MonoBehaviour
{
    public ElementListAsset elementListAsset;
    private AtomPhysics atom;

    private void OnEnable()
    {
        Init();
        atom.startDataChanged += IdentifyElement;
    }

    private void OnDisable()
    {
        Init();
        atom.startDataChanged -= IdentifyElement;
    }

    private void Init()
    {
        atom = GetComponent<AtomPhysics>();
    }

    private void IdentifyElement()
    {
        if (elementListAsset == null)
        {
            Debug.LogError("ElementListAsset is not defined");
            return;
        }
        Init();
        foreach (ElementAsset element in elementListAsset.elementAssets)
        {
            if (atom.atomStatus.nucleus.atomicNumber == element.atomicNumber)
            {
                Debug.Log(element.elementName);
                break;
            }
        }
    }

    // Custom editor class within the same script
#if UNITY_EDITOR
    [CustomEditor(typeof(ElementIdentifier))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Atom Data", EditorStyles.boldLabel);
            // Draw the default inspector
            DrawDefaultInspector();


            // Get a reference to the target object
            ElementIdentifier myComponent = (ElementIdentifier)target;



            // Add a button and handle its click event
            if (GUILayout.Button("Identify element"))
            {
                myComponent.IdentifyElement();
            }
        }
    }
#endif
}

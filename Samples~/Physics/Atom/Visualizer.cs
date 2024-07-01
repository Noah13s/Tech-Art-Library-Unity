using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Visualiser : MonoBehaviour
{
    private AtomPhysics atom;
    private List<GameObject> childObjects = new List<GameObject>(); // List to store child objects

    // This script adds child gameobjects based on atom.startData.numberOfNeutrons every time it is called via its subscribed event 
    // (on validation of another script). This happens in near real-time and the goal of this script is to replicate a slider value in gameobject children.

    private void Init()
    {
        atom = GetComponent<AtomPhysics>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            atom.startDataChanged -= OnStartDataChanged;
            atom.startDataChanged += OnStartDataChanged;
        }
    }
#endif

    private void OnEnable()
    {
        Init(); // Initialize atom
        atom.startDataChanged += OnStartDataChanged;
    }

    private void OnDisable()
    {
        atom.startDataChanged -= OnStartDataChanged;
    }

    private void OnStartDataChanged()
    {
        GameObject nucleus;
        GameObject protons;
        GameObject neutrons;
        GameObject electron;
#if UNITY_EDITOR
        // Ensure we are in edit mode and not playing
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                ClearChildObjects(childObjects);
                nucleus = CreateChildObject(transform, "Nucleus");
                electron = CreateChildObject(transform, "Electrons");
                protons = CreateChildObject(nucleus.transform, "Protons");
                neutrons = CreateChildObject(nucleus.transform, "Neutrons");

                CreateChildObjects(atom.atomStatus.nucleus.atomicNumber, protons.transform, "Proton");
                CreateChildObjects(atom.atomStatus.nucleus.neutronNumber, neutrons.transform, "neutron");

                CreateChildObjects(atom.atomStatus.numberOfElectrons, electron.transform, "Electron");
            };
        }
        else
        {
            ClearChildObjects(childObjects);
            nucleus = CreateChildObject(transform, "Nucleus");
            electron = CreateChildObject(transform, "Electrons");
            protons = CreateChildObject(nucleus.transform, "Protons");
            neutrons = CreateChildObject(nucleus.transform, "Neutrons");

            CreateChildObjects(atom.atomStatus.numberOfElectrons, protons.transform, "Proton");
            CreateChildObjects(atom.atomStatus.numberOfElectrons, neutrons.transform, "neutron");

            CreateChildObjects(atom.atomStatus.numberOfElectrons, electron.transform, "Electron");
        }
#else
        // In play mode, directly clear and create child objects
        ClearChildObjects(childObjects);
        CreateChildObjects(atom.startData.numberOfNeutrons);
#endif
    }

    private void CreateChildObjects(int numberOfObjects, Transform parent, string name)
    {
        for (int i = 0; i < numberOfObjects; i++)
        {
            GameObject newChild = new GameObject(name);
            // Customize newChild properties or behavior if needed
            childObjects.Add(newChild);
            newChild.transform.SetParent(parent, false);
        }
    }

    private GameObject CreateChildObject(Transform parent, string name)
    {
        GameObject newChild = new GameObject(name);
        // Customize newChild properties or behavior if needed
        childObjects.Add(newChild);
        newChild.transform.SetParent(parent, false);
        return newChild;
    }

    private void ClearChildObjects(List<GameObject> objects)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                // In edit mode, use DestroyImmediate to destroy objects
                DestroyImmediate(obj);
#else
                // In play mode, use Destroy to destroy objects
                Destroy(obj);
#endif
            }
        }
        objects.Clear();
    }
}

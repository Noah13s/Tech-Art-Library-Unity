using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Rendering.InspectorCurveEditor;


#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AtomPhysics))]
public class Visualiser : MonoBehaviour
{
    private AtomPhysics atom;
    private List<GameObject> childObjects = new List<GameObject>();

    private void Init()
    {
        atom = GetComponent<AtomPhysics>();
    }

    private void OnEnable()
    {
        Init();
    }

    public void UpdateChildObjects()
    {
        Init();
        ClearChildObjects();

        GameObject nucleus = CreateChildObject("Nucleus", transform);
        GameObject protons = CreateChildObject("Protons", nucleus.transform);
        GameObject neutrons = CreateChildObject("Neutrons", nucleus.transform);
        GameObject electrons = CreateChildObject("Electrons", transform);
        if (atom!=null)
        {
            foreach (var proton in CreateChildObjects(atom.atomStatus.nucleus.atomicNumber, protons.transform, "Proton"))
            {
                var protonParticle = proton.AddComponent<FundamentalParticle>();
            };
            foreach (var neutron in CreateChildObjects(atom.atomStatus.nucleus.neutronNumber, neutrons.transform, "Neutron"))
            {
                var neutronParticle = neutron.AddComponent<FundamentalParticle>();
            };
            foreach (GameObject electron in CreateChildObjects(atom.atomStatus.numberOfElectrons, electrons.transform, "Electron"))
            {
                var electronParticle = electron.AddComponent<FundamentalParticle>();
            };
        }
    }

    private List<GameObject> CreateChildObjects(int count, Transform parent, string name)
    {
        List<GameObject> children= new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            childObjects.Add(child);
            children.Add(child);
        }
        return children;
    }

    private GameObject CreateChildObject(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        childObjects.Add(child);
        return child;
    }

    private void ClearChildObjects()
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            if (child != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
#endif
                {
                    Destroy(child.gameObject);
                }
            }
        }

        childObjects.Clear();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Visualiser))]
    public class VisualiserEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Visualiser visualiser = (Visualiser)target;

            if (GUILayout.Button("Update Atom Visualisation"))
            {
                visualiser.UpdateChildObjects();
            }
        }
    }
#endif
}

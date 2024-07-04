using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Rendering.InspectorCurveEditor;


#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Atom))]
public class Visualiser : MonoBehaviour
{
    public bool displayRepresentation=false;
    private Atom atom;
    private List<GameObject> childObjects = new List<GameObject>();

    private void DisplayRepresentation(bool showOrHide)
    {

    }

    private void Init()
    {
        atom = GetComponent<Atom>();
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
                protonParticle.particleName = "Hadron (baryon)";
                protonParticle.type = FundamentalParticle.FundatmentalType.Fermion;
                protonParticle.Spin = "1 / 2";
                protonParticle.fermion = FundamentalParticle.Fermion.Quarks;
                // Initialize quarkProperties array with one element
                protonParticle.quarkProperties = new FundamentalParticle.QuarkProperties[3];

                // Assign values to the single element in quarkProperties
                protonParticle.quarkProperties[0] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.UpQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Positive2Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Red
                };
                // Assign values to the single element in quarkProperties
                protonParticle.quarkProperties[1] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.UpQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Positive2Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Green
                };
                // Assign values to the single element in quarkProperties
                protonParticle.quarkProperties[2] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.DownQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Negative1Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Blue
                };
            };
            foreach (var neutron in CreateChildObjects(atom.atomStatus.nucleus.neutronNumber, neutrons.transform, "Neutron"))
            {
                var neutronParticle = neutron.AddComponent<FundamentalParticle>();
                neutronParticle.particleName = "Hadron (baryon)";
                neutronParticle.type = FundamentalParticle.FundatmentalType.Fermion;
                neutronParticle.Spin = "1 / 2";
                neutronParticle.fermion = FundamentalParticle.Fermion.Quarks;
                // Initialize quarkProperties array with one element
                neutronParticle.quarkProperties = new FundamentalParticle.QuarkProperties[3];

                // Assign values to the single element in quarkProperties
                neutronParticle.quarkProperties[0] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.DownQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Negative1Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Red
                };
                // Assign values to the single element in quarkProperties
                neutronParticle.quarkProperties[1] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.DownQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Negative1Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Green
                };
                // Assign values to the single element in quarkProperties
                neutronParticle.quarkProperties[2] = new FundamentalParticle.QuarkProperties
                {
                    quark = FundamentalParticle.Quark.UpQuark,
                    quarkCharge = FundamentalParticle.QuarkCharge.Positive2Over3e,
                    quarkColor = FundamentalParticle.QuarkColor.Blue
                };
            };
            foreach (GameObject electron in CreateChildObjects(atom.atomStatus.numberOfElectrons, electrons.transform, "Electron"))
            {
                var electronParticle = electron.AddComponent<FundamentalParticle>();
                electron.AddComponent<Electron>();
                electronParticle.particleName = "Electron";
                electronParticle.type = FundamentalParticle.FundatmentalType.Fermion;
                electronParticle.fermion = FundamentalParticle.Fermion.Lepton;
                electronParticle.Spin = "1 / 2";
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
        if (displayRepresentation)
        {
            child.AddComponent<MeshFilter>();
            child.AddComponent<MeshRenderer>();
        }
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

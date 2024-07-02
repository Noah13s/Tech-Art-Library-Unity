using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static FundamentalParticle;
using static UnityEngine.GraphicsBuffer;

public class FundamentalParticle : MonoBehaviour
{

    [Serializable]
    public enum Fermion
    {
        Quarks = 0,
        Lepton
    }
    [Serializable]
    public enum Quark
    {
        UpQuark = 0,
        DownQuark,
        CharmQuark,
        StrangeQuark,
        TopQuark,
        BottomQuark
    }
    [Serializable]
    public enum QuarkCharge
    {
        Positive2Over3e = 0,
        Negative1Over3e
    }
    [Serializable]
    public enum QuarkColor
    {
        Red = 0,
        Green,
        Blue
    }
    [Serializable]
    public struct QuarkProperties
    {
        public Quark quark;
        public QuarkCharge quarkCharge;
        public QuarkColor quarkColor;
    }
    [Serializable]
    public enum Boson
    {
        Gauge = 0,
        Higgs,
        Graviton
    }
    [Serializable]
    public enum Gauge
    {
        Photon = 0,
        Gluon,
        WAndZ
    }

    [Serializable]
    public enum FundatmentalType
    {
        Fermion=0,
        Boson,
        Anyon
    }
    [ReadOnly]
    public string particleName;

    public FundatmentalType type;

    public string Spin;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Fermion)]
    public Fermion fermion;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Boson)]
    public Boson boson;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Boson)]
    [EnumConditionalVisibility("boson", (int)Boson.Gauge)]
    public Gauge gauge;

    public QuarkProperties[] quarkProperties;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Custom editor class within the same script
#if UNITY_EDITOR
    [CustomEditor(typeof(FundamentalParticle))]
    public class FundamentalParticleEditor : Editor
    {
        SerializedProperty typeProperty;
        SerializedProperty fermionProperty;
        SerializedProperty quarkPropertiesProperty;

        void OnEnable()
        {
            typeProperty = serializedObject.FindProperty("type");
            fermionProperty = serializedObject.FindProperty("fermion");
            quarkPropertiesProperty = serializedObject.FindProperty("quarkProperties");
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object's representation
            serializedObject.Update();

            // Draw all default inspector fields except for quarkProperties
            DrawPropertiesExcluding(serializedObject, "quarkProperties");

            // Conditionally show quarkProperties based on type and fermion
            FundamentalParticle.FundatmentalType selectedType = (FundamentalParticle.FundatmentalType)typeProperty.enumValueIndex;
            FundamentalParticle.Fermion selectedFermion = (FundamentalParticle.Fermion)fermionProperty.enumValueIndex;

            if (selectedType == FundamentalParticle.FundatmentalType.Fermion &&
                selectedFermion == FundamentalParticle.Fermion.Quarks)
            {
                EditorGUILayout.PropertyField(quarkPropertiesProperty, true);  // Show the quarkProperties array
            }
            else if (selectedType == FundamentalParticle.FundatmentalType.Anyon)
            {
                EditorGUILayout.PropertyField(quarkPropertiesProperty, true);  // Show the quarkProperties array for Anyon type
            }

            // Apply any changes to the serializedObject
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}

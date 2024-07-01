using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FundamentalParticle;

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
    public FundatmentalType type;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Fermion)]
    public Fermion fermion;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Boson)]
    public Boson boson;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Boson)]
    [EnumConditionalVisibility("boson", (int)Boson.Gauge)]
    public Gauge gauge;

    [EnumConditionalVisibility("type", (int)FundatmentalType.Fermion)]
    [EnumConditionalVisibility("fermion", (int)Fermion.Quarks)]
    public QuarkProperties quarkProperties;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

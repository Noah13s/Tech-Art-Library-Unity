using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Molecule : MonoBehaviour
{
    [Serializable]
    public struct Composition
    {
        public string name;
        public string description;
        public int atomCount;
        public AtomData atomProperties;
    }
    public Composition[] composition;
}

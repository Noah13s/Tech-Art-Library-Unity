using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementAsset", menuName = "Physics/ElementAsset")]
public class ElementAsset : ScriptableObject
{
    [SerializeField]
    public string elementName;
    [Range(0, 98)]
    [SerializeField]
    public int atomicNumber;
    public int[] stableIsotopes;

    /*
    private void OnValidate()
    {
        elementDefintion.nucleus.massNumber = elementDefintion.nucleus.neutronNumber + elementDefintion.nucleus.atomicNumber;
        elementDefintion.decayTime = elementDefintion.halfLife;
    }*/
}

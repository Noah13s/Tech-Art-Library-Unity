using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementAssetList", menuName = "Physics/ElementAssetList")]
public class ElementListAsset : ScriptableObject
{
    public ElementAsset[] elementAssets;
}

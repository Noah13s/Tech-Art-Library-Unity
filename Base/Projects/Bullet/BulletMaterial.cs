using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletMaterial : MonoBehaviour
{
    // List of preset materials
    public enum MaterialPreset
    {
        Steel,
        Wood,
        Cloth,
        Plastic,
        Custom // allows manual mass entry
    }

    [Header("Material Settings")]
    public MaterialPreset selectedMaterial = MaterialPreset.Steel;

    [Tooltip("Mass in kg; only editable if Custom is selected")]
    public float mass = 1f;

    // Internal dictionary to hold preset masses
    private Dictionary<MaterialPreset, float> presetMasses = new Dictionary<MaterialPreset, float>()
    {
        { MaterialPreset.Steel, 7.8f },
        { MaterialPreset.Wood, 0.6f },
        { MaterialPreset.Cloth, 0.1f },
        { MaterialPreset.Plastic, 0.9f }
    };

    private void OnValidate()
    {
        // If not custom, set mass automatically from preset
        if (selectedMaterial != MaterialPreset.Custom)
        {
            if (presetMasses.ContainsKey(selectedMaterial))
                mass = presetMasses[selectedMaterial];
        }
        // If Custom, mass can be set manually via inspector
    }
}

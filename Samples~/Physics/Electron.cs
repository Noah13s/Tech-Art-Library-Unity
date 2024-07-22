using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Electron : MonoBehaviour
{
    [SerializeField]
    [Tooltip("These orbitals define the probability of finding an electron at a particular location. There are different types of orbitals with varying shapes and energies, and the specific type of orbital an electron occupies influences its energy level and behavior.")]
    public AtomData.Orbitals orbitals;

    public float speed;
#if UNITY_EDITOR
    [ReadOnly]
#endif
    public float energyContributionInEV;
#if UNITY_EDITOR
    [ReadOnly]
#endif
    public float energyContributionInJoules;

    // Method to update energy contributions based on provided energy in eV and J
    public void UpdateEnergyContributions(float energyInEV, float energyInJoules)
    {
        energyContributionInEV = energyInEV;
        energyContributionInJoules = energyInJoules;
    }
}

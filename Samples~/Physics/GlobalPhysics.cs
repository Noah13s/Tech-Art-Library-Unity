using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum ChargeState
{
    Neutral = 0,
    Positive,
    Negative
}

public enum MatterState
{
    Solid = 0,
    Liquid,
    Gas,
    Plasma
}

[Serializable]
public struct Temperature
{
    public float Celcius;
    public float Kelvin;
    public float Fahrenheit;
}

public static class QuantumCalculator
{
    public const float ElectronCharge = 1.602e-19f; // Elementary charge in Coulombs
    private const float ElectronMass = 9.10938356e-31f; // Mass of electron in kg
    private const float PlanckConstant = 6.62607015e-34f; // Planck's constant in Js
    private const float ReducedPlanckConstant = PlanckConstant / (2 * Mathf.PI);

    // Electron configuration for elements up to Z=20
    private static readonly Dictionary<int, string[]> ElectronShells = new Dictionary<int, string[]>
    {
        { 1, new[] { "1s1" } },
        { 2, new[] { "1s2" } },
        { 3, new[] { "1s2", "2s1" } },
        { 4, new[] { "1s2", "2s2" } },
        { 5, new[] { "1s2", "2s2", "2p1" } },
        { 6, new[] { "1s2", "2s2", "2p2" } },
        { 7, new[] { "1s2", "2s2", "2p3" } },
        { 8, new[] { "1s2", "2s2", "2p4" } },
        { 9, new[] { "1s2", "2s2", "2p5" } },
        { 10, new[] { "1s2", "2s2", "2p6" } },
        { 11, new[] { "1s2", "2s2", "2p6", "3s1" } },
        { 12, new[] { "1s2", "2s2", "2p6", "3s2" } },
        { 13, new[] { "1s2", "2s2", "2p6", "3s2", "3p1" } },
        { 14, new[] { "1s2", "2s2", "2p6", "3s2", "3p2" } },
        { 15, new[] { "1s2", "2s2", "2p6", "3s2", "3p3" } },
        { 16, new[] { "1s2", "2s2", "2p6", "3s2", "3p4" } },
        { 17, new[] { "1s2", "2s2", "2p6", "3s2", "3p5" } },
        { 18, new[] { "1s2", "2s2", "2p6", "3s2", "3p6" } },
        { 19, new[] { "1s2", "2s2", "2p6", "3s2", "3p6", "4s1" } },
        { 20, new[] { "1s2", "2s2", "2p6", "3s2", "3p6", "4s2" } }
    };

    public static Dictionary<string, int> CalculateElectronConfiguration(int atomicNumber)
    {
        var config = new Dictionary<string, int>();

        foreach (var shell in ElectronShells)
        {
            foreach (var subshell in shell.Value)
            {
                if (atomicNumber > 0)
                {
                    int maxElectronsInSubshell = GetMaxElectronsInSubshell(subshell);
                    int electronsInSubshell = Math.Min(atomicNumber, maxElectronsInSubshell);
                    config[subshell] = electronsInSubshell;
                    atomicNumber -= electronsInSubshell;
                }
            }
        }

        return config;
    }

    private static int GetMaxElectronsInSubshell(string subshell)
    {
        char subshellType = subshell[1];
        return subshellType switch
        {
            's' => 2,
            'p' => 6,
            'd' => 10,
            'f' => 14,
            _ => 0
        };
    }

    public static List<float> CalculateElectronVelocities(int numberOfElectrons, float kineticEnergyInEV)
    {
        List<float> velocities = new List<float>();
        float totalKineticEnergyInJoules = kineticEnergyInEV * ElectronCharge;

        for (int i = 0; i < numberOfElectrons; i++)
        {
            float kineticEnergyPerElectron = totalKineticEnergyInJoules / numberOfElectrons;
            float speed = Mathf.Sqrt((2 * kineticEnergyPerElectron) / ElectronMass);
            velocities.Add(speed);
        }

        return velocities;
    }

    public static float CalculateElectronEnergyContribution(string orbital, float speed, float totalKineticEnergyInEV)
    {
        float kineticEnergyInEV = totalKineticEnergyInEV / speed; // Adjust as needed for actual physics
        return kineticEnergyInEV;
    }
}

public class GlobalPhysics : MonoBehaviour
{

}

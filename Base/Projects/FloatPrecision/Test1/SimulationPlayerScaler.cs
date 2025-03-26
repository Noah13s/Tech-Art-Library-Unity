using UnityEngine;

public class SimulationPlayerScaler : MonoBehaviour
{
    // This represents the player's simulation scale value (e.g., a height of 1.8e+02 in simulation units)
    [SerializeField] float simulationPlayerScale = 1.0f;
    // Conversion factor (simulation to Unity units, e.g. 1e-6 means 1,000,000 simulation units = 1 Unity unit)
    [SerializeField] float conversionFactor = 1e-6f;

    void Start()
    {
        // Scale the container to reflect the simulation scale.
        transform.localScale = Vector3.one * simulationPlayerScale * conversionFactor;
    }
}

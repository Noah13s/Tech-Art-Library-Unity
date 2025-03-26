using UnityEngine;

// Attach this script to any object that represents a huge simulation object,
// such as your Earth sphere. Fill in the real-world simulation values in the Inspector.
public class SimulationObject : MonoBehaviour
{
    // The object's position in your simulation (e.g., Earth's center, if simulation origin is at 0,0,0).
    public Vector3 simulationPosition;
    // The object's scale in simulation units (for Earth, 1.2742e+07).
    public float simulationScale = 1f;
}

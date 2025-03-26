using UnityEngine;

public class ScaleAndDistanceFaker : MonoBehaviour
{
    [SerializeField] float conversionFactor = 1e-6f;
    [SerializeField] Transform referencePoint; // Player or Camera

    // Keep a running offset for world movement
    public Vector3 worldOffset;

    void LateUpdate()
    {
        // Instead of resetting position, combine the offset with the reference position
        transform.position = referencePoint.position + worldOffset;

        // Update each simulation object based on its simulation data (same as before)
        foreach (SimulationObject simObj in GetComponentsInChildren<SimulationObject>())
        {
            Vector3 simPos = simObj.simulationPosition;
            if (referencePoint != null)
            {
                simPos -= referencePoint.position;
            }
            simObj.transform.localPosition = simPos * conversionFactor;
            simObj.transform.localScale = Vector3.one * simObj.simulationScale * conversionFactor;
        }
    }
}

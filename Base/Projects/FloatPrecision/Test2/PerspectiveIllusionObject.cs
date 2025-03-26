using UnityEngine;

public class PerspectiveIllusionObject : MonoBehaviour
{
    public Vector3 simulationPosition;
    public float simulationScale = 1f;
    public float maxDistanceFromPlayer = 10000;
    public FloatPrecisionPlayer player;

    void Update()
    {
        Vector3 relativePosition = simulationPosition - player.playerPosition;
        float actualDistance = relativePosition.magnitude;

        if (actualDistance > maxDistanceFromPlayer)
        {
            // Position at max distance from visual center (0,0,0)
            Vector3 direction = relativePosition.normalized;
            transform.position = direction * maxDistanceFromPlayer;

            // Scale down based on actual distance
            transform.localScale = Vector3.one * simulationScale * (maxDistanceFromPlayer / actualDistance);
        }
        else
        {
            // Use relative position and full scale
            transform.position = relativePosition;
            transform.localScale = Vector3.one * simulationScale;
        }
    }
}
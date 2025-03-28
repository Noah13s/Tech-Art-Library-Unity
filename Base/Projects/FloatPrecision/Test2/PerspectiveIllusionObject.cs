using UnityEngine;
using System;
using UnityEngine.Events;

public class PerspectiveIllusionObject : MonoBehaviour
{
    public DoubleVector3 simulationPosition;
    public double simulationScale = 1f;  // This is the original planet scale
    public float maxDistanceFromPlayer = 10000;
    public FloatPrecisionPlayer player;
    [NonSerialized] public double surfaceDistance;
    [SerializeField] private UnityEvent<Int64> altitude;
    [SerializeField] private UnityEvent<Int64> centerDistance;

    void Update()
    {
        DoubleVector3 relativePosition = simulationPosition - player.playerPosition;
        double actualDistance = relativePosition.Magnitude();

        // Use the original simulation scale to calculate the radius
        double objectRadius = simulationScale * 0.5;

        // Calculate surface distance, allowing it to go negative
        surfaceDistance = actualDistance - objectRadius;

        altitude?.Invoke((long)surfaceDistance);
        centerDistance?.Invoke((long)actualDistance);

        if (surfaceDistance > maxDistanceFromPlayer)
        {
            DoubleVector3 direction = relativePosition.Normalized();
            DoubleVector3 newPos = direction * (maxDistanceFromPlayer + objectRadius);
            transform.position = (Vector3)newPos;

            // Scale factor is based on maxDistanceFromPlayer, but using simulationScale as reference
            float scaleFactor = (float)simulationScale * ((float)maxDistanceFromPlayer / (float)surfaceDistance);
            transform.localScale = Vector3.one * scaleFactor;
        }
        else
        {
            // Keep the transform position relative to the simulation
            transform.position = (Vector3)relativePosition;

            // Ensure the object is always scaled based on simulationScale
            transform.localScale = Vector3.one * (float)simulationScale;
        }
    }
}

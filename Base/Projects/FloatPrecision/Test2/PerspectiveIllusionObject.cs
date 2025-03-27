using UnityEngine;

using System;

public class PerspectiveIllusionObject : MonoBehaviour
{
    public DoubleVector3 simulationPosition;
    public double simulationScale = 1f;
    public float maxDistanceFromPlayer = 10000;
    public FloatPrecisionPlayer player;
    [NonSerialized] public double surfaceDistance;

    void Update()
    {
        DoubleVector3 relativePosition = simulationPosition - player.playerPosition;
        double actualDistance = relativePosition.Magnitude();

        // Calculate the distance from the object's surface.
        float objectRadius = transform.localScale.x * 0.5f;
        surfaceDistance = actualDistance - objectRadius;

        if (surfaceDistance > maxDistanceFromPlayer)
        {
            DoubleVector3 direction = relativePosition.Normalized();
            DoubleVector3 newPos = direction * (maxDistanceFromPlayer + objectRadius);
            transform.position = (Vector3)newPos;
            float scaleFactor = (float)simulationScale * ((float)maxDistanceFromPlayer / (float)surfaceDistance);
            transform.localScale = Vector3.one * scaleFactor;
        }
        else
        {
            transform.position = (Vector3)relativePosition;
            transform.localScale = Vector3.one * (float)simulationScale;
        }
    }
}
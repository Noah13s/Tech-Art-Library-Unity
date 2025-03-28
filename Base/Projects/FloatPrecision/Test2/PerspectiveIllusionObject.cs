using UnityEngine;
using System;
using UnityEngine.Events;

public class PerspectiveIllusionObject : MonoBehaviour
{
    public DoubleVector3 simulationPosition;
    public double simulationScale = 1f;  // True planet scale (diameter)
    public float maxDistanceFromPlayer = 10000;
    public FloatPrecisionPlayer player;
    [NonSerialized] public double surfaceDistance;
    [SerializeField] private UnityEvent<Int64> altitude;
    [SerializeField] private UnityEvent<Int64> centerDistance;

    // Transition range (in world units) below maxDistanceFromPlayer over which we blend to true mode.
    public float transitionRange = 100f;

    void Update()
    {
        // Calculate the true relative position from the player to the planet's simulation center.
        DoubleVector3 relativePosition = simulationPosition - player.playerPosition;
        double actualDistance = relativePosition.Magnitude();

        // True radius (planet's radius based on simulationScale, which represents the full diameter)
        double objectRadius = simulationScale * 0.5;

        // Surface distance is defined as distance from the planet's surface (can be negative).
        surfaceDistance = actualDistance - objectRadius;
        altitude?.Invoke((long)surfaceDistance);
        centerDistance?.Invoke((long)actualDistance);

        // --- Compute far mode (illusion) values ---
        // In far mode, we force the planet to appear at a fixed distance:
        double fixedDistance = maxDistanceFromPlayer + objectRadius;
        // Far mode position is the planet's direction scaled to fixedDistance.
        DoubleVector3 farPosition = relativePosition.Normalized() * fixedDistance;
        // Far mode scale is computed so that the planet's apparent size remains constant:
        // (apparent size ~ scale/distance). We want farScale / fixedDistance == simulationScale / actualDistance.
        float farScale = (float)(simulationScale * (fixedDistance / actualDistance));

        // --- Near mode values (true mode) ---
        DoubleVector3 nearPosition = relativePosition;
        float nearScale = (float)simulationScale;

        // --- Determine blending factor ---
        // When surfaceDistance is greater than maxDistanceFromPlayer, we want full far mode (t = 0).
        // When surfaceDistance is below (maxDistanceFromPlayer - transitionRange), we want full near mode (t = 1).
        float t;
        if (surfaceDistance >= maxDistanceFromPlayer)
        {
            t = 0f;
        }
        else if (surfaceDistance <= maxDistanceFromPlayer - transitionRange)
        {
            t = 1f;
        }
        else
        {
            // Blend linearly between these extremes.
            t = Mathf.InverseLerp(maxDistanceFromPlayer, maxDistanceFromPlayer - transitionRange, (float)surfaceDistance);
        }

        // --- Blend position and scale ---
        DoubleVector3 finalPosition = DoubleVector3.Lerp(farPosition, nearPosition, t);
        float finalScale = Mathf.Lerp(farScale, nearScale, t);

        transform.position = (Vector3)finalPosition;
        transform.localScale = Vector3.one * finalScale;
    }
}

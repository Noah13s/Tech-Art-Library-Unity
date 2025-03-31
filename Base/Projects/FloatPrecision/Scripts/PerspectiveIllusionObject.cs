using UnityEngine;
using System;
using UnityEngine.Events;

/// <summary>
/// Script that handles the unity transform of a gameobject to represent
/// a very large or far away object such as a planet.<br></br>
/// To do so it uses perspective to give the illusion of distance while 
/// staying within maximum distance from the player.
/// </summary>
public class PerspectiveIllusionObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("World position of the planet")]
    public DoubleVector3 simulationPosition;
    [Tooltip("Diameter of the planet in meters.")]
    public double simulationScale = 1f;  // True planet scale (diameter)
    [Tooltip("The maximum distance from the player the Gameobject can have.")]
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
        // In far mode, we want the object's center to be exactly maxDistanceFromPlayer from the player.
        double fixedDistance = maxDistanceFromPlayer; // use maxDistanceFromPlayer directly
        // Far mode position: along the same direction, clamped to fixedDistance.
        DoubleVector3 farPosition = relativePosition.Normalized() * fixedDistance;
        // Far mode scale: we want the apparent size (scale/distance) to remain constant.
        float farScale = (float)(simulationScale * (fixedDistance / actualDistance));

        // --- Near mode values (true mode) ---
        DoubleVector3 nearPosition = relativePosition;
        float nearScale = (float)simulationScale;

        // --- Determine blending factor ---
        // When surfaceDistance >= maxDistanceFromPlayer, use full far mode (t = 0).
        // When surfaceDistance <= maxDistanceFromPlayer - transitionRange, use full near mode (t = 1).
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
            t = Mathf.InverseLerp(maxDistanceFromPlayer, maxDistanceFromPlayer - transitionRange, (float)surfaceDistance);
        }

        // --- Blend position and scale ---
        DoubleVector3 finalPosition = DoubleVector3.Lerp(farPosition, nearPosition, t);
        float finalScale = Mathf.Lerp(farScale, nearScale, t);

        transform.position = (Vector3)finalPosition;
        transform.localScale = Vector3.one * finalScale;
    }
}

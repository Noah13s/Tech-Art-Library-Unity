using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.Rendering;

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

    [Tooltip("Surface-distance range used to blend from the compressed far representation to true local scale. Use a broad range so the handoff is imperceptible.")]
    public float transitionRange = 100f;

    [Header("Rendering")]
    [Tooltip("Real-time shadows from perspective-compressed celestial meshes are not physically valid and can cover the entire local ground patch.")]
    public bool castRealtimeShadows = false;

    private void OnEnable()
    {
        if (castRealtimeShadows)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer objectRenderer in renderers)
        {
            objectRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        CalculateRenderState(
            out DoubleVector3 renderedPosition,
            out double renderedScale,
            out double actualDistance,
            out surfaceDistance);

        altitude?.Invoke((long)surfaceDistance);
        centerDistance?.Invoke((long)actualDistance);

        transform.position = (Vector3)renderedPosition;
        transform.localScale = Vector3.one * (float)renderedScale;
    }

    /// <summary>
    /// Calculates the player-relative position and scale used to render this object.
    /// Consumers such as close-up surface patches should use this state so they remain
    /// aligned with the perspective illusion in both far and near modes.
    /// </summary>
    public void CalculateRenderState(
        out DoubleVector3 renderedPosition,
        out double renderedScale,
        out double actualDistance,
        out double distanceFromSurface)
    {
        if (player == null)
        {
            renderedPosition = new DoubleVector3(0, 0, 0);
            renderedScale = 0.0;
            actualDistance = 0.0;
            distanceFromSurface = double.PositiveInfinity;
            return;
        }

        DoubleVector3 relativePosition = simulationPosition - player.playerPosition;
        actualDistance = relativePosition.Magnitude();
        double objectRadius = simulationScale * 0.5;
        distanceFromSurface = actualDistance - objectRadius;

        if (actualDistance <= double.Epsilon)
        {
            renderedPosition = relativePosition;
            renderedScale = simulationScale;
            return;
        }

        double fixedDistance = Math.Max(0.0, maxDistanceFromPlayer);
        DoubleVector3 farPosition = relativePosition.Normalized() * fixedDistance;
        double farScale = simulationScale * (fixedDistance / actualDistance);

        // Position and scale must use exactly the same blend factor. Their ratio then
        // remains constant, so the object's apparent angular size cannot change during
        // the handoff. SmoothStep also gives the transition zero velocity at both ends.
        double blendRange = Math.Min(fixedDistance, Math.Max(0.0, transitionRange));
        double t;
        if (distanceFromSurface >= fixedDistance)
        {
            t = 0.0;
        }
        else if (blendRange <= double.Epsilon || distanceFromSurface <= fixedDistance - blendRange)
        {
            t = 1.0;
        }
        else
        {
            t = (fixedDistance - distanceFromSurface) / blendRange;
        }

        t = t * t * (3.0 - 2.0 * t);

        renderedPosition = DoubleVector3.Lerp(farPosition, relativePosition, t);
        renderedScale = farScale + (simulationScale - farScale) * t;
    }

    private void OnValidate()
    {
        simulationScale = Math.Max(0.0, simulationScale);
        maxDistanceFromPlayer = Mathf.Max(0f, maxDistanceFromPlayer);
        transitionRange = Mathf.Clamp(transitionRange, 0f, maxDistanceFromPlayer);
    }
}

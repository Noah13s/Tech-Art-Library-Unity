using System;
using UnityEngine;

[RequireComponent(typeof(AtmosphereEffect))]
[DefaultExecutionOrder(-50)]
public class AtmosphereHandler : MonoBehaviour
{
    [SerializeField] private PerspectiveIllusionObject planet;
    [SerializeField] private AtmosphereEffect atmosphereEffect;

    [Header("Stable Render Proxy")]
    [Tooltip("Maximum atmosphere proxy radius as a fraction of the planet's maximum rendered distance. Keeping this fixed near the planet prevents large-number shader precision loss.")]
    [Range(0.1f, 0.99f)]
    [SerializeField] private float stableRadiusFraction = 0.95f;

    private void Awake()
    {
        if (atmosphereEffect == null)
        {
            atmosphereEffect = GetComponent<AtmosphereEffect>();
        }
    }

    private void Start()
    {
        if (atmosphereEffect != null)
        {
            atmosphereEffect.enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (planet == null || atmosphereEffect == null)
        {
            return;
        }

        if (planet.player == null || planet.simulationScale <= double.Epsilon)
        {
            return;
        }

        DoubleVector3 centerFromPlayer = planet.simulationPosition - planet.player.playerPosition;
        double centerDistance = centerFromPlayer.Magnitude();
        if (centerDistance <= double.Epsilon)
        {
            return;
        }

        double simulationRadius = planet.simulationScale * 0.5;
        double maximumRenderDistance = Math.Max(1.0, planet.maxDistanceFromPlayer);

        // Far away, keep the atmosphere center at maxDistanceFromPlayer just like the
        // planet proxy. Near the planet, cap the proxy radius so scattering remains in
        // a stable, calibrated numeric range instead of expanding to millions of units.
        double farScale = maximumRenderDistance / centerDistance;
        double stableRadius = maximumRenderDistance * stableRadiusFraction;
        double stableScale = stableRadius / simulationRadius;
        double proxyScale = Math.Min(farScale, stableScale);

        planet.CalculateRenderState(out _, out double planetRenderDiameter, out _, out _);
        double planetRenderScale = planetRenderDiameter / planet.simulationScale;
        double renderToProxyScale = planetRenderScale > double.Epsilon
            ? proxyScale / planetRenderScale
            : 1.0;

        DoubleVector3 proxyCenter = centerFromPlayer * proxyScale;
        float proxyRadius = Mathf.Max(1f, (float)(simulationRadius * proxyScale));

        atmosphereEffect.SetCameraRelativeRenderState(
            planet.player.transform,
            (Vector3)proxyCenter,
            proxyRadius,
            Mathf.Max(float.Epsilon, (float)renderToProxyScale));
    }

    private void OnValidate()
    {
        stableRadiusFraction = Mathf.Clamp(stableRadiusFraction, 0.1f, 0.99f);
    }
}

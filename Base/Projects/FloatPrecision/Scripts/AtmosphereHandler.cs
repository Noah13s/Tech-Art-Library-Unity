using UnityEngine;

[RequireComponent(typeof(AtmosphereEffect))]
[DefaultExecutionOrder(200)]
public class AtmosphereHandler : MonoBehaviour
{
    [SerializeField] private PerspectiveIllusionObject planet;
    [SerializeField] private AtmosphereEffect atmosphereEffect;

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

        float radiusScale = planet.surfaceDistance <= planet.maxDistanceFromPlayer ? 0.993f : 1f;
        atmosphereEffect.planetRadius = planet.transform.lossyScale.x * 0.5f * radiusScale;
    }
}

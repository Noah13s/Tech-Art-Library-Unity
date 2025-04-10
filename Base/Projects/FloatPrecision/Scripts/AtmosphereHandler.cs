using System;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(AtmosphereEffect))]
public class AtmosphereHandler : MonoBehaviour
{
    [SerializeField] private PerspectiveIllusionObject planet;
    [SerializeField] private AtmosphereEffect atmosphereEffect;

    // Start is called before the first frame update
    void Awake()
    {
        // Check if TextMeshPro is available at runtime using reflection
        atmosphereEffect = GetComponent<AtmosphereEffect>();
    }

    private void Start()
    {
        atmosphereEffect.enabled = true;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (atmosphereEffect != null)
        {

            if (planet.surfaceDistance <= planet.maxDistanceFromPlayer) 
            {
                atmosphereEffect.planetRadius = (planet.transform.lossyScale.x / 2) * 0.993f;
            }
            else
            {
                atmosphereEffect.planetRadius = (planet.transform.lossyScale.x / 2);

            }

        }
    }
}

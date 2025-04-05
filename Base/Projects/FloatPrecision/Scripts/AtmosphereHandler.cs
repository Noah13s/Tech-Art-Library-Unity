using System;
using System.Reflection;
using UnityEngine;

public class AtmosphereHandler : MonoBehaviour
{
    [SerializeField] private PerspectiveIllusionObject planet;
    [SerializeField] private AtmosphereEffect atmosphereEffect;

    private Type atmosphereType;
    private object atmosphereComponent;

    private FieldInfo planetRadius;
    // Start is called before the first frame update
    void Awake()
    {
        // Check if TextMeshPro is available at runtime using reflection
        atmosphereType = Type.GetType("AtmosphereEffect, Atmosphere");

        if (atmosphereType != null)
        {
            atmosphereComponent = gameObject.GetComponent(atmosphereType);
            planetRadius = atmosphereType.GetField("planetRadius");           
        }
    }

    private void Start()
    {
        atmosphereEffect.enabled = true;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (atmosphereComponent != null)
        {
            if (planetRadius != null)
            {
                if (planet.surfaceDistance <= planet.maxDistanceFromPlayer) 
                {
                    planetRadius.SetValue(atmosphereComponent, (planet.transform.lossyScale.x / 2)*0.993f);
                }
                else
                {
                    planetRadius.SetValue(atmosphereComponent, (planet.transform.lossyScale.x / 2));
                }
            }
        }
    }
}

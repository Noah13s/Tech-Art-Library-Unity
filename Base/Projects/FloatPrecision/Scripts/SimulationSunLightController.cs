using UnityEngine;

/// <summary>
/// Aligns a directional light with the direction between two simulation-space objects.
/// The light's forward vector points from the sun toward the lighting target, while
/// its negative forward vector is the target-to-sun direction used by the atmosphere.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(Light))]
public sealed class SimulationSunLightController : MonoBehaviour
{
    [SerializeField] private PerspectiveIllusionObject sun;
    [SerializeField] private PerspectiveIllusionObject lightingTarget;
    [SerializeField] private bool setAsRenderSettingsSun = true;

    private Light directionalLight;

    private void OnEnable()
    {
        directionalLight = GetComponent<Light>();
        UpdateLightDirection();
    }

    private void LateUpdate()
    {
        UpdateLightDirection();
    }

    private void UpdateLightDirection()
    {
        if (sun == null || lightingTarget == null)
            return;

        DoubleVector3 targetToSun = sun.simulationPosition - lightingTarget.simulationPosition;
        if (targetToSun.Magnitude() <= double.Epsilon)
            return;

        Vector3 directionToSun = (Vector3)targetToSun.Normalized();
        Vector3 lightForward = -directionToSun;
        Vector3 up = Mathf.Abs(Vector3.Dot(lightForward, Vector3.up)) > 0.999f
            ? Vector3.forward
            : Vector3.up;

        transform.rotation = Quaternion.LookRotation(lightForward, up);

        if (setAsRenderSettingsSun)
        {
            directionalLight ??= GetComponent<Light>();
            RenderSettings.sun = directionalLight;
        }
    }
}

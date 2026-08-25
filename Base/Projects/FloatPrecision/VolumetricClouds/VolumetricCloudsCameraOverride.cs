using UnityEngine;

/// <summary>
/// Per-camera opt-out for the volumetric cloud renderer.
/// </summary>
[DisallowMultipleComponent]
public sealed class VolumetricCloudsCameraOverride : MonoBehaviour
{
    public bool renderClouds = true;
}

using UnityEngine;
using UnityEngine.Rendering;
using System;


[ExecuteAlways]
public class AtmosphereEffect : MonoBehaviour 
{
	public AtmosphereProfile profile;

	public Transform sun;
	public bool directional = true;

	[Min(1f)] public float planetRadius = 1000.0f;
	[Min(1f)] public float cutoffDepth = 50.0f;
	[Min(0.025f)] public float atmosphereScale = 0.25f;

	public float AtmosphereSize => (1 + atmosphereScale) * planetRadius;


	private Material material;
	private ComputeShader computeInstance;
	private RenderTexture opticalDepthTexture;
	private Transform renderOrigin;
	private Vector3 renderCenterFromOrigin;
	private float sceneDepthScale = 1f;
	private bool useCameraRelativeRenderState;


	// Values to check if optical depth texture is up to date or not. This method is a little messy but does the job.
	private int _width, _points;
	private float _scale, _rayFalloff, _mieFalloff, _hAbsorbtion;


	private void OnEnable() 
	{
		AtmosphereRenderPass.RegisterEffect(this);
	}


	// NOTE : Since Atmosphere shader has no defined properties, values must be updated every frame as they aren't stored in the property sheet.
	private void LateUpdate() 
	{
		if (material == null || sun == null || profile == null) 
		{
			return;
		}

		profile.SetProperties(material);
		ValidateOpticalDepth();

		material.SetTexture("_BakedOpticalDepth", opticalDepthTexture);
    	if (directional)
      	{	
			// For directional sun
			material.SetVector("_SunParams", -sun.forward);
			material.EnableKeyword("DIRECTIONAL_SUN");
  		} 
    	else
      	{
			// For positional sun
			material.SetVector("_SunParams", sun.position);
			material.DisableKeyword("DIRECTIONAL_SUN");
  		}

		material.SetFloat("_AtmosphereRadius", AtmosphereSize);
		material.SetFloat("_PlanetRadius", planetRadius);
		material.SetFloat("_CutoffRadius", planetRadius - cutoffDepth);
	}


	/// <summary>
	/// Supplies a numerically stable atmosphere proxy. Scene and camera distances are
	/// scaled by the same factor in the render pass, preserving all apparent angles.
	/// </summary>
	public void SetCameraRelativeRenderState(
		Transform origin,
		Vector3 centerFromOrigin,
		float radius,
		float depthScale)
	{
		renderOrigin = origin;
		renderCenterFromOrigin = centerFromOrigin;
		planetRadius = Mathf.Max(1f, radius);
		sceneDepthScale = Mathf.Max(float.Epsilon, depthScale);
		useCameraRelativeRenderState = origin != null;
	}


	internal void PrepareForCamera(Camera camera)
	{
		if (material == null || camera == null)
		{
			return;
		}

		Vector3 center = PlanetCenter;
		Vector3 cameraPosition = camera.transform.position;
		float depthScale = 1f;

		if (useCameraRelativeRenderState && renderOrigin != null)
		{
			Vector3 cameraFromOrigin = cameraPosition - renderOrigin.position;
			cameraPosition = renderOrigin.position + cameraFromOrigin * sceneDepthScale;
			depthScale = sceneDepthScale;
		}

		material.SetVector("_PlanetCenter", center);
		material.SetVector("_AtmosphereCameraPosition", cameraPosition);
		material.SetFloat("_AtmosphereSceneDepthScale", depthScale);
	}


	private Vector3 PlanetCenter => useCameraRelativeRenderState && renderOrigin != null
		? renderOrigin.position + renderCenterFromOrigin
		: transform.position;


	private void OnDisable() 
	{
		AtmosphereRenderPass.RemoveEffect(this);

		// Probably not needed. Do it just in case anyway
		if (computeInstance != null) 
		{
			DestroyImmediate(computeInstance);
		}

		if (opticalDepthTexture != null)
		{
			opticalDepthTexture.Release();
			DestroyImmediate(opticalDepthTexture);
		}
	}


	private void ValidateOpticalDepth()
	{
		if (profile == null)
		{
			return;
		}

		bool upToDate = profile.IsUpToDate(ref _width, ref _points, ref _rayFalloff, ref _mieFalloff, ref _hAbsorbtion);
		bool sizeChange = _scale != atmosphereScale;
		bool textureExists = opticalDepthTexture != null && opticalDepthTexture.IsCreated();

		if (!upToDate || sizeChange || !textureExists) 
		{
			if (computeInstance == null) 
			{
				// Create an instance per effect so multiple effects can bake their optical depth simultaneously
				computeInstance = Instantiate(profile.OpticalDepthCompute);
			}

			// Density depends on normalized altitude. Bake a unit-radius LUT once and
			// scale its optical path length in the fragment shader, making it reusable
			// for every camera-relative planet proxy radius.
			profile.BakeOpticalDepth(
				ref opticalDepthTexture,
				computeInstance,
				1f,
				1f + atmosphereScale);
			
			_scale = atmosphereScale;
		}
	}


	internal Material GetMaterial(Shader atmosphereShader) 
	{
		if (material == null)
		{
			material = new Material(atmosphereShader);
		}

		return material;
	}


	/// <summary>
	/// Is the effect sphere visible to the provided camera frustum planes?
	/// </summary>
	public bool IsVisible(Plane[] cameraPlanes) 
	{
		if (profile == null || sun == null) 
		{
			return false;
		}

		Vector3 pos = PlanetCenter;
		float radius = AtmosphereSize;

		// Cull spherical bounds, ignoring camera far plane at index 5
		for (int i = 0; i < cameraPlanes.Length - 1; i++) 
		{
			float distance = cameraPlanes[i].GetDistanceToPoint(pos);

			if (distance < 0 && Mathf.Abs(distance) > radius) 
			{
				return false;
			}
		}

		return true;
	}


	/// <summary>
	/// Returns signed distance from position to atmosphere shell
	/// </summary>
	public float DistToAtmosphere(Vector3 pos) 
	{
		return (pos - PlanetCenter).magnitude - AtmosphereSize;
	}


	private void OnDrawGizmosSelected() 
	{
		if (sun != null) 
		{
			Gizmos.color = Color.green;
			Vector3 center = PlanetCenter;
			Vector3 sunDir = directional ? -sun.forward : (sun.position - center).normalized;
			Gizmos.DrawRay(center, sunDir * planetRadius);
		}
	}
}

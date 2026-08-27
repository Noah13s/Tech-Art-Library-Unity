#ifndef URP_VOLUMETRIC_CLOUDS_SHADOWS_HLSL
#define URP_VOLUMETRIC_CLOUDS_SHADOWS_HLSL

// avoid the cloud horizontal scrolling when in camera space
#define _LOCAL_VOLUMETRIC_CLOUDS

#include "./VolumetricCloudsDefs.hlsl"
#include "./VolumetricCloudsUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"

TEXTURE2D(_VolumetricCloudsShadow);

half _ShadowCookieResolution;
float3 _CloudShadowSunOrigin;
float3 _VolumetricCloudsShadowOriginToggle;
float3 _CloudShadowSunRight;
float3 _CloudShadowSunUp;
half3 _CloudShadowSunForward;
half _ShadowIntensity;
half _ShadowOpacityFallback;
half _CloudShadowSampleCount;
float3 _CameraPositionPS;
//half _ShadowPlaneOffset;

half3 TraceVolumetricCloudsShadows(Varyings input) : SV_Target
{
    // Compute the normalized coordinate on the shadow plane
    float2 normalizedCoord = input.texcoord;

    // Compute the origin of the ray properties in the planet space
    float3 rayOriginPS = _CloudShadowSunOrigin.xyz + (normalizedCoord.x * _CloudShadowSunRight.xyz + normalizedCoord.y * _CloudShadowSunUp.xyz);
    half3 rayDirection = -_CloudShadowSunForward.xyz;

    // Compute the attenuation
    half transmittance = 1.0;
    float closestDistance = FLT_MAX;
    float farthestDistance = FLT_MIN;
    bool validShadow = false;

    float startDistance;
    float endDistance;
    if (IntersectCloudVolume(
        rayOriginPS,
        rayDirection,
        _LowestCloudAltitude,
        _HighestCloudAltitude,
        startDistance,
        endDistance))
    {
        float totalDistance = max(0.0, endDistance - startDistance);
        int sampleCount = clamp((int)round(_CloudShadowSampleCount), 6, 32);
        float stepSize = totalDistance * rcp(max((float)sampleCount, 1.0));

        // Use stable midpoint integration. The cookie itself is snapped to its
        // texel grid, so density samples remain stationary in world/light space
        // instead of swimming with sub-pixel camera motion.
        for (int i = 0; i < 32; ++i)
        {
            if (i >= sampleCount)
                break;

            float dist = startDistance + stepSize * (i + 0.5);
            float3 positionPS = rayOriginPS + rayDirection * dist;

            CloudProperties cloudProperties;
            EvaluateCloudProperties(positionPS, 0.0, 0.0, true, true, cloudProperties);

            if (cloudProperties.density > CLOUD_DENSITY_TRESHOLD)
            {
                closestDistance = min(closestDistance, dist);
                farthestDistance = max(farthestDistance, dist);
                const half3 currentStepExtinction = exp(-_ScatteringTint.xyz * cloudProperties.density * cloudProperties.sigmaT * stepSize);
                transmittance *= Luminance(currentStepExtinction);
                validShadow = true;
            }
        }
    }
    // If we didn't manage to hit a non null density, we need to fix the distances
    //half4 result = validShadow ? half4(1.0 / closestDistance, lerp(1.0 - _ShadowIntensity, 1.0, transmittance), 1.0 / farthestDistance, 1.0) : half4(0.0, 1.0, 0.0, 0.0);
    
    //half shadows = lerp(1.0 - _ShadowIntensity, 1.0, transmittance);
    half shadows = lerp(1.0 - _ShadowIntensity, 1.0, saturate(transmittance - 0.05));

    // Feather the finite local cookie into neutral sunlight. This prevents a
    // visible square when the camera approaches the coverage boundary.
    float edgeDistance = min(
        min(normalizedCoord.x, 1.0 - normalizedCoord.x),
        min(normalizedCoord.y, 1.0 - normalizedCoord.y));
    half edgeWeight = smoothstep(0.0, 0.075, edgeDistance);
    shadows = lerp(1.0, shadows, edgeWeight);

    /*
    // Evaluate the shadow
    float2 distances = validShadow ? float2(closestDistance, farthestDistance) : float2(0.0, 0.0);

    // This should be the world position of the shading point in object's shader.
    // We use a user defined shadow receiving plane here.
    float3 positionWS = float3(_VolumetricCloudsShadowOriginToggle.x, _ShadowPlaneOffset, _VolumetricCloudsShadowOriginToggle.z);

    // Compute the vector from the shadow origin to the point to shade
    //float3 shadowOriginVec = positionWS - _VolumetricCloudsShadowOriginToggle.xyz;
    //float zCoord = dot(shadowOriginVec, _CloudShadowSunForward.xyz);
    //float zCoord = _ShadowPlaneOffset - _VolumetricCloudsShadowOriginToggle.y;
    float2 lowCloudsIntersections;
    IntersectRaySphere(positionWS - _PlanetCenterPosition, light.forward, _PlanetaryRadius + _VolumetricCloudsBottomAltitude, lowCloudsIntersections);
    float zCoord = lowCloudsIntersections.x;

    half shadowRange = saturate((zCoord - distances.x) / (distances.y - distances.x));
    shadows = shadows != 1.0 ? lerp(shadows, 1.0, shadowRange) : 1.0;
    */

    half3 result = validShadow ? shadows.xxx : half3(1.0, 1.0, 1.0);

    return result;
}

half gaussian(half radius, half sigma)
{
    half v = radius / sigma;
    return exp(-v * v);
}

half3 FilterVolumetricCloudsShadow(Varyings input) : SV_Target
{
    float2 normalizedCoord = input.texcoord;

    // We let the sampler handle clamping to border.
    if (normalizedCoord.x <= _BlitTexture_TexelSize.x || normalizedCoord.y <= _BlitTexture_TexelSize.y || normalizedCoord.x >= 1.0 - _BlitTexture_TexelSize.x || normalizedCoord.y >= 1.0 - _BlitTexture_TexelSize.y)
        return _ShadowOpacityFallback.xxx;

    // Loop through the neighborhood
    half3 shadowSum = 0.0;
    half3 weightSum = 0.0;
    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            half r = sqrt(x * x + y * y);
            half weight = gaussian(r, 0.9);

            // Read the shadow data from the texture
            float2 offsetUV = normalizedCoord + float2(x, y) * _BlitTexture_TexelSize.xy;
            half3 shadowData = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, s_linear_clamp_sampler, offsetUV, 0).xyz;

            // here, we only take into account shadow distance data if transmission is not 1.0
            /*if (shadowData.y != 1.0)
            {
                shadowSum.xz += weight * shadowData.xz;
                weightSum.xz += weight;
            }
            */
            shadowSum.xyz += weight * shadowData.xyz;
            weightSum.xyz += weight;
        }
    }
    /*
    if (any(weightSum.xz == 0.0))
    {
        half3 shadowData = SAMPLE_TEXTURE2D_LOD(_BlitTexture, s_linear_clamp_sampler, normalizedCoord, 0).xyz;
        shadowSum = shadowData;
        weightSum = 1.0;
    }
    */

    // Normalize and return the result
    return shadowSum / weightSum;
}

#endif

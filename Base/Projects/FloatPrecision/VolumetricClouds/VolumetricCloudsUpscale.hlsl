#ifndef URP_VOLUMETRIC_CLOUDS_UPSCALE_HLSL
#define URP_VOLUMETRIC_CLOUDS_UPSCALE_HLSL

// Cloud radiance is already integrated (premultiplied by extinction) and the
// alpha channel stores transmittance. Filter both together so partially
// transparent silhouettes cannot pull in unrelated sky or opaque cloud data.
half CloudOpacity(half4 cloudSample)
{
    return saturate(1.0h - cloudSample.a);
}

half CloudSpatialWeight(float2 offsetInTexels, half sigma)
{
    return exp2(-dot(offsetInTexels, offsetInTexels) * sigma);
}

half CloudRangeWeight(half centerOpacity, half neighborOpacity, half sharpness)
{
    half difference = centerOpacity - neighborOpacity;
    return exp2(-difference * difference * sharpness);
}

half4 SanitizeCloudSample(half4 cloudSample)
{
    cloudSample.a = saturate(cloudSample.a);

    // Tiny residual radiance in an otherwise transparent sample is a common
    // source of isolated dark/bright edge pixels after temporal accumulation.
    if (CloudOpacity(cloudSample) < (1.0h / 1024.0h))
        return half4(0.0h, 0.0h, 0.0h, 1.0h);

    return cloudSample;
}

half StabilizeSubpixelCoverage(half opacity)
{
    // Suppress isolated, very low-coverage Monte Carlo hits without clipping
    // ordinary translucent cloud edges. The smooth toe avoids the hard popping
    // caused by a binary alpha threshold.
    return opacity * smoothstep(0.015h, 0.10h, opacity);
}

half4 BilateralUpscale(float2 screenUV)
{
    float2 textureSize = _VolumetricCloudsLightingTexture_TexelSize.zw;
    float2 texelSize = _VolumetricCloudsLightingTexture_TexelSize.xy;
    float2 sourcePosition = screenUV * textureSize - 0.5;
    float2 sourceBase = floor(sourcePosition);
    float2 sourceFraction = sourcePosition - sourceBase;

    half4 center = SAMPLE_TEXTURE2D_X_LOD(
        _VolumetricCloudsLightingTexture,
        s_linear_clamp_sampler,
        screenUV,
        0).rgba;
    half centerOpacity = CloudOpacity(center);

    half filteredOpacity = 0.0h;
    half3 filteredLighting = 0.0h;
    half lightingWeight = 0.0h;
    half totalWeight = 0.0h;

    // The original implementation used the same spatial distance for every
    // tap and a singular 1/colorDifference weight. A single stochastic ray
    // sample could therefore dominate an entire 7x7 footprint. This compact
    // 5x5 Gaussian kernel has a real per-tap distance and an opacity range
    // term, which preserves silhouettes while averaging Monte Carlo noise.
    UNITY_UNROLL
    for (int y = -2; y <= 2; ++y)
    {
        UNITY_UNROLL
        for (int x = -2; x <= 2; ++x)
        {
            float2 tapOffset = float2(x, y);
            float2 tapUV = (sourceBase + tapOffset + 0.5) * texelSize;
            half4 tap = SAMPLE_TEXTURE2D_X_LOD(
                _VolumetricCloudsLightingTexture,
                s_linear_clamp_sampler,
                tapUV,
                0).rgba;

            float2 distanceFromPixel = tapOffset - sourceFraction + 0.5;
            half spatialWeight = CloudSpatialWeight(distanceFromPixel, 0.72h);
            half rangeWeight = CloudRangeWeight(centerOpacity, CloudOpacity(tap), 3.0h);
            half weight = max(spatialWeight * rangeWeight, 1e-4h);

            half tapOpacity = CloudOpacity(tap);
            // Opacity represents pixel coverage, so reconstruct it with the
            // spatial kernel. A center-sample range test would preserve the
            // original binary hit/miss pattern as visible dots.
            filteredOpacity += tapOpacity * spatialWeight;
            if (tapOpacity > (1.0h / 1024.0h))
            {
                half radianceWeight = weight * tapOpacity;
                filteredLighting += (tap.rgb * rcp(tapOpacity)) * radianceWeight;
                lightingWeight += radianceWeight;
            }
            totalWeight += spatialWeight;
        }
    }

    half resultOpacity = StabilizeSubpixelCoverage(
        filteredOpacity * rcp(max(totalWeight, 1e-4h)));
    if (lightingWeight < 1e-4h)
        return half4(0.0h, 0.0h, 0.0h, 1.0h);
    half3 resultLighting = filteredLighting * rcp(max(lightingWeight, 1e-4h));
    return SanitizeCloudSample(half4(resultLighting * resultOpacity, 1.0h - resultOpacity));
}

// A small same-resolution reconstruction filter is used before temporal
// accumulation as well. It removes isolated ray-march outliers without the
// broad blur caused by increasing primary steps or using a large spatial blur.
half4 EdgeAwareCloudFilter(float2 screenUV)
{
    half4 center = SAMPLE_TEXTURE2D_X_LOD(
        _VolumetricCloudsLightingTexture,
        s_linear_clamp_sampler,
        screenUV,
        0).rgba;
    half centerOpacity = CloudOpacity(center);

    // Estimate the locally coherent coverage before trusting the center ray.
    // A lone hit/miss at a thin boundary is an outlier, whereas a real contour
    // has high neighborhood variance and keeps the center as its reference.
    half opacityMoment1 = 0.0h;
    half opacityMoment2 = 0.0h;
    half opacityWeight = 0.0h;

    UNITY_UNROLL
    for (int estimateY = -1; estimateY <= 1; ++estimateY)
    {
        UNITY_UNROLL
        for (int estimateX = -1; estimateX <= 1; ++estimateX)
        {
            float2 estimateOffset = float2(estimateX, estimateY);
            half4 estimateTap = SAMPLE_TEXTURE2D_X_LOD(
                _VolumetricCloudsLightingTexture,
                s_linear_clamp_sampler,
                screenUV + estimateOffset * _VolumetricCloudsLightingTexture_TexelSize.xy,
                0).rgba;
            half estimateOpacity = CloudOpacity(estimateTap);
            half estimateWeight = CloudSpatialWeight(estimateOffset, 0.85h);
            opacityMoment1 += estimateOpacity * estimateWeight;
            opacityMoment2 += estimateOpacity * estimateOpacity * estimateWeight;
            opacityWeight += estimateWeight;
        }
    }

    half inverseOpacityWeight = rcp(max(opacityWeight, 1e-4h));
    half neighborhoodOpacity = opacityMoment1 * inverseOpacityWeight;
    half opacityVariance = max(
        opacityMoment2 * inverseOpacityWeight - neighborhoodOpacity * neighborhoodOpacity,
        0.0h);
    half neighborhoodCoherence = 1.0h - saturate(opacityVariance * 5.0h);
    half outlierStrength = saturate(abs(centerOpacity - neighborhoodOpacity) * 3.5h)
        * neighborhoodCoherence;
    // Always give the coherent neighbourhood a small vote. This turns a
    // stochastic binary hit/miss boundary into sub-pixel coverage instead of
    // a dotted contour, while the stronger outlier term removes lone samples.
    half edgeStrength = saturate(opacityVariance * 8.0h);
    half coverageReconstruction = lerp(0.65h, 0.35h, edgeStrength);
    coverageReconstruction = saturate(coverageReconstruction + outlierStrength * 0.5h);
    half referenceOpacity = lerp(centerOpacity, neighborhoodOpacity, coverageReconstruction);

    half filteredOpacity = 0.0h;
    half3 filteredLighting = 0.0h;
    half lightingWeight = 0.0h;
    half totalWeight = 0.0h;
    half3 centerLighting = centerOpacity > (1.0h / 1024.0h)
        ? center.rgb * rcp(centerOpacity)
        : 0.0h;
    half centerLuminance = dot(centerLighting, half3(0.2126h, 0.7152h, 0.0722h));

    UNITY_UNROLL
    for (int y = -2; y <= 2; ++y)
    {
        UNITY_UNROLL
        for (int x = -2; x <= 2; ++x)
        {
            float2 offset = float2(x, y);
            half4 tap = SAMPLE_TEXTURE2D_X_LOD(
                _VolumetricCloudsLightingTexture,
                s_linear_clamp_sampler,
                screenUV + offset * _VolumetricCloudsLightingTexture_TexelSize.xy,
                0).rgba;
            half spatialWeight = CloudSpatialWeight(offset, 0.82h);
            half rangeWeight = CloudRangeWeight(referenceOpacity, CloudOpacity(tap), 0.85h);
            half weight = max(spatialWeight * rangeWeight, 1e-4h);

            half tapOpacity = CloudOpacity(tap);
            filteredOpacity += tapOpacity * spatialWeight;
            if (tapOpacity > (1.0h / 1024.0h))
            {
                half3 tapLighting = tap.rgb * rcp(tapOpacity);
                half tapLuminance = dot(tapLighting, half3(0.2126h, 0.7152h, 0.0722h));
                half relativeLuminanceDifference = abs(tapLuminance - centerLuminance)
                    * rcp(max(max(tapLuminance, centerLuminance), 0.05h));
                half detailWeight = centerOpacity > (1.0h / 1024.0h)
                    ? exp2(-relativeLuminanceDifference * relativeLuminanceDifference * 2.0h)
                    : 1.0h;
                half radianceWeight = weight * detailWeight * tapOpacity;
                filteredLighting += tapLighting * radianceWeight;
                lightingWeight += radianceWeight;
            }
            totalWeight += spatialWeight;
        }
    }

    half resultOpacity = StabilizeSubpixelCoverage(
        filteredOpacity * rcp(max(totalWeight, 1e-4h)));
    if (lightingWeight < 1e-4h)
        return half4(0.0h, 0.0h, 0.0h, 1.0h);
    half3 resultLighting = filteredLighting * rcp(max(lightingWeight, 1e-4h));
    return SanitizeCloudSample(half4(resultLighting * resultOpacity, 1.0h - resultOpacity));
}

half BilateralUpscaleTransmittance(float2 screenUV)
{
    return BilateralUpscale(screenUV).a;
}

#endif

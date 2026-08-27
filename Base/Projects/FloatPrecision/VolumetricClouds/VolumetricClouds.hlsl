#ifndef URP_VOLUMETRIC_CLOUDS_HLSL
#define URP_VOLUMETRIC_CLOUDS_HLSL

#include "./VolumetricCloudsDefs.hlsl"
#include "./VolumetricCloudsUtilities.hlsl"

CloudRay BuildCloudsRay(float2 screenUV, float depth, float3 invViewDirWS, bool isOccluded)
{
    CloudRay ray;

#ifdef _LOCAL_VOLUMETRIC_CLOUDS
    ray.originWS = GetCameraPositionWS();
#else
    ray.originWS = float3(0.0, 0.0, 0.0);
#endif

    ray.direction = invViewDirWS;

    // Compute the max cloud ray length
    // For opaque objects, we only care about clouds in front of them.
#ifdef _LOCAL_VOLUMETRIC_CLOUDS
    // The depth may from a high-res texture which isn't ideal but can save performance.
    float distance = LinearEyeDepth(depth, _ZBufferParams) * rcp(dot(ray.direction, -UNITY_MATRIX_V[2].xyz));
    ray.maxRayLength = lerp(MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE, distance, isOccluded);
#else
    ray.maxRayLength = MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE;
#endif

    ray.integrationNoise = GenerateRandomFloat(screenUV);

    return ray;
}

VolumetricRayResult TraceVolumetricRay(CloudRay cloudRay)
{
    // Initiliaze the volumetric ray
    VolumetricRayResult volumetricRay;
    volumetricRay.scattering = 0.0;
    volumetricRay.ambient = 0.0;
    volumetricRay.transmittance = 1.0;
    volumetricRay.meanDistance = FLT_MAX;
    volumetricRay.invalidRay = true;

    // Determine if ray intersects bounding volume, if the ray does not intersect the cloud volume AABB, skip right away
    RayMarchRange rayMarchRange;
    if (GetCloudVolumeIntersection(cloudRay.originWS, cloudRay.direction, rayMarchRange))
    {
        if (cloudRay.maxRayLength >= rayMarchRange.start)
        {
            // Initialize the depth for accumulation
            volumetricRay.meanDistance = 0.0;

            // Total distance that the ray must travel including empty spaces
            // Clamp the travel distance to whatever is closer
            // - Sky Occluder
            // - Volume end
            // - Far plane
            float totalDistance = min(rayMarchRange.end, cloudRay.maxRayLength) - rayMarchRange.start;

            // Compute the environment lighting that is going to be used for the cloud evaluation
            float3 rayMarchStartPS = ConvertToPS(cloudRay.originWS) + rayMarchRange.start * cloudRay.direction;
            float3 rayMarchEndPS = rayMarchStartPS + totalDistance * cloudRay.direction;

            // Tracking the number of steps that have been made
            int currentIndex = 0;

            // Normalization value of the depth
            float meanDistanceDivider = 0.0;

            // Subdivide the complete cloud traversal into a predictable set of
            // physical intervals. The primary-step setting is a budget, while the
            // requested physical step determines how many of those samples are
            // actually useful. If the budget cannot satisfy the requested step we
            // still cover the full traversal instead of silently clipping clouds.
            float baseStepS = min(_BaseStepSize, _MaxStepSize);
            float desiredMaxStepS = min(
                _MaxStepSize,
                baseStepS + rayMarchRange.start * _AdaptiveStepSizeFactor);
            int sampleBudget = max(1, (int)_NumPrimarySteps);
            int minimumSamples = min(sampleBudget, 24);
            int requiredSamples = max(
                1,
                (int)ceil(totalDistance * rcp(max(desiredMaxStepS, 0.001))));
            int sampleCount = clamp(requiredSamples, minimumSamples, sampleBudget);

            // Uniform world-distance intervals spend too much of the fixed budget
            // at the horizon and make every near-camera interval a visible slab.
            // Distribute interval lengths geometrically from the requested near
            // step to the physical maximum. The complete traversal is still
            // covered, but the samples that occupy many screen pixels receive the
            // highest physical resolution.
            float stepRatio = max(1.0, _MaxStepSize * rcp(max(baseStepS, 0.001)));
            float geometricRate = sampleCount > 1
                ? pow(stepRatio, rcp((float)(sampleCount - 1)))
                : 1.0;
            float geometricSum = abs(geometricRate - 1.0) > 0.00001
                ? baseStepS * (pow(geometricRate, (float)sampleCount) - 1.0)
                    * rcp(geometricRate - 1.0)
                : baseStepS * sampleCount;
            float stepDistributionScale = totalDistance * rcp(max(geometricSum, 0.001));
            // Keep one independently scrambled shadow phase for the complete
            // view ray. Changing it at every primary step makes adjacent samples
            // receive unrelated four-tap shadow estimates, which integrates into
            // alternating bright/dark slabs. This phase still varies per pixel and
            // frame, so temporal accumulation converges without spatial banding.
            float rayLightJitter = frac(
                cloudRay.integrationNoise * 0.754877666 + 0.569840296);
            float currentDistance = 0.0;

            // Initialize the values for the optimized ray marching
            bool activeSampling = true;
            int sequentialEmptySamples = 0;
            half3 reconstructedSunLuminance = 0.0h;
            half reconstructedAmbientLuminance = 0.0h;
            half lightingReconstructionValid = 0.0h;
            half reconstructedDensity = 0.0h;
            half reconstructedSigmaT = 0.0h;
            half reconstructedAmbientOcclusion = 1.0h;
            half densityReconstructionValid = 0.0h;

            // Do the ray march for every step that we can.
            while (currentIndex < sampleCount && currentDistance < totalDistance)
            {
                float stepS = baseStepS
                    * stepDistributionScale
                    * pow(geometricRate, (float)currentIndex);
                stepS = min(stepS, totalDistance - currentDistance);

                // Shift the complete sampling lattice by one spatially and
                // temporally varying phase. Keeping the same phase along a ray is
                // important: independently jittering every interval leaves fixed
                // interval boundaries whose averaged lighting still reads as
                // slices. A moving lattice turns those slices into stationary,
                // zero-mean sampling noise that temporal reconstruction can remove.
                float primaryJitter = cloudRay.integrationNoise;
                float sampleDistance = min(
                    currentDistance + primaryJitter * stepS,
                    totalDistance);

                // Compute the camera-distance based attenuation
                float densityAttenuationValue = DensityFadeValue(rayMarchRange.start + sampleDistance);
                // Compute the mip offset for the erosion texture
                float erosionMipOffset = ErosionMipOffset(rayMarchRange.start + sampleDistance);

                // Accumulate in WS and convert at each iteration to avoid precision issues
                float3 samplePositionWS = cloudRay.originWS
                    + (rayMarchRange.start + sampleDistance) * cloudRay.direction;
                float3 currentPositionPS = ConvertToPS(samplePositionWS);

                // Should we be evaluating the clouds or just doing the large ray marching
                if (activeSampling)
                {
                    // If the density is null, we can skip as there will be no contribution.
                    // Do not collapse an antithetic pair to its midpoint here: that
                    // produces a stable, visible lighting plane at every interval.
                    CloudProperties properties;
                    EvaluateCloudProperties(currentPositionPS, 0.0, erosionMipOffset, false, false, properties);
                    properties.density *= densityAttenuationValue;

                    // Reconstruct fractional interval coverage with a second
                    // density-only probe half a stratum away. This prevents one
                    // thresholded lookup from turning an entire long interval
                    // into an opaque sheet. Lighting remains a single stochastic
                    // evaluation, so this is much cheaper than doubling either
                    // primary steps or the four-tap shadow march.
                    float coverageJitter = frac(primaryJitter + 0.5);
                    float coverageDistance = min(
                        currentDistance + coverageJitter * stepS,
                        totalDistance);
                    float coverageCameraDistance = rayMarchRange.start + coverageDistance;
                    float3 coveragePositionWS = cloudRay.originWS
                        + coverageCameraDistance * cloudRay.direction;
                    CloudProperties coverageProperties;
                    EvaluateCloudProperties(
                        ConvertToPS(coveragePositionWS),
                        0.0,
                        ErosionMipOffset(coverageCameraDistance),
                        false,
                        false,
                        coverageProperties);
                    coverageProperties.density *= DensityFadeValue(coverageCameraDistance);

                    properties.density = 0.5h
                        * (properties.density + coverageProperties.density);
                    properties.ambientOcclusion = 0.5h
                        * (properties.ambientOcclusion + coverageProperties.ambientOcclusion);
                    properties.sigmaT = 0.5h
                        * (properties.sigmaT + coverageProperties.sigmaT);

                    // Reconstruct the thresholded density signal continuously
                    // along the ray. Without this, one binary-like lookup is held
                    // over the complete physical interval and becomes a visible
                    // opaque slice. The reconstruction radius is tied to physical
                    // step length rather than an arbitrary screen-space blur.
                    half rawDensity = properties.density;
                    if (densityReconstructionValid > 0.5h)
                    {
                        half densityResponse = (half)saturate(
                            stepS * rcp(stepS + 2.5 * max((float)_BaseStepSize, 0.001)));
                        densityResponse = max(densityResponse, 0.14h);
                        properties.density = lerp(
                            reconstructedDensity,
                            rawDensity,
                            densityResponse);

                        // Empty probes should decay the reconstructed coverage,
                        // not zero the extinction coefficient in the same step.
                        if (rawDensity <= CLOUD_DENSITY_TRESHOLD)
                        {
                            properties.sigmaT = reconstructedSigmaT;
                            properties.ambientOcclusion = reconstructedAmbientOcclusion;
                        }
                    }

                    reconstructedDensity = properties.density;
                    if (rawDensity > CLOUD_DENSITY_TRESHOLD)
                    {
                        reconstructedSigmaT = properties.sigmaT;
                        reconstructedAmbientOcclusion = properties.ambientOcclusion;
                    }
                    densityReconstructionValid = 1.0h;

                    if (properties.density > CLOUD_DENSITY_TRESHOLD)
                    {
                        // Contribute to the average depth (must be done first in case we end up inside a cloud at the next step)
                        half transmitanceXdensity = volumetricRay.transmittance * properties.density;
                        volumetricRay.meanDistance += (rayMarchRange.start + sampleDistance) * transmitanceXdensity;
                        meanDistanceDivider += transmitanceXdensity;

                        // Evaluate the cloud at the position
                        EvaluateCloud(
                            properties,
                            cloudRay.direction,
                            currentPositionPS,
                            stepS,
                            sampleDistance / totalDistance,
                            rayLightJitter,
                            reconstructedSunLuminance,
                            reconstructedAmbientLuminance,
                            lightingReconstructionValid,
                            volumetricRay);

                        // if most of the energy is absorbed, just leave.
                        if (volumetricRay.transmittance < 0.003)
                        {
                            volumetricRay.transmittance = 0.0;
                            break;
                        }

                        // Reset the empty sample counter
                        sequentialEmptySamples = 0;
                    }
                    else
                    {
                        sequentialEmptySamples++;
                        if (sequentialEmptySamples >= EMPTY_STEPS_BEFORE_LARGE_STEPS)
                        {
                            lightingReconstructionValid = 0.0h;
                            densityReconstructionValid = 0.0h;
                        }
                    }

                    // If it has been more than EMPTY_STEPS_BEFORE_LARGE_STEPS, disable active sampling and start large steps
                    if (sequentialEmptySamples == EMPTY_STEPS_BEFORE_LARGE_STEPS)
                        activeSampling = false;

                    // Do the next step
                    currentDistance += stepS;

                }
                else
                {
                    lightingReconstructionValid = 0.0h;
                    densityReconstructionValid = 0.0h;
                    CloudProperties properties;
                    EvaluateCloudProperties(currentPositionPS, 1.0, 0.0, true, false, properties);

                    // Apply the fade in function to the density
                    properties.density *= densityAttenuationValue;

                    // If the density is lower than our tolerance,
                    if (properties.density < CLOUD_DENSITY_TRESHOLD)
                    {
                        currentDistance += stepS * 2.0;
                    }
                    else
                    {
                        // Somewhere between this step and the previous clouds started
                        // We reset all the counters and enable active sampling
                        currentDistance = max(0.0, currentDistance - stepS);
                        currentIndex -= 1;
                        activeSampling = true;
                        sequentialEmptySamples = 0;
                    }
                }
                currentIndex++;
            }

            // Normalized the depth we computed
            if (volumetricRay.meanDistance != 0.0)
            {
                volumetricRay.invalidRay = false;
                volumetricRay.meanDistance /= meanDistanceDivider;
                volumetricRay.meanDistance = min(volumetricRay.meanDistance, cloudRay.maxRayLength);

                float3 currentPositionPS = ConvertToPS(cloudRay.originWS) + volumetricRay.meanDistance * cloudRay.direction;
                float relativeHeight = EvaluateNormalizedCloudHeight(currentPositionPS);

                Light sun = GetMainLight();

                // Evaluate the sun color at the position
            #ifdef _PHYSICALLY_BASED_SUN
                half3 sunColor = _SunColor * EvaluateSunColorAttenuation(currentPositionPS, sun.direction, true) * _SunLightDimmer; // _SunColor includes PI
            #else
                half3 sunColor = sun.color * PI * _SunLightDimmer;
            #endif

                // Evaluate the environement lighting contribution
            #ifdef _CLOUDS_AMBIENT_PROBE
                half3 radialUp = normalize(currentPositionPS);
                half3 ambientTermTop = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsAmbientProbe, sampler_VolumetricCloudsAmbientProbe, radialUp, 4.0).rgb;
                half3 ambientTermBottom = SAMPLE_TEXTURECUBE_LOD(_VolumetricCloudsAmbientProbe, sampler_VolumetricCloudsAmbientProbe, -radialUp, 4.0).rgb;
            #else
                half3 radialUp = normalize(currentPositionPS);
                half3 ambientTermTop = EvaluateVolumetricCloudsAmbientProbe(radialUp);
                half3 ambientTermBottom = EvaluateVolumetricCloudsAmbientProbe(-radialUp);
            #endif
                half3 probeAmbient = max(
                    0,
                    lerp(ambientTermBottom, ambientTermTop, relativeHeight)
                        * _AmbientProbeDimmer);

                // FloatPrecision renders its atmosphere after opaque geometry as
                // a fullscreen effect, so a dynamic reflection probe can contain
                // only black space even on the daylight side of the planet. Keep
                // valid probe lighting, but provide the missing low-frequency sky
                // irradiance from the local sun elevation. This prevents dense
                // near-ground clouds becoming black silhouettes without flattening
                // their self-shadowing or eliminating the night-side transition.
                half sunElevation = dot(radialUp, sun.direction);
                half daylight = smoothstep(-0.16, 0.24, sunElevation);
                half horizonLight = 1.0 - abs(saturate(sunElevation));
                half3 skyFallback = lerp(
                    half3(0.012, 0.020, 0.038),
                    half3(0.30, 0.40, 0.54),
                    daylight);
                skyFallback += half3(0.11, 0.075, 0.035)
                    * horizonLight
                    * daylight;
                skyFallback *= _AmbientProbeDimmer;
                half3 ambient = max(probeAmbient, skyFallback);

                volumetricRay.scattering = sunColor * volumetricRay.scattering;
                volumetricRay.scattering += ambient * volumetricRay.ambient;
            }
        }
    }
    return volumetricRay;
}

#endif

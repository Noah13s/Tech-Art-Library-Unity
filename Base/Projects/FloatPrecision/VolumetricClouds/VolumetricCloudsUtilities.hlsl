#ifndef URP_VOLUMETRIC_CLOUDS_UTILITIES_HLSL
#define URP_VOLUMETRIC_CLOUDS_UTILITIES_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

half3 EvaluateVolumetricCloudsAmbientProbe(half3 normalWS)
{
    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1(normalWS, clouds_SHAr, clouds_SHAg, clouds_SHAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(normalWS, clouds_SHBr, clouds_SHBg, clouds_SHBb, clouds_SHC);

    return res;
}

// From HDRP: VolumetricCloudsUtilities.hlsl

// The number of octaves for the multi-scattering
#define NUM_MULTI_SCATTERING_OCTAVES 2
#define PHASE_FUNCTION_STRUCTURE half2
// Global offset to the high frequency noise
#define CLOUD_DETAIL_MIP_OFFSET 0.0
// Global offset for reaching the LUT/AO
#define CLOUD_LUT_MIP_OFFSET 1.0
// Size of Preset LUT (unused since it's not a compute shader)
#define CLOUD_MAP_LUT_PRESET_SIZE 64.0
// Density below wich we consider the density is zero (optimization reasons)
#define CLOUD_DENSITY_TRESHOLD 0.001
// Number of steps before we start the large steps
#define EMPTY_STEPS_BEFORE_LARGE_STEPS 8
// Forward eccentricity
#define FORWARD_ECCENTRICITY 0.76
// A weaker backward lobe preserves dark cloud bases without producing a flat halo.
#define BACKWARD_ECCENTRICITY 0.28
// Distance until which the erosion texture is used
#define MIN_EROSION_DISTANCE 3000.0
#define MAX_EROSION_DISTANCE 100000.0
// Value that is used to normalize the noise textures
#define NOISE_TEXTURE_NORMALIZATION_FACTOR 100000.0
// Maximal distance until which the "skybox"
#define MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE 200000.0 //FLT_MAX
// Maximal size of a light step
#define LIGHT_STEP_MAXIMAL_SIZE 1000.0

// The planet center position
#define _PlanetCenterPosition _PlanetCenterRadius.xyz
#define ConvertToPS(x) (x - _PlanetCenterPosition)

// Structure that holds all the data required for the cloud ray marching
struct CloudRay
{
    // Origin of the ray in camera-relative space
    float3 originWS;
    // Direction of the ray in world space
    float3 direction;
    // Maximal ray length before hitting the far plane or an occluder
    float maxRayLength;
    // Integration Noise
    float integrationNoise;
};

// Structure that holds the result of our volumetric ray
struct VolumetricRayResult
{
    // Amount of lighting that reach the clouds
    // We keep track of sun light and ambient light separately for optimization
    // They are combine at the end of tracing
    half3 scattering;
    half ambient;
    // Transmittance through the clouds
    half transmittance;
    // Mean distance of the clouds
    float meanDistance;
    // Flag that defines if the ray is valid or not
    bool invalidRay;
};

// Perceptual blending
half EvaluateFinalTransmittance(half3 sceneColor, half transmittance)
{
    // Due to the high intensity of the sun, we often need apply the transmittance in a tonemapped space
    // As we only produce one transmittance, we evaluate the approximation on the luminance of the color
    half luminance = Luminance(sceneColor * _PostExposure);

    if (luminance > 0.0)
    {
        // Apply the transmittance in tonemapped space
        half resultLuminance = luminance * rcp(1.0 + luminance) * transmittance;
        resultLuminance = resultLuminance * rcp(1.0 - resultLuminance);

        // By softening the transmittance attenuation curve for pixels adjacent to cloud boundaries when the luminance is super high,  
        // We can prevent sun flicker and improve perceptual blending. (https://www.desmos.com/calculator/vmly6erwdo)
        half finalTransmittance = max(resultLuminance * rcp(luminance), pow(transmittance, 6));

        // This approach only makes sense if the color is not black
        transmittance = lerp(transmittance, finalTransmittance, _ImprovedTransmittanceBlend);
    }
    return saturate(transmittance);
}

// These 2 functions were moved to the Core RP package by the commit below:
// "[HDRP] Optimizations and quality improvements to PBR sky"
// https://github.com/Unity-Technologies/Graphics/commit/9f7464a87cb8a09f23869dc178560bb8b072d4ca
#if UNITY_VERSION < 202330

// Use an infinite far plane
// https://chaosinmotion.com/2010/09/06/goodbye-far-clipping-plane/
// 'depth' is the linear depth (view-space Z position)
float EncodeInfiniteDepth(float depth, float near)
{
    return saturate(near / depth);
}

// 'z' is the depth encoded in the depth buffer (1 at near plane, 0 at far plane)
float DecodeInfiniteDepth(float z, float near)
{
    return near / max(z, FLT_EPS);
}

#endif

// Function that takes a world space position and converts it to a depth value
float ConvertCloudDepth(float3 position)
{
    float4 hClip = TransformWorldToHClip(position);
    return hClip.z / hClip.w;
}

float GenerateRandomFloat(float2 screenUV)
{
    float2 pixel = floor(screenUV * _ScreenSize.xy);

    // Interleaved gradient noise has much lower low-frequency clumping than a
    // per-pixel hash. That matters at cloud silhouettes: white-noise clusters
    // read as holes and black speckles even after a small reconstruction
    // filter, while IGN distributes the same number of ray samples evenly.
    pixel += float2(_Seed * 17.0, _Seed * 29.0);
    float spatialNoise = frac(52.9829189 * frac(dot(
        pixel,
        float2(0.06711056, 0.00583715))));

    // A golden-ratio temporal sequence converges more evenly than unrelated
    // white-noise frames and is independent of Time.timeScale. Cloud edges can
    // therefore denoise while gameplay is paused instead of freezing one noisy
    // ray offset indefinitely.
    return frac(spatialNoise + _CloudFrameIndex * 0.61803398875);
}

// Returns the closest hit in X and the farthest hit in Y.
// Returns a negative number if there's no intersection.
// (result.y >= 0) indicates success.
// (result.x < 0) indicates that we are inside the sphere.
float2 IntersectSphere(float sphereRadius, float cosChi,
                       float radialDistance, float rcpRadialDistance)
{
    // r_o = float2(0, r)
    // r_d = float2(sinChi, cosChi)
    // p_s = r_o + t * r_d
    //
    // R^2 = dot(r_o + t * r_d, r_o + t * r_d)
    // R^2 = ((r_o + t * r_d).x)^2 + ((r_o + t * r_d).y)^2
    // R^2 = t^2 + 2 * dot(r_o, r_d) + dot(r_o, r_o)
    //
    // t^2 + 2 * dot(r_o, r_d) + dot(r_o, r_o) - R^2 = 0
    //
    // Solve: t^2 + (2 * b) * t + c = 0, where
    // b = r * cosChi,
    // c = r^2 - R^2.
    //
    // t = (-2 * b + sqrt((2 * b)^2 - 4 * c)) / 2
    // t = -b + sqrt(b^2 - c)
    // t = -b + sqrt((r * cosChi)^2 - (r^2 - R^2))
    // t = -b + r * sqrt((cosChi)^2 - 1 + (R/r)^2)
    // t = -b + r * sqrt(d)
    // t = r * (-cosChi + sqrt(d))
    //
    // Why do we do this? Because it is more numerically robust.

    float d = Sq(sphereRadius * rcpRadialDistance) - saturate(1 - cosChi * cosChi);

    // Return the value of 'd' for debugging purposes.
    return (d < 0) ? d : (radialDistance * float2(-cosChi - sqrt(d),
                                                  -cosChi + sqrt(d)));
}

// TODO: remove.
float2 IntersectSphere(float sphereRadius, float cosChi, float radialDistance)
{
    return IntersectSphere(sphereRadius, cosChi, radialDistance, rcp(radialDistance));
}

float ComputeCosineOfHorizonAngle(float r)
{
    float R = _EarthRadius;
    float sinHor = R * rcp(r);
    return -sqrt(saturate(1 - sinHor * sinHor));
}

// Function that interects a ray with a sphere (optimized for very large sphere), returns up to two positives distances.

// numSolutions: 0, 1 or 2 positive solves
// startWS: rayOriginWS, might be camera positionWS
// dir: normalized ray direction
// radius: planet radius
// result: the distance of hitPos, which means the value of solves
int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;
    result = 0.0;
    int numSolutions = 0;
    if (d >= 0.0)
    {
        // Compute the values required for the solution eval
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        result = float2(c / q, q / a);
        // Remove the solutions we do not want
        numSolutions = 2;
        if (result.x < 0.0)
        {
            numSolutions--;
            result.x = result.y;
        }
        if (result.y < 0.0)
            numSolutions--;
    }
    // Return the number of solutions
    return numSolutions;
}

// Returns true if the ray exits the cloud volume (doesn't intersect earth)
// The ray is supposed to start inside the volume
bool ExitCloudVolume(float3 originPS, half3 dir, float higherBoundPS, out float tExit)
{
    // Given that we are inside the volume, we are guaranteed to exit at the outer bound
    float radialDistance = length(originPS);
    float cosChi = dot(originPS, dir) * rcp(radialDistance);
    tExit = IntersectSphere(higherBoundPS, cosChi, radialDistance, rcp(radialDistance)).y;

    // If the ray intersects the earth, then the sun is occluded by the earth
    return cosChi >= ComputeCosineOfHorizonAngle(radialDistance);
}

struct RayMarchRange
{
    // The start of the range
    float start;
    // The length of the range
    float end;
};

// Returns true if the ray intersects the cloud volume
// Outputs the entry and exit distance from the volume
bool IntersectCloudVolume(float3 originPS, half3 dir, float lowerBoundPS, float higherBoundPS, out float tEntry, out float tExit)
{
    bool intersect;
    float radialDistance = length(originPS);
    float rcpRadialDistance = rcp(radialDistance);
    float cosChi = dot(originPS, dir) * rcpRadialDistance;
    float2 tInner = IntersectSphere(lowerBoundPS, cosChi, radialDistance, rcpRadialDistance);
    float2 tOuter = IntersectSphere(higherBoundPS, cosChi, radialDistance, rcpRadialDistance);

    if (tInner.x < 0.0 && tInner.y >= 0.0) // Below the lower bound
    {
        // The ray starts at the intersection with the lower bound and ends at the intersection with the outer bound
        tEntry = tInner.y;
        tExit = tOuter.y;
        // We don't see the clouds if they are behind Earth
        intersect = cosChi >= ComputeCosineOfHorizonAngle(radialDistance);
    }
    else // Inside or above the cloud volume
    {
        // The ray starts at the intersection with the outer bound, or at 0 if we are inside
        // The ray ends at the lower bound if we hit it, at the outer bound otherwise
        tEntry = max(tOuter.x, 0.0f);
        tExit = tInner.x >= 0.0 ? tInner.x : tOuter.y;
        // We don't see the clouds if we don't hit the outer bound
        intersect = tOuter.y >= 0.0f;
    }

    return intersect;
}

bool GetCloudVolumeIntersection(float3 originWS, half3 dir, out RayMarchRange rayMarchRange)
{
#ifdef _LOCAL_VOLUMETRIC_CLOUDS
    return IntersectCloudVolume(ConvertToPS(originWS), dir, _LowestCloudAltitude, _HighestCloudAltitude, rayMarchRange.start, rayMarchRange.end);
#else
    {
        ZERO_INITIALIZE(RayMarchRange, rayMarchRange);

        // intersect with all three spheres
        float2 intersectionInter, intersectionOuter;
        int numInterInner = RaySphereIntersection(originWS, dir, _LowestCloudAltitude, intersectionInter);
        int numInterOuter = RaySphereIntersection(originWS, dir, _HighestCloudAltitude, intersectionOuter);

        // The ray starts at the first intersection with the lower bound and goes up to the first intersection with the outer bound
        rayMarchRange.start = intersectionInter.x;
        rayMarchRange.end = intersectionOuter.x;

        // Return if we have an intersection
        return true;
    }
#endif
}

struct CloudProperties
{
    // Normalized float that tells the "amount" of clouds that is at a given location
    half density;
    // Ambient occlusion for the ambient probe
    half ambientOcclusion;
    // Normalized value that tells us the height within the cloud volume (vertically)
    float height;
    // Extinction over the interval
    half sigmaT;
};

// Global attenuation of the density based on the camera distance
half DensityFadeValue(float distanceToCamera)
{
    return saturate((distanceToCamera - _FadeInStart) * rcp(max(_FadeInDistance, 0.0001)));
}

// Evaluate the erosion mip offset based on the camera distance
float ErosionMipOffset(float distanceToCamera)
{
    return lerp(0.0, 4.0, saturate((distanceToCamera - MIN_EROSION_DISTANCE) * rcp(MAX_EROSION_DISTANCE - MIN_EROSION_DISTANCE)));
}

// Function that returns the normalized height inside the cloud layer
float EvaluateNormalizedCloudHeight(float3 positionPS)
{
    return RangeRemap(_LowestCloudAltitude, _HighestCloudAltitude, length(positionPS));
}

// Animation of the cloud shape position
float3 AnimateShapeNoisePosition(float3 positionPS)
{
    // We reduce the top-view repetition of the pattern
    positionPS.y += (positionPS.x / 3.0 + positionPS.z / 7.0);
    // We add the contribution of the wind displacements
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _MediumWindSpeed + float3(0.0, _VerticalShapeWindDisplacement, 0.0);
    //return positionPS;
}

// Animation of the cloud erosion position
float3 AnimateErosionNoisePosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _SmallWindSpeed + float3(0.0, _VerticalErosionWindDisplacement, 0.0);
    //return positionPS;
}

// Structure that holds all the data used to define the cloud density of a point in space
struct CloudCoverageData
{
    // From a top down view, in what proportions this pixel has clouds
    half coverage;
    // From a top down view, in what proportions this pixel has clouds
    half rainClouds;
    // Value that allows us to request the cloudtype using the density
    half cloudType;
    // Maximal cloud height
    half maxCloudHeight;
    // Low-frequency, seamless shape used when the whole planet is visible.
    half planetaryShape;
    // Weather-derived tendency to build tall cumulus and anvil structures.
    half convection;
    // Per-sample blend from nearby 3D detail to the planet-wide weather field.
    half detailLod;
};

half EvaluatePlanetDetailLod(float3 positionPS)
{
    float3 cameraPS = ConvertToPS(GetCameraPositionWS());
    float distanceToCamera = distance(positionPS, cameraPS);
    float fadeRange = max(_PlanetDetailFadeEnd - _PlanetDetailFadeStart, 0.0001);
    half distanceLod = smoothstep(
        _PlanetDetailFadeStart,
        _PlanetDetailFadeStart + fadeRange,
        distanceToCamera);
    return max(_PlanetViewLod, distanceLod);
}

// Decode the planet-wide weather map once and use the exact same macro envelope
// for both the ray-marched volume and the orbital shell. None of these signals
// are view-LOD dependent: detail LOD is allowed to change internal 3D structure,
// but must never move a weather-system boundary while the camera approaches it.
void DecodePlanetaryWeather(
    half3 weather,
    out half coverage,
    out half shape,
    out half convection)
{
    half threshold = 1.0 - _PlanetaryCoverage;
    half softness = lerp(0.20, 0.055, _PlanetaryCoverageContrast);
    coverage = smoothstep(
        threshold - softness,
        threshold + softness,
        weather.x);

    half shapeMask = smoothstep(0.27, 0.72, weather.y);
    half fineDetail = smoothstep(0.22, 0.80, weather.z);
    shape = saturate(
        pow(shapeMask, 1.15)
        * lerp(0.32, 1.0, fineDetail)
        * lerp(0.18, 1.0, weather.y));

    convection = saturate(
        smoothstep(0.48, 0.82, weather.z) * 0.70
        + smoothstep(0.62, 0.90, weather.y) * 0.30);
}

// Samples the unique, procedurally generated global weather field once around
// the planet. The texture wraps only at longitude and clamps at the poles.
void EvaluatePlanetaryWeather(
    float3 positionPS,
    out half coverage,
    out half shape,
    out half convection)
{
    // Global weather is advanced once through the longitudinal offset below.
    // Adding the local-noise wind displacement here as well made the planet-wide
    // field shear and travel twice as fast, especially in compressed orbit views.
    float3 planetDirection = normalize(positionPS);
    float2 weatherUV = float2(
        atan2(planetDirection.z, planetDirection.x) * 0.159154943 + 0.5 + _PlanetaryWeatherOffset,
        asin(clamp(planetDirection.y, -1.0, 1.0)) * 0.318309886 + 0.5);

    half3 weather = SAMPLE_TEXTURE2D_LOD(
        _PlanetaryWeatherMap,
        sampler_PlanetaryWeatherMap,
        weatherUV,
        0.0).rgb;

    DecodePlanetaryWeather(weather, coverage, shape, convection);
}

struct OrbitalCloudShellResult
{
    half3 scattering;
    half transmittance;
    float distance;
    bool valid;
};

// A single analytic layer preserves the planet-wide weather silhouette when the
// perspective-compressed volumetric layer becomes thinner than a useful ray step.
// It lives in this full-screen pass so scene depth, atmosphere composition and
// per-camera cloud opt-outs remain identical to the nearby volumetric path.
OrbitalCloudShellResult EvaluateOrbitalCloudShell(CloudRay cloudRay)
{
    OrbitalCloudShellResult result;
    ZERO_INITIALIZE(OrbitalCloudShellResult, result);
    result.transmittance = 1.0;
    result.distance = FLT_MAX;
    result.valid = false;

    float3 rayOriginPS = ConvertToPS(cloudRay.originWS);
    float radialDistance = length(rayOriginPS);
    if (radialDistance <= 0.0001)
        return result;

    float shellRadius = lerp(_LowestCloudAltitude, _HighestCloudAltitude, 0.62);
    float inverseRadialDistance = rcp(radialDistance);
    float cosChi = dot(rayOriginPS, cloudRay.direction) * inverseRadialDistance;
    float2 intersections = IntersectSphere(
        shellRadius,
        cosChi,
        radialDistance,
        inverseRadialDistance);

    float hitDistance = intersections.x >= 0.0 ? intersections.x : intersections.y;
    if (hitDistance < 0.0 || hitDistance > cloudRay.maxRayLength)
        return result;

    float3 hitPositionPS = rayOriginPS + cloudRay.direction * hitDistance;
    // Use the unthresholded weather channels here. The red channel establishes
    // synoptic coverage while green cuts it into translucent fronts and wisps;
    // feeding the already-thresholded volumetric outputs into a second shell
    // threshold produces the large opaque "cloud continents" seen previously.
    float3 weatherDirection = normalize(hitPositionPS);
    float2 weatherUV = float2(
        atan2(weatherDirection.z, weatherDirection.x) * 0.159154943
            + 0.5 + _PlanetaryWeatherOffset,
        asin(clamp(weatherDirection.y, -1.0, 1.0)) * 0.318309886 + 0.5);
    half3 weather = SAMPLE_TEXTURE2D_LOD(
        _PlanetaryWeatherMap,
        sampler_PlanetaryWeatherMap,
        weatherUV,
        0.0).rgb;

    half synopticMask;
    half structuredShape;
    half convection;
    DecodePlanetaryWeather(
        weather,
        synopticMask,
        structuredShape,
        convection);
    half density = synopticMask * structuredShape;
    if (density <= CLOUD_DENSITY_TRESHOLD)
        return result;

    half3 shellNormal = normalize(hitPositionPS);
    half viewPath = rcp(max(abs(dot(shellNormal, -cloudRay.direction)), 0.38));
    half opticalDepth = density * _OrbitalProxyOpacity * viewPath;
    half opacity = saturate(1.0 - exp(-opticalDepth));
    if (opacity <= CLOUD_DENSITY_TRESHOLD)
        return result;

    Light sun = GetMainLight();
    half rawNdotL = dot(shellNormal, sun.direction);
    half NdotL = saturate(rawNdotL);
    half dayVisibility = smoothstep(-0.08, 0.20, rawNdotL);
    half backScattering = pow(
        saturate(dot(-cloudRay.direction, sun.direction)),
        8.0) * 0.16 * dayVisibility;

#if defined(_PHYSICALLY_BASED_SUN)
    half3 sunColor = _SunColor * _SunLightDimmer;
#else
    // The shell is an albedo proxy, not a multiple-scattering integration. Using
    // the volumetric path's PI radiance multiplier clips all RG density variation
    // to white before it can blend over the planet.
    half3 sunColor = sun.color * _SunLightDimmer;
#endif

    half directLight = (0.18 + 0.82 * sqrt(NdotL)) * dayVisibility
        + backScattering;
    half nightAmbient = _OrbitalProxyAmbient * lerp(0.18, 1.0, dayVisibility);
    half selfShadow = lerp(1.0, 0.72, density);
    half3 shellColor = _OrbitalProxyTint.rgb
        * (nightAmbient + sunColor * directLight)
        * selfShadow;

    result.scattering = shellColor * opacity;
    result.transmittance = 1.0 - opacity;
    result.distance = hitDistance;
    result.valid = true;
    return result;
}

// Function that evaluates the coverage data for a given point in planet space
void GetCloudCoverageData(float3 positionPS, out CloudCoverageData data)
{
    // Convert the position into dome space and center the texture is centered above (0, 0, 0)
    //float2 normalizedPosition = AnimateCloudMapPosition(positionPS).xz / _NormalizationFactor * _CloudMapTiling.xy + _CloudMapTiling.zw - 0.5;
//#if defined(CLOUDS_SIMPLE_PRESET)
    half planetaryCoverage;
    half planetaryShape;
    half convection;
    half detailLod = EvaluatePlanetDetailLod(positionPS);
    EvaluatePlanetaryWeather(
        positionPS,
        planetaryCoverage,
        planetaryShape,
        convection);

    half coverageInfluence = lerp(_PlanetaryCoverageInfluence, 1.0, detailLod);
    half4 cloudMapData = half4(lerp(0.9, planetaryCoverage, coverageInfluence), 0.0, 0.25, 1.0);
//#else
    //float4 cloudMapData = SAMPLE_TEXTURE2D_LOD(_CloudMapTexture, s_linear_repeat_sampler, float2(normalizedPosition), 0);
//#endif
    data.coverage = cloudMapData.x;
    data.rainClouds = cloudMapData.y;
    data.cloudType = cloudMapData.z;
    data.maxCloudHeight = cloudMapData.w;
    data.planetaryShape = planetaryShape;
    data.convection = convection;
    data.detailLod = detailLod;
}

// Texture-free value noise is used only for the silhouette-defining frequencies.
// Unlike the imported 3D textures it does not repeat after one texture period, so
// a flight across a cloud bank cannot reveal a tiled grid. The texture volumes are
// retained later as inexpensive, high-frequency edge erosion.
float HashCloudCell(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float NonPeriodicNoise3D(float3 p)
{
    float3 cell = floor(p);
    float3 local = frac(p);
    local = local * local * (3.0 - 2.0 * local);

    float n000 = HashCloudCell(cell + float3(0.0, 0.0, 0.0));
    float n100 = HashCloudCell(cell + float3(1.0, 0.0, 0.0));
    float n010 = HashCloudCell(cell + float3(0.0, 1.0, 0.0));
    float n110 = HashCloudCell(cell + float3(1.0, 1.0, 0.0));
    float n001 = HashCloudCell(cell + float3(0.0, 0.0, 1.0));
    float n101 = HashCloudCell(cell + float3(1.0, 0.0, 1.0));
    float n011 = HashCloudCell(cell + float3(0.0, 1.0, 1.0));
    float n111 = HashCloudCell(cell + float3(1.0, 1.0, 1.0));

    float n00 = lerp(n000, n100, local.x);
    float n10 = lerp(n010, n110, local.x);
    float n01 = lerp(n001, n101, local.x);
    float n11 = lerp(n011, n111, local.x);
    return lerp(lerp(n00, n10, local.y), lerp(n01, n11, local.y), local.z);
}

float3 RotateCloudCoordinates(float3 p)
{
    return float3(
        dot(p, float3(0.00, 0.80, 0.60)),
        dot(p, float3(-0.80, 0.36, -0.48)),
        dot(p, float3(-0.60, -0.48, 0.64)));
}

half EvaluateProceduralCloudShape(
    float3 baseCoordinates,
    half height,
    half convection,
    bool cheapVersion)
{
    float3 macroCoordinates = baseCoordinates * _MacroShapeScale;
    float macroA = NonPeriodicNoise3D(macroCoordinates * 0.43 + float3(7.1, 19.7, 3.4));
    float macroB = NonPeriodicNoise3D(
        RotateCloudCoordinates(macroCoordinates) * 0.91 + float3(31.8, 5.2, 17.6));
    float macroShape = smoothstep(0.28, 0.78, macroA * 0.58 + macroB * 0.42);

    float3 warp = (float3(macroA, macroB, macroA + macroB) - float3(0.5, 0.5, 1.0))
        * (0.85 * _LocalShapeVariation);
    float systemVariation = smoothstep(0.16, 0.84, macroA * 0.61 + macroB * 0.39);
    float3 cumulusCoordinates = RotateCloudCoordinates(baseCoordinates)
        * _CumulusScale + warp + float3(13.2, 47.1, 8.7);
    // Irrationally related, rotated value-noise octaves define a non-periodic
    // cloud envelope.  The extra high octave is subtractive: it cuts turbulent
    // pockets into the envelope instead of adding another layer of round blobs.
    float billowA = NonPeriodicNoise3D(cumulusCoordinates);
    float billowLarge = NonPeriodicNoise3D(
        RotateCloudCoordinates(cumulusCoordinates) * 0.57
            + float3(23.4, 71.8, 5.9));
    float billowBody = lerp(billowLarge, billowA, systemVariation);
    float billows = billowBody;

    if (!cheapVersion)
    {
        float billowB = NonPeriodicNoise3D(
            RotateCloudCoordinates(cumulusCoordinates) * 1.93
                + float3(53.7, 11.9, 29.4));
        float billowC = NonPeriodicNoise3D(
            RotateCloudCoordinates(cumulusCoordinates * 3.71 + float3(9.3, 61.4, 22.8)));
        float billowD = NonPeriodicNoise3D(
            RotateCloudCoordinates(cumulusCoordinates.yzx * 7.13 + float3(41.7, 3.8, 73.1)));
        billows = saturate(
            billowBody * 0.54
            + billowB * 0.30
            + billowC * 0.16
            - (1.0 - billowD) * (0.15 * _DetailStrength));

        // The reference-quality cloud set uses a ~2.3 km Worley base with high
        // persistence. Keep that cellular character local to the procedural
        // envelope and decorrelate two samples so the 3D texture can never become
        // a planet-wide repeating silhouette.
        float3 worleyCoordinatesA = RotateCloudCoordinates(cumulusCoordinates)
            * 0.83 + float3(0.17, 0.53, 0.89);
        float3 worleyCoordinatesB = RotateCloudCoordinates(cumulusCoordinates.yzx)
            * 1.71 + float3(0.61, 0.29, 0.97);
        half worleyA = SAMPLE_TEXTURE3D_LOD(
            _Worley128RGBA,
            s_trilinear_repeat_sampler,
            worleyCoordinatesA,
            0.45).r;
        half worleyB = SAMPLE_TEXTURE3D_LOD(
            _Worley128RGBA,
            s_trilinear_repeat_sampler,
            worleyCoordinatesB,
            1.15).r;
        half cellularDetail = saturate(worleyA * 0.62 + worleyB * 0.38);
        half cellularErosion = saturate((0.64 - cellularDetail) * 2.2);
        billows = saturate(
            billows - cellularErosion * (0.24 * _DetailStrength));
        billows = saturate(billows * 1.18);
    }

    // The reference implementation describes several cloud types with the same
    // ~2.3 km base scale but different altitude envelopes. Reconstruct that
    // principle procedurally: weather convection continuously selects stratus,
    // cumulus and congestus rather than stretching every cloud through the full
    // layer. This is what prevents the horizon from becoming a wall of spheres.
    half developedConvection = saturate(convection * _VerticalDevelopment);
    half localTypeSignal = smoothstep(0.24, 0.78, macroB * 0.58 + macroA * 0.42);
    half cumulusType = saturate(
        localTypeSignal * lerp(0.58, 0.26, developedConvection)
        + developedConvection);
    half congestusType = smoothstep(
        0.62,
        0.90,
        developedConvection * 0.76 + localTypeSignal * 0.24);
    half macroSupport = smoothstep(0.24, 0.78, macroShape);

    half stratusProfile = smoothstep(0.015, 0.075, height)
        * (1.0 - smoothstep(0.30, 0.46, height));
    half cumulusTop = saturate(
        lerp(0.40, 0.76, cumulusType)
        * lerp(0.82, 1.12, systemVariation));
    half cumulusProfile = smoothstep(0.018, 0.10, height)
        * (1.0 - smoothstep(cumulusTop - 0.20, cumulusTop, height));
    half congestusProfile = smoothstep(0.04, 0.16, height)
        * (1.0 - smoothstep(0.78, 0.98, height));

    half stratusNoise = billows * 0.62 + macroShape * 0.38;
    half stratus = smoothstep(0.40, 0.64, stratusNoise)
        * stratusProfile
        * lerp(1.0, 0.28, cumulusType);

    half cumulusNoise = billows * 0.76 + macroShape * 0.24;
    half cumulus = smoothstep(0.44, 0.64, cumulusNoise)
        * cumulusProfile
        * lerp(0.24, 1.0, macroSupport)
        * lerp(0.68, 1.0, cumulusType);

    half towerCore = smoothstep(
        0.57,
        0.75,
        billowA * 0.48 + billows * 0.30 + macroShape * 0.22)
        * congestusProfile
        * lerp(0.16, 1.0, macroSupport)
        * congestusType;
    half anvilProfile = smoothstep(0.67, 0.78, height)
        * (1.0 - smoothstep(0.90, 1.0, height));
    half anvil = smoothstep(0.46, 0.70, macroShape * 0.62 + billowA * 0.38)
        * anvilProfile
        * congestusType;

    return saturate(stratus * 0.38 + cumulus + towerCore * 0.82 + anvil * 0.34);
}

// Density remapping function
half DensityRemap(half x, half a, half b, half c, half d)
{
    return (((x - a) * rcp(b - a)) * (d - c)) + c;
}

// Horizon zero dawn technique to darken the clouds
half PowderEffect(half cloudDensity, half cosAngle, half intensity)
{
    half powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

// Function that evaluates the cloud properties at a given absolute world space position
void EvaluateCloudProperties(float3 positionPS, float noiseMipOffset, float erosionMipOffset, bool cheapVersion, bool lightSampling,
                            out CloudProperties properties)
{
    // Initliaze all the values to 0 in case
    ZERO_INITIALIZE(CloudProperties, properties);

//#ifndef CLOUDS_SIMPLE_PRESET
    // When using a cloud map, we cannot support the full planet due to UV issues
//#endif

    // Remove global clouds below the horizon
#ifndef _LOCAL_VOLUMETRIC_CLOUDS
    if (positionPS.y < _EarthRadius)
        return;
#endif


    // By default the ambient occlusion is 1.0
    properties.ambientOcclusion = 1.0;

    // Evaluate the normalized height of the position within the cloud volume
    properties.height = EvaluateNormalizedCloudHeight(positionPS);

    // When rendering in camera space, we still want horizontal scrolling
#ifndef _LOCAL_VOLUMETRIC_CLOUDS
    positionPS.xz += _WorldSpaceCameraPos.xz;
#endif

    // Evaluate planet-wide coverage before the local noise. The macro pattern is
    // stable on the sphere and becomes the dominant silhouette from orbit.
    CloudCoverageData cloudCoverageData;
    GetCloudCoverageData(positionPS, cloudCoverageData);

    // If this region of space has no cloud coverage, exit right away.
    if (cloudCoverageData.coverage.x <= CLOUD_DENSITY_TRESHOLD || cloudCoverageData.maxCloudHeight < properties.height)
        return;

    // Evaluate the generic sampling coordinates
    float3 baseNoiseSamplingCoordinates = float3(AnimateShapeNoisePosition(positionPS).xzy / NOISE_TEXTURE_NORMALIZATION_FACTOR) * _ShapeScale - float3(_ShapeNoiseOffset.x, _ShapeNoiseOffset.y, _VerticalShapeNoiseOffset);

    // Evaluate the coordinates at which the noise will be sampled and apply wind displacement
    baseNoiseSamplingCoordinates += properties.height * float3(_WindDirection.x, _WindDirection.y, 0.0f) * _AltitudeDistortion;

    half detailLod = cloudCoverageData.detailLod;
    half proceduralShape = EvaluateProceduralCloudShape(
        baseNoiseSamplingCoordinates,
        properties.height,
        cloudCoverageData.convection,
        cheapVersion);

    // Read from the LUT
//#if defined(CLOUDS_SIMPLE_PRESET)
    half3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudCurveTexture, s_linear_repeat_sampler, half2(0.0, properties.height), 0).xyz;
//#else
    //half3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudLutTexture, s_linear_repeat_sampler, float2(cloudCoverageData.cloudType, properties.height), CLOUD_LUT_MIP_OFFSET).xyz;
//#endif

    // Adjust the shape and erosion factor based on the LUT and the coverage
    half shapeFactor = lerp(0.65, 1.0, _ShapeFactor) * densityErosionAO.y;
    half erosionFactor = _ErosionFactor * densityErosionAO.y * (1.0 - detailLod);
#if defined(_CLOUDS_MICRO_EROSION)
    half microDetailFactor = _MicroErosionFactor * densityErosionAO.y * (1.0 - detailLod);
#endif

    half weatherStrength = saturate(
        cloudCoverageData.coverage
        * lerp(0.68, 1.20, cloudCoverageData.planetaryShape));
    // Keep clear air genuinely empty, then transition quickly to optically dense
    // cloud. A low threshold made every high-coverage region a translucent fog
    // sheet; this compact remap creates readable silhouettes and internal depth.
    // The type blend and its subtractive Worley erosion lower the statistical
    // mean versus the former additive blob model. Keep the final threshold in
    // the useful part of that distribution so coherent banks survive while the
    // newly introduced high-frequency voids still cut their silhouettes.
    half shapeThreshold = lerp(0.68, 0.38, weatherStrength)
        + (1.0 - shapeFactor) * 0.06;
    half densityTransitionWidth = lerp(0.24, 0.035, _EdgeHardness);
    // A four-tap shadow ray cannot resolve the same near-binary density edge as
    // the primary ray. Sampling that edge directly makes each light probe toggle
    // between clear and opaque, exposing the primary intervals as bright/dark
    // layers. Widen only the shadow-density footprint to reconstruct the average
    // optical coverage seen by a long light interval. Camera density remains
    // crisp, while self-shadowing changes continuously at no additional samples.
    if (lightSampling)
        densityTransitionWidth = max(densityTransitionWidth, 0.34h);
    half shapedDensity = smoothstep(
        shapeThreshold,
        min(0.995, shapeThreshold + densityTransitionWidth),
        proceduralShape);
    // Both representations use the same invariant synoptic envelope. Local 3D
    // noise only modulates density inside it; it can no longer create unrelated
    // banks which disappear when the orbital shell fades in. Keeping a non-zero
    // local floor also prevents valid macro coverage from becoming a hole solely
    // because one low-frequency 3D sample happened to be below its threshold.
    half synopticEnvelope = pow(
        saturate(cloudCoverageData.planetaryShape),
        1.18)
        * pow(cloudCoverageData.coverage, 1.05);
    half distantDensity = synopticEnvelope * densityErosionAO.x;
    half localStructure = lerp(0.38, 1.28, shapedDensity);
    half volumetricDensity = distantDensity
        * localStructure
        * lerp(0.90, 1.10, cloudCoverageData.convection);

    // Detail LOD now removes local structure without changing the shared macro
    // support. At contrast/influence 1, approaching a cloud system refines it in
    // place instead of changing its planet-wide coverage.
    half base_cloud = lerp(volumetricDensity, distantDensity, detailLod);

    // Weight the ambient occlusion's contribution
    properties.ambientOcclusion = densityErosionAO.z;

    // Extinction is supplied in inverse physical metres and converted to render
    // units by C#. This removes opacity changes caused by perspective compression.
    properties.sigmaT = _ExtinctionCoefficient
        * lerp(0.82, 1.28, cloudCoverageData.convection);

    // The ambient occlusion value that is baked is less relevant if there is shaping or erosion, small hack to compensate that
    half ambientOcclusionBlend = saturate(1.0 - max(erosionFactor, shapeFactor) * 0.5);
    properties.ambientOcclusion = lerp(1.0, properties.ambientOcclusion, ambientOcclusionBlend);

    // Apply the erosion for nicer details
    if (!cheapVersion && erosionFactor > 0.0001)
    {
        float3 erosionCoords = AnimateErosionNoisePosition(positionPS)
            / NOISE_TEXTURE_NORMALIZATION_FACTOR * _ErosionScale;
        float3 erosionCoordsB = RotateCloudCoordinates(erosionCoords) * 0.873
            + float3(0.173, 0.619, 0.947);
        half erosionA = SAMPLE_TEXTURE3D_LOD(
            _ErosionNoise,
            s_linear_repeat_sampler,
            erosionCoords,
            CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset + detailLod * 4.0).x;
        half erosionB = SAMPLE_TEXTURE3D_LOD(
            _ErosionNoise,
            s_linear_repeat_sampler,
            erosionCoordsB,
            CLOUD_DETAIL_MIP_OFFSET + 0.65 + erosionMipOffset + detailLod * 4.0).x;
        half erosionNoise = 1.0 - lerp(erosionA, erosionB, 0.43);
        erosionNoise = lerp(
            0.0,
            erosionNoise,
            erosionFactor * _DetailStrength * cloudCoverageData.coverage);
        properties.ambientOcclusion = saturate(properties.ambientOcclusion - sqrt(erosionNoise * _ErosionOcclusion));
        base_cloud = DensityRemap(base_cloud, erosionNoise, 1.0, 0.0, 1.0);

        #if defined(_CLOUDS_MICRO_EROSION)
        float3 fineCoords = AnimateErosionNoisePosition(positionPS)
            / NOISE_TEXTURE_NORMALIZATION_FACTOR * _MicroErosionScale;
        half fineA = SAMPLE_TEXTURE3D_LOD(
            _ErosionNoise,
            s_linear_repeat_sampler,
            fineCoords,
            CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).x;
        half fineB = SAMPLE_TEXTURE3D_LOD(
            _ErosionNoise,
            s_linear_repeat_sampler,
            RotateCloudCoordinates(fineCoords) * 1.117 + float3(0.31, 0.79, 0.53),
            CLOUD_DETAIL_MIP_OFFSET + 0.8 + erosionMipOffset).x;
        half fineNoise = 1.0 - lerp(fineA, fineB, 0.38);
        fineNoise = lerp(
            0.0,
            fineNoise,
            microDetailFactor * 0.45 * _DetailStrength * cloudCoverageData.coverage);
        base_cloud = DensityRemap(base_cloud, fineNoise, 1.0, 0.0, 1.0);
        #endif
    }

    // Given that we are not sampling the erosion texture, we compensate by substracting an erosion value
    if (lightSampling)
    {
        base_cloud -= erosionFactor * 0.1;
        #if defined(_CLOUDS_MICRO_EROSION)
        base_cloud -= microDetailFactor * 0.15;
        #endif
    }

    // Make sure we do not send any negative values
    base_cloud = max(0, base_cloud);

    // Attenuate everything by the density multiplier
    properties.density = base_cloud * _DensityMultiplier;
}

// Function that evaluates the transmittance to the sun at a given cloud position
half3 EvaluateSunTransmittance(
    float3 positionPS,
    half3 sunDirection,
    PHASE_FUNCTION_STRUCTURE phaseFunction,
    float lightJitter)
{
    // Compute the Ray to the limits of the cloud volume in the direction of the light
    float totalLightDistance = 0.0;
    half3 transmittance = half3(0.0, 0.0, 0.0);

    // If we early out, this means we've hit the earth itself
    if (ExitCloudVolume(positionPS, sunDirection, _HighestCloudAltitude, totalLightDistance))
    {
        // Because of the very limited numebr of light steps and the potential humongous distance to cover, we decide to potnetially cover less and make it more useful
        totalLightDistance = clamp(totalLightDistance, 0, _NumLightSteps * LIGHT_STEP_MAXIMAL_SIZE);

        // Apply a small bias to compensate for the imprecision in the ray-sphere intersection at world scale.
        totalLightDistance += 5.0;

        // Compute the size of the current step
        float intervalSize = totalLightDistance * rcp((float)_NumLightSteps);
        float opticalDepth = 0;

        // Collect total density along light ray.
        for (int j = 0; j < _NumLightSteps; j++)
        {
            // Stratify the inexpensive shadow estimate independently from the
            // primary ray. The former fixed 0.25 offset produced coherent lighting
            // contours at every primary sample plane. A temporally rotated sequence
            // preserves the same cost but turns those contours into convergent noise.
            float lightSequence = frac(
                lightJitter + (j + 1) * 0.754877666);
            float intervalJitter = lerp(0.15, 0.85, lightSequence);
            float dist = intervalSize * (j + intervalJitter);

            // Evaluate the current sample point
            float3 currentSamplePointPS = positionPS + sunDirection * dist;
            // Get the cloud properties at the sample point
            CloudProperties lightRayCloudProperties;
            EvaluateCloudProperties(currentSamplePointPS, 3.0 * j / _NumLightSteps, 0.0, true, true, lightRayCloudProperties);

            opticalDepth += lightRayCloudProperties.density * lightRayCloudProperties.sigmaT;
        }

        // Compute the luminance for each octave
        // https://magnuswrenninge.com/wp-content/uploads/2010/03/Wrenninge-OzTheGreatAndVolumetric.pdf
        half3 extinction = intervalSize * opticalDepth * _ScatteringTint.xyz;
        for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
        {
            half msFactor = PositivePow(_MultiScattering, o);
            transmittance += exp(-extinction * msFactor) * (phaseFunction[o] * msFactor);
        }
    }

    return transmittance;
}

float ChapmanUpperApprox(float z, float cosTheta)
{
    float c = cosTheta;
    float n = 0.761643 * ((1 + 2 * z) - (c * c * z));
    float d = c * z + sqrt(z * (1.47721 + 0.273828 * (c * c * z)));

    return 0.5 * c + (n * rcp(d));
}

float ChapmanHorizontal(float z)
{
    float r = rsqrt(z);
    float s = z * r; // sqrt(z)

    return 0.626657 * (r + 2 * s);
}

// Default atmosphere settings of HDRP physically based sky
#if defined(PHYSICALLY_BASED_SKY)
half _AirScaleHeight;
half _AerosolScaleHeight;
half _AirDensityFalloff;
half _AerosolDensityFalloff;
//float _AtmosphericRadius;
#define _PlanetaryRadius _EarthRadius // TODO: unify earth radius control
half3 _AirSeaLevelExtinction;
half _AerosolSeaLevelExtinction;
#else
#define _AirScaleHeight 8000.0
#define _AerosolScaleHeight 1200.0
#define _AirDensityFalloff 1.0 / _AirScaleHeight
#define _AerosolDensityFalloff 1.0 / _AerosolScaleHeight
#define _PlanetaryRadius _EarthRadius
#define _AirSeaLevelExtinction (half3(5.8, 13.5, 33.1) / 1000000.0)
#define _AerosolSeaLevelExtinction 0.00001
#endif

//#define _AlphaSaturation 1.0
//#define _AlphaMultiplier 1.0

float3 ComputeAtmosphericOpticalDepth(float r, float cosTheta, bool aboveHorizon)
{
    const float2 n = float2(_AirDensityFalloff, _AerosolDensityFalloff);
    const float2 H = float2(_AirScaleHeight, _AerosolScaleHeight);
    const float  R = _PlanetaryRadius;

    float2 z = n * r;
    float2 Z = n * R;

    float sinTheta = sqrt(saturate(1 - cosTheta * cosTheta));

    float2 ch;
    ch.x = ChapmanUpperApprox(z.x, abs(cosTheta)) * exp(Z.x - z.x); // Rescaling adds 'exp'
    ch.y = ChapmanUpperApprox(z.y, abs(cosTheta)) * exp(Z.y - z.y); // Rescaling adds 'exp'

    if (!aboveHorizon) // Below horizon, intersect sphere
    {
        float sinGamma = (r / R) * sinTheta;
        float cosGamma = sqrt(saturate(1 - sinGamma * sinGamma));

        float2 ch_2;
        ch_2.x = ChapmanUpperApprox(Z.x, cosGamma); // No need to rescale
        ch_2.y = ChapmanUpperApprox(Z.y, cosGamma); // No need to rescale

        ch = ch_2 - ch;
    }
    else if (cosTheta < 0)   // Above horizon, lower hemisphere
    {
        // z_0 = n * r_0 = (n * r) * sin(theta) = z * sin(theta).
        // Ch(z, theta) = 2 * exp(z - z_0) * Ch(z_0, Pi/2) - Ch(z, Pi - theta).
        float2 z_0 = z * sinTheta;
        float2 b = exp(Z - z_0); // Rescaling cancels out 'z' and adds 'Z'
        float2 a;
        a.x = 2 * ChapmanHorizontal(z_0.x);
        a.y = 2 * ChapmanHorizontal(z_0.y);
        float2 ch_2 = a * b;

        ch = ch_2 - ch;
    }

    float2 optDepth = ch * H;

    return optDepth.x * _AirSeaLevelExtinction.xyz + optDepth.y * _AerosolSeaLevelExtinction;
}

// This function evaluates the sun color attenuation from the physically based sky
half3 EvaluateSunColorAttenuation(float3 positionPS, half3 sunDirection, bool estimatePenumbra = false)
{
    float r = length(positionPS);
    float cosTheta = dot(positionPS, sunDirection) * rcp(r); // Normalize

    // Point can be below horizon due to precision issues
    r = max(r, _PlanetaryRadius);
    float cosHoriz = ComputeCosineOfHorizonAngle(r);

    if (cosTheta >= cosHoriz) // Above horizon
    {
        float3 oDepth = ComputeAtmosphericOpticalDepth(r, cosTheta, true);
        half3 opacity = 1 - TransmittanceFromOpticalDepth(oDepth);
        half penumbra = saturate((cosTheta - cosHoriz) / 0.0019); // very scientific value
        half3 attenuation = 1 - opacity;// (Desaturate(opacity, _AlphaSaturation) * _AlphaMultiplier);
        return estimatePenumbra ? attenuation * penumbra : attenuation;
    }
    else
    {
        return 0;
    }
}

// Function that evaluates the sun color along the ray
half3 EvaluateSunColor(float3 entryEvaluationPointPS, float3 exitEvaluationPointPS, half3 sunDirection, half3 sunColor, float relativeRayDistance)
{
    // evaluate the attenuation at both points (entrance and exit of the cloud layer)
    half3 sunColor0 = sunColor * EvaluateSunColorAttenuation(entryEvaluationPointPS, sunDirection, true);
    half3 sunColor1 = sunColor * EvaluateSunColorAttenuation(exitEvaluationPointPS, sunDirection, false);

    return lerp(sunColor0, sunColor1, relativeRayDistance);
}

// Evaluates the inscattering from this position
void EvaluateCloud(CloudProperties cloudProperties, half3 rayDirection,
                float3 currentPositionPS, float stepSize, float relativeRayDistance,
                float lightJitter,
                inout half3 reconstructedSunLuminance,
                inout half reconstructedAmbientLuminance,
                inout half lightingReconstructionValid,
                inout VolumetricRayResult volumetricRay)
{
    // Apply the extinction
    const half extinction = cloudProperties.density * cloudProperties.sigmaT;
    const half transmittance = exp(-extinction * stepSize);

    Light sun = GetMainLight();
    half cosAngle = dot(rayDirection, sun.direction);

    // Evaluate the phase function for each of the octaves
    half2 phaseFunction = half2(0.0, 0.0);
    half forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0), cosAngle);
    half backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0), cosAngle);
    phaseFunction[0] = forwardP * 0.82 + backwardsP * 0.18;

#if NUM_MULTI_SCATTERING_OCTAVES >= 2
    forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1), cosAngle);
    backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1), cosAngle);
    phaseFunction[1] = forwardP * 0.72 + backwardsP * 0.28;
#endif

#if NUM_MULTI_SCATTERING_OCTAVES >= 3
    forwardP = HenyeyGreensteinPhaseFunction(FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2), cosAngle);
    backwardsP = HenyeyGreensteinPhaseFunction(-BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 2), cosAngle);
    phaseFunction[2] = forwardP + backwardsP;
#endif

    // Compute the powder effect
    half powderEffect = PowderEffect(cloudProperties.density, cosAngle, _PowderEffectIntensity);

    // Evaluate the sun visibility
    half3 sunTransmittance = EvaluateSunTransmittance(
        currentPositionPS,
        sun.direction,
        phaseFunction,
        lightJitter);

    // Four shadow samples are enough for a stochastic residual, but not for the
    // complete low-frequency lighting field: their binary density hits project
    // as contour layers. Use a stable local-column approximation as a control
    // variate and retain the raymarched result for cloud-to-cloud variation.
    // This preserves depth and temporal detail without exposing individual taps.
    half3 analyticSunTransmittance = 0.0h;
    // Do not feed the point-sampled density into the control variate: doing so
    // simply reproduces the same primary strata at higher contrast. A smooth
    // height-dependent mean occupancy supplies the stable low-frequency column;
    // local density variation remains in the stochastic residual below.
    half meanColumnOccupancy = lerp(
        0.025h,
        0.005h,
        saturate((half)cloudProperties.height));
    half localExtinction = cloudProperties.sigmaT * meanColumnOccupancy;
    half radialSunAlignment = abs(dot(normalize(currentPositionPS), sun.direction));
    float localColumnLength = lerp(2200.0, 280.0, saturate(cloudProperties.height))
        * rcp(max((float)radialSunAlignment, 0.28));
    half3 analyticExtinction = localExtinction
        * localColumnLength
        * _ScatteringTint.xyz;
    for (int analyticOctave = 0; analyticOctave < NUM_MULTI_SCATTERING_OCTAVES; ++analyticOctave)
    {
        half analyticMsFactor = PositivePow(_MultiScattering, analyticOctave);
        analyticSunTransmittance += exp(-analyticExtinction * analyticMsFactor)
            * (phaseFunction[analyticOctave] * analyticMsFactor);
    }
    sunTransmittance = lerp(
        analyticSunTransmittance,
        sunTransmittance,
        0.12h);

    // Compute luminance separately to factor out color multiplication at the end of the loop
    // Use 1 as placeholder to compute the 'transfer function'
    half forwardAlignment = saturate(cosAngle * 0.5 + 0.5);
    half silverLining = pow(forwardAlignment, 10.0)
        * _SilverLiningIntensity
        * saturate(1.0 - cloudProperties.density * 1.8);
    half3 sunLuminance = sunTransmittance
        * powderEffect
        * (1.0 + silverLining);
    half ambientLuminance = 1.0 * cloudProperties.ambientOcclusion;

    // The four-tap shadow ray is a stochastic estimate at discrete primary
    // positions. Integrating it as a constant over the full primary interval
    // exposes every sample as a bright or dark slab. Reconstruct a continuous
    // lighting field along the view ray with a physical, step-aware one-pole
    // filter. Density and extinction remain untouched, so silhouettes stay
    // sharp; only the noisy low-frequency lighting estimate is reconstructed.
    if (lightingReconstructionValid > 0.5h)
    {
        half reconstructionResponse = (half)saturate(
            stepSize * rcp(stepSize + 8.0 * max((float)_BaseStepSize, 0.001)));
        reconstructionResponse = max(reconstructionResponse, 0.08h);
        sunLuminance = lerp(
            reconstructedSunLuminance,
            sunLuminance,
            reconstructionResponse);
        ambientLuminance = lerp(
            reconstructedAmbientLuminance,
            ambientLuminance,
            reconstructionResponse);
    }

    reconstructedSunLuminance = sunLuminance;
    reconstructedAmbientLuminance = ambientLuminance;
    lightingReconstructionValid = 1.0h;

    // "Energy-conserving analytical integration"
    // See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
    // No division by clamped extinction because albedo == 1 => sigma_s == sigma_e so it simplifies
    // Note: this is not true anymore when _ScatteringTint is modified, but it still looks correct
    volumetricRay.scattering += sunLuminance     * (volumetricRay.transmittance - volumetricRay.transmittance * transmittance);
    volumetricRay.ambient    += ambientLuminance * (volumetricRay.transmittance - volumetricRay.transmittance * transmittance);
    volumetricRay.transmittance *= transmittance;
}

#endif

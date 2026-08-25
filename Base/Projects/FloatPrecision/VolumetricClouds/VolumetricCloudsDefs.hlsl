#ifndef URP_VOLUMETRIC_CLOUDS_DEFINES_HLSL
#define URP_VOLUMETRIC_CLOUDS_DEFINES_HLSL

TEXTURE2D(_PlanetaryWeatherMap);
SAMPLER(sampler_PlanetaryWeatherMap);

CBUFFER_START(UnityPerMaterial)
float _Seed;
half _NumPrimarySteps;
half _NumLightSteps;
half _MaxStepSize;
float _HighestCloudAltitude;
float _LowestCloudAltitude;
half4 _ShapeNoiseOffset;
half _VerticalShapeNoiseOffset;
half4 _WindDirection;
half4 _WindVector;
half _VerticalShapeWindDisplacement;
half _VerticalErosionWindDisplacement;
half _MediumWindSpeed;
half _SmallWindSpeed;
half _AltitudeDistortion;
float _DensityMultiplier;
half _PowderEffectIntensity;
half _ShapeScale;
half _ShapeFactor;
half _LocalShapeVariation;
half _PlanetaryCoverageScale;
half _PlanetaryCoverage;
half _PlanetaryCoverageContrast;
half _PlanetaryCoverageInfluence;
half _PlanetViewLod;
float _PlanetDetailFadeStart;
float _PlanetDetailFadeEnd;
half _OrbitalProxyOpacity;
half4 _OrbitalProxyTint;
half _OrbitalProxyAmbient;
float _PlanetaryWeatherOffset;
float _ErosionScale;
half _ErosionFactor;
half _ErosionOcclusion;
float _MicroErosionScale;
half _MicroErosionFactor;
half _FadeInStart;
half _FadeInDistance;
half _MultiScattering;
half4 _ScatteringTint;
half _AmbientProbeDimmer;
half _SunLightDimmer;
float _EarthRadius;
half _AccumulationFactor;
half _NormalizationFactor;
half _CloudNearPlane;
CBUFFER_END

// Ambient Probe (unity_SH)
half4 clouds_SHAr;
half4 clouds_SHAg;
half4 clouds_SHAb;
half4 clouds_SHBr;
half4 clouds_SHBg;
half4 clouds_SHBb;
half4 clouds_SHC;

half _ImprovedTransmittanceBlend;
float _PostExposure; // Exposure from the ColorAdjustments override
half3 _SunColor;

#ifndef URP_PHYSICALLY_BASED_SKY_DEFINES_INCLUDED
float4 _PlanetCenterRadius;
#endif

#endif

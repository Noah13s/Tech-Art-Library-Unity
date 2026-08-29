Shader "Tech Art Library/Float Precision/Smart Earth Ground"
{
    Properties
    {
        [Header(Biome Colors)]
        _WaterColor ("Water", Color) = (0.035, 0.19, 0.32, 1)
        _BeachColor ("Beach", Color) = (0.72, 0.62, 0.40, 1)
        _GrassColor ("Grass", Color) = (0.16, 0.34, 0.12, 1)
        _RockColor ("Mountain Rock", Color) = (0.30, 0.27, 0.23, 1)
        _SnowColor ("Snow", Color) = (0.91, 0.94, 0.97, 1)

        [Header(Coastline Classification)]
        [Toggle] _UseLandWaterMask ("Use Land Water Mask", Float) = 0
        _WaterMaskThreshold ("Water Mask Threshold", Range(0, 1)) = 0.5
        _WaterMaskFeather ("Coastline Feather", Range(0.001, 0.25)) = 0.04
        _BeachMaskWidth ("Beach Width In Mask", Range(0.001, 0.5)) = 0.12

        [Header(Elevation Biomes)]
        _WaterLevel ("Water Level", Float) = 20
        _WaterBlend ("Water Edge Blend", Range(1, 500)) = 40
        _BeachEnd ("Beach End", Float) = 500
        _BeachBlend ("Beach Blend", Range(1, 1000)) = 180
        _MountainStart ("Mountain Start", Float) = 1400
        _MountainFull ("Mountain Full", Float) = 2600
        _SnowStart ("Snow Start", Float) = 3200
        _SnowFull ("Snow Full", Float) = 4300

        [Header(Slope Biomes)]
        _RockSlopeStart ("Rock Slope Start", Range(0, 1)) = 0.28
        _RockSlopeFull ("Rock Slope Full", Range(0, 1)) = 0.62
        _SnowSlopeLoss ("Snow Loss On Cliffs", Range(0, 1)) = 0.72

        [Header(Optional Biome Textures)]
        _TextureInfluence ("Texture Influence", Range(0, 1)) = 0
        [NoScaleOffset] _WaterTexture ("Water Texture", 2D) = "white" {}
        [NoScaleOffset] _BeachTexture ("Beach Texture", 2D) = "white" {}
        [NoScaleOffset] _GrassTexture ("Grass Texture", 2D) = "white" {}
        [NoScaleOffset] _RockTexture ("Rock Texture", 2D) = "white" {}
        [NoScaleOffset] _SnowTexture ("Snow Texture", 2D) = "white" {}
        _WaterTextureMetres ("Water Texture Size (m)", Float) = 500
        _BeachTextureMetres ("Beach Texture Size (m)", Float) = 120
        _GrassTextureMetres ("Grass Texture Size (m)", Float) = 180
        _RockTextureMetres ("Rock Texture Size (m)", Float) = 240
        _SnowTextureMetres ("Snow Texture Size (m)", Float) = 300

        [Header(Lighting)]
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.65
        [HDR] _MinimumAmbientColor ("Atmospheric Ambient", Color) = (0.22, 0.28, 0.34, 1)
        _ShadowFloor ("Large Scale Shadow Floor", Range(0, 0.5)) = 0.12
        _WaterSmoothness ("Water Smoothness", Range(0, 1)) = 0.82
        _GroundSmoothness ("Ground Smoothness", Range(0, 1)) = 0.18
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.35

        [HideInInspector] _BaseMap ("Base Map", 2D) = "white" {}
        [HideInInspector] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Cutoff ("Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _Surface ("Surface", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_WaterTexture); SAMPLER(sampler_WaterTexture);
            TEXTURE2D(_BeachTexture); SAMPLER(sampler_BeachTexture);
            TEXTURE2D(_GrassTexture); SAMPLER(sampler_GrassTexture);
            TEXTURE2D(_RockTexture); SAMPLER(sampler_RockTexture);
            TEXTURE2D(_SnowTexture); SAMPLER(sampler_SnowTexture);
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _BeachColor;
                float4 _GrassColor;
                float4 _RockColor;
                float4 _SnowColor;
                float _UseLandWaterMask;
                float _WaterMaskThreshold;
                float _WaterMaskFeather;
                float _BeachMaskWidth;
                float _WaterLevel;
                float _WaterBlend;
                float _BeachEnd;
                float _BeachBlend;
                float _MountainStart;
                float _MountainFull;
                float _SnowStart;
                float _SnowFull;
                float _RockSlopeStart;
                float _RockSlopeFull;
                float _SnowSlopeLoss;
                float _TextureInfluence;
                float _WaterTextureMetres;
                float _BeachTextureMetres;
                float _GrassTextureMetres;
                float _RockTextureMetres;
                float _SnowTextureMetres;
                float _AmbientStrength;
                float4 _MinimumAmbientColor;
                float _ShadowFloor;
                float _WaterSmoothness;
                float _GroundSmoothness;
                float _SpecularStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 biomeData : TEXCOORD1;
                float3 radialNormalOS : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 radialNormalWS : TEXCOORD2;
                float2 groundUV : TEXCOORD3;
                float2 biomeData : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                half fogFactor : TEXCOORD6;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.radialNormalWS = TransformObjectToWorldDir(input.radialNormalOS, true);
                output.groundUV = input.uv;
                output.biomeData = input.biomeData;
                output.shadowCoord = TransformWorldToShadowCoord(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            float3 SampleBiomeTexture(TEXTURE2D_PARAM(textureMap, samplerMap), float2 uv, float metres)
            {
                float safeMetres = max(abs(metres), 0.01);
                return SAMPLE_TEXTURE2D(textureMap, samplerMap, uv / safeMetres).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 radialUpWS = normalize(input.radialNormalWS);
                float slope = 1.0 - saturate(dot(normalWS, radialUpWS));
                float elevation = input.biomeData.x;

                float elevationWater = 1.0 - smoothstep(
                    _WaterLevel - _WaterBlend,
                    _WaterLevel + _WaterBlend,
                    elevation);
                float elevationBeach = (1.0 - elevationWater) * (1.0 - smoothstep(
                    _BeachEnd - _BeachBlend,
                    _BeachEnd + _BeachBlend,
                    elevation));

                float maskFeather = max(_WaterMaskFeather, 0.0001);
                float maskWater = smoothstep(
                    _WaterMaskThreshold - maskFeather,
                    _WaterMaskThreshold + maskFeather,
                    saturate(input.biomeData.y));

                // The water value is interpolated across coastline triangles. Keep
                // a narrow band on the land side for beaches; inland lowlands no
                // longer become water merely because the height map starts at zero.
                float maskBeach = (1.0 - maskWater) * smoothstep(
                    _WaterMaskThreshold - max(_BeachMaskWidth, maskFeather),
                    _WaterMaskThreshold + maskFeather,
                    saturate(input.biomeData.y));

                float useMask = saturate(_UseLandWaterMask);
                float water = lerp(elevationWater, maskWater, useMask);
                float beach = lerp(elevationBeach, maskBeach, useMask);
                float aboveWater = 1.0 - water;

                float heightRock = smoothstep(_MountainStart, max(_MountainFull, _MountainStart + 1.0), elevation);
                float slopeRock = smoothstep(_RockSlopeStart, max(_RockSlopeFull, _RockSlopeStart + 0.001), slope);
                float snow = smoothstep(_SnowStart, max(_SnowFull, _SnowStart + 1.0), elevation);
                snow *= 1.0 - slopeRock * _SnowSlopeLoss;

                float rock = max(heightRock, slopeRock) * (1.0 - snow) * aboveWater;
                beach *= 1.0 - max(rock, snow);
                float grass = max(0.0, 1.0 - water - beach - rock - snow);
                float weightSum = max(water + beach + grass + rock + snow, 0.0001);
                float4 weights = float4(water, beach, grass, rock) / weightSum;
                snow /= weightSum;

                float influence = saturate(_TextureInfluence);
                float3 waterTexture = SampleBiomeTexture(
                    TEXTURE2D_ARGS(_WaterTexture, sampler_WaterTexture), input.groundUV, _WaterTextureMetres);
                float3 beachTexture = SampleBiomeTexture(
                    TEXTURE2D_ARGS(_BeachTexture, sampler_BeachTexture), input.groundUV, _BeachTextureMetres);
                float3 grassTexture = SampleBiomeTexture(
                    TEXTURE2D_ARGS(_GrassTexture, sampler_GrassTexture), input.groundUV, _GrassTextureMetres);
                float3 rockTexture = SampleBiomeTexture(
                    TEXTURE2D_ARGS(_RockTexture, sampler_RockTexture), input.groundUV, _RockTextureMetres);
                float3 snowTexture = SampleBiomeTexture(
                    TEXTURE2D_ARGS(_SnowTexture, sampler_SnowTexture), input.groundUV, _SnowTextureMetres);

                float3 waterColor = _WaterColor.rgb * lerp(1.0.xxx, waterTexture, influence);
                float3 beachColor = _BeachColor.rgb * lerp(1.0.xxx, beachTexture, influence);
                float3 grassColor = _GrassColor.rgb * lerp(1.0.xxx, grassTexture, influence);
                float3 rockColor = _RockColor.rgb * lerp(1.0.xxx, rockTexture, influence);
                float3 snowColor = _SnowColor.rgb * lerp(1.0.xxx, snowTexture, influence);
                float3 albedo = waterColor * weights.x + beachColor * weights.y +
                    grassColor * weights.z + rockColor * weights.w + snowColor * snow;

                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                // Planet-scale shadow coordinates can lose enough precision to
                // report a fully occluded local patch. Preserve a small amount of
                // direct light so local/player/cloud shadows still read without
                // collapsing every biome to black.
                half shadowVisibility = lerp(
                    saturate(_ShadowFloor),
                    1.0h,
                    mainLight.shadowAttenuation);
                half directAttenuation = mainLight.distanceAttenuation * shadowVisibility;

                // The space skybox supplies almost no spherical-harmonic energy.
                // Close to Earth, atmospheric multiple scattering provides a real
                // diffuse sky term even when the global environment probe is black.
                half3 ambientProbe = SampleSH(normalWS);
                half3 ambient = max(ambientProbe, (half3)_MinimumAmbientColor.rgb) * _AmbientStrength;
                half3 direct = mainLight.color * ndotl * directAttenuation;

                half smoothness = lerp(_GroundSmoothness, _WaterSmoothness, weights.x);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = exp2(4.0 + smoothness * 8.0);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) *
                    smoothness * _SpecularStrength * directAttenuation;

                half3 color = albedo * (ambient + direct) + mainLight.color * specular;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half diffuse = saturate(dot(normalWS, light.direction));
                    color += albedo * light.color * diffuse *
                        light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

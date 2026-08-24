Shader "Tech Art Library/Planets/Earth Surface"
{
    Properties
    {
        [MainTexture] _DiffuseTexture("Day Map", 2D) = "white" {}
        [Normal] _NormalTexture("Normal Map", 2D) = "bump" {}
        _RoughnessTexture("Specular / Smoothness Map", 2D) = "black" {}
        _EmissionTexture("Night Lights Map", 2D) = "black" {}

        [HDR] _EmissionColor("Night Lights Tint", Color) = (1, 1, 1, 1)
        _EmissionStrength("Night Lights Strength", Range(0, 20)) = 1
        _NightFadeStart("Night Fade Start", Range(-0.5, 0.5)) = -0.12
        _NightFadeEnd("Night Fade End", Range(-0.5, 0.5)) = 0.05

        _Color("Surface Tint", Color) = (1, 1, 1, 1)
        _Intensity("Day Map Intensity", Range(0, 4)) = 1
        _NormalStrength("Normal Strength", Range(0, 2)) = 1
        _SpecularStrength("Ocean Specular Strength", Range(0, 2)) = 0.35
        _GlobalTiling("Global Tiling", Vector) = (1, 1, 0, 0)

        [Toggle] _UseDiffuseMap("Use Day Map", Float) = 1
        [Toggle] _UseNormalMap("Use Normal Map", Float) = 1
        [Toggle] _UseRoughnessMap("Use Specular Map", Float) = 1
        [Toggle] _UseEmissionMap("Use Night Lights Map", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
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
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_DiffuseTexture);
            SAMPLER(sampler_DiffuseTexture);
            TEXTURE2D(_NormalTexture);
            SAMPLER(sampler_NormalTexture);
            TEXTURE2D(_RoughnessTexture);
            SAMPLER(sampler_RoughnessTexture);
            TEXTURE2D(_EmissionTexture);
            SAMPLER(sampler_EmissionTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _DiffuseTexture_ST;
                float4 _EmissionColor;
                float4 _Color;
                float4 _GlobalTiling;
                float _EmissionStrength;
                float _NightFadeStart;
                float _NightFadeEnd;
                float _Intensity;
                float _NormalStrength;
                float _SpecularStrength;
                float _UseDiffuseMap;
                float _UseNormalMap;
                float _UseRoughnessMap;
                float _UseEmissionMap;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = input.uv * _GlobalTiling.xy * _DiffuseTexture_ST.xy + _DiffuseTexture_ST.zw;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 geometricNormalWS = normalize(input.normalWS);
                half3 normalWS = geometricNormalWS;

                if (_UseNormalMap > 0.5)
                {
                    half3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, input.uv),
                        _NormalStrength);
                    half3x3 tangentToWorld = half3x3(
                        normalize(input.tangentWS),
                        normalize(input.bitangentWS),
                        geometricNormalWS);
                    normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
                }

                half3 dayMap = SAMPLE_TEXTURE2D(
                    _DiffuseTexture,
                    sampler_DiffuseTexture,
                    input.uv).rgb;
                half3 albedo = lerp(_Color.rgb, dayMap * _Color.rgb * _Intensity, saturate(_UseDiffuseMap));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half geometricNdotL = dot(geometricNormalWS, mainLight.direction);
                half shadedNdotL = saturate(dot(normalWS, mainLight.direction));
                half lightAttenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                half3 ambient = SampleSH(normalWS) * albedo;
                half3 direct = albedo * mainLight.color * shadedNdotL * lightAttenuation;

                half smoothness = SAMPLE_TEXTURE2D(
                    _RoughnessTexture,
                    sampler_RoughnessTexture,
                    input.uv).r * saturate(_UseRoughnessMap);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = lerp(8.0h, 256.0h, smoothness);
                half specularTerm = pow(saturate(dot(normalWS, halfDirection)), specularPower);
                half3 specular = mainLight.color
                    * specularTerm
                    * smoothness
                    * _SpecularStrength
                    * shadedNdotL
                    * lightAttenuation;

                // The terminator uses the geometric normal so normal-map detail cannot
                // make individual night lights flicker on the sun-facing hemisphere.
                half fadeWidth = max(0.0001h, _NightFadeEnd - _NightFadeStart);
                half nightMask = 1.0h - smoothstep(
                    _NightFadeStart,
                    _NightFadeStart + fadeWidth,
                    geometricNdotL);
                half3 nightLights = SAMPLE_TEXTURE2D(
                    _EmissionTexture,
                    sampler_EmissionTexture,
                    input.uv).rgb
                    * _EmissionColor.rgb
                    * _EmissionStrength
                    * nightMask
                    * saturate(_UseEmissionMap);

                half3 color = ambient + direct + specular + nightLights;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0.0h;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output = (DepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    return half4(packedNormalWS, 0.0h);
                #else
                    return half4(normalWS, 0.0h);
                #endif
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

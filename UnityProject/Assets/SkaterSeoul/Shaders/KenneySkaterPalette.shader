Shader "SkaterSeoul/Kenney Skater Palette"
{
    Properties
    {
        _BaseMap ("Skin Atlas", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(pos);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half redCloth = step(0.38h, source.r) * step(source.g * 1.24h, source.r) * step(source.b * 1.24h, source.r);
                half luminance = saturate(dot(source.rgb, half3(0.30h, 0.59h, 0.11h)) * 1.25h);
                half3 cream = lerp(half3(0.37h, 0.32h, 0.26h), half3(0.91h, 0.85h, 0.73h), luminance);
                source.rgb = lerp(source.rgb, cream, redCloth);
                half tealAccent = step(0.30h, source.g) * step(source.r * 1.10h, source.g) * step(source.b * 1.10h, source.g);
                source.rgb = lerp(source.rgb, lerp(half3(0.66h, 0.62h, 0.53h), half3(0.96h, 0.93h, 0.85h), luminance), tealAccent);
                half darkHair = step(0.035h, source.r) * (1.0h - step(0.36h, source.r)) *
                    step(source.g * 1.28h, source.r) * step(source.b * 1.5h, source.r);
                source.rgb = lerp(source.rgb, half3(0.035h, 0.045h, 0.06h), darkHair);
                half3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half lambert = saturate(dot(normal, mainLight.direction));
                half3 lighting = SampleSH(normal) + mainLight.color * (lambert * mainLight.shadowAttenuation);
                half4 color = half4(source.rgb * lighting, source.a);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}

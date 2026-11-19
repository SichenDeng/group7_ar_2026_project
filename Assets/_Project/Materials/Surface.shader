Shader "AR Function Lab/Surface"
{
    Properties
    {
        _BaseColor ("Surface Color", Color) = (0.12, 0.65, 0.74, 1)
        _GridColor ("Grid Color", Color) = (0.04, 0.22, 0.3, 1)
        _GridCount ("Grid Count", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridCount;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float light = 0.35 + 0.65 * abs(dot(normalize(input.normalWS), normalize(float3(0.4, 0.8, -0.3))));
                float2 gridUV = input.uv * _GridCount;
                float2 distance = abs(frac(gridUV - 0.5) - 0.5) / max(fwidth(gridUV), 0.0001);
                float grid = 1 - saturate(min(distance.x, distance.y));
                float3 color = lerp(_BaseColor.rgb * light, _GridColor.rgb, grid * 0.55);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

Shader "Game/LowPolyWater"
{
    Properties
    {
        _ShallowColor ("Shallow", Color) = (0.20, 0.62, 0.68, 0.60)
        _DeepColor ("Deep", Color) = (0.04, 0.20, 0.38, 0.92)
        _WaveScale ("Wave Scale", Float) = 0.35
        _WaveSpeed ("Wave Speed", Float) = 0.6
        _Specular ("Specular", Range(0, 2)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _WaveScale;
                float _WaveSpeed;
                half _Specular;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float timeValue = _Time.y * _WaveSpeed;
                float2 positionXZ = input.positionWS.xz;

                // Three analytic wave gradients; the mesh itself remains only two triangles.
                float2 gradient = 0;
                gradient += float2(0.80, 0.60) * 0.30 * cos(dot(positionXZ, float2(0.80, 0.60)) * 0.9 + timeValue * 1.7);
                gradient += float2(-0.50, 0.85) * 0.22 * cos(dot(positionXZ, float2(-0.50, 0.85)) * 1.4 + timeValue * 2.1);
                gradient += float2(0.95, -0.30) * 0.15 * cos(dot(positionXZ, float2(0.95, -0.30)) * 2.3 + timeValue * 2.9);
                float3 normalWS = normalize(float3(-gradient.x * _WaveScale, 1.0, -gradient.y * _WaveScale));

                Light mainLight = GetMainLight();
                float3 viewDirection = normalize(GetCameraPositionWS() - input.positionWS);
                float facing = saturate(dot(normalWS, viewDirection));
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, 1.0 - facing);
                half3 lighting = mainLight.color * saturate(dot(normalWS, mainLight.direction)) + SampleSH(normalWS);
                color *= lighting;

                float3 halfDirection = normalize(mainLight.direction + viewDirection);
                color += mainLight.color * pow(saturate(dot(normalWS, halfDirection)), 90.0) * _Specular;
                half alpha = lerp(_ShallowColor.a, _DeepColor.a, facing);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

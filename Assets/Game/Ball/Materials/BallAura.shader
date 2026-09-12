// Selection aura for Ball Stacks: an additive fresnel rim rendered on a
// slightly-enlarged sphere shell around the ball, with a soft pulse so it
// reads as "targeted" rather than "solid". Colour is set per player through
// a MaterialPropertyBlock (_BaseColor).
Shader "Ball Stacks/Ball Aura"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 0.35, 0.25, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 2.5
        _RimIntensity("Rim Intensity", Range(0, 4)) = 1.6
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 4
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "AuraForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _RimPower;
                half _RimIntensity;
                half _PulseSpeed;
                half _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                half rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);

                half pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);
                half strength = rim * _RimIntensity * pulse;

                return half4(_BaseColor.rgb * strength, saturate(strength) * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}

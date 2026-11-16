Shader "HERMES/Orbital Transfer"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+4" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One One
        ColorMask RGB

        Pass
        {
            Name "OrbitalLight"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half edge = saturate(1.0h - abs(input.uv.x * 2.0h - 1.0h));
                half tips = smoothstep(0.0h, 0.11h, min(input.uv.y, 1.0h - input.uv.y));
                half intensity = edge * edge * tips * input.color.a;
                return half4(input.color.rgb * intensity, 0.0h);
            }
            ENDHLSL
        }
    }
}

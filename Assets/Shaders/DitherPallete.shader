Shader "UI/URP/Dither Quantize Palette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Pixelation)]
        _PixelSize ("Pixel Size (1 = off)", Range(1, 32)) = 1

        [Header(Quantization)]
        _Levels ("Levels per channel", Range(2, 256)) = 8

        [Header(Dithering)]
        [KeywordEnum(Off, Bayer2, Bayer4, Bayer8)] _Dither ("Dither Matrix", Float) = 3
        _DitherStrength ("Dither Strength", Range(0, 1)) = 1

        [Header(Palette)]
        [Toggle(_USE_PALETTE)] _UsePalette ("Use Palette", Float) = 0
        [NoScaleOffset] _PaletteTex ("Palette (N x 1, Point, no mips)", 2D) = "white" {}
        _PaletteStrength ("Palette Strength", Range(0, 1)) = 1

        // UI
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #pragma multi_compile_local _DITHER_OFF _DITHER_BAYER2 _DITHER_BAYER4 _DITHER_BAYER8
            #pragma multi_compile_local _ _USE_PALETTE
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PaletteTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _PaletteTex_TexelSize;
                half4  _Color;
                float4 _ClipRect;
                float  _PixelSize;
                float  _Levels;
                float  _DitherStrength;
                float  _PaletteStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                float4 worldPos   : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.worldPos   = v.positionOS;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv         = v.uv;
                o.color      = v.color * _Color;
                return o;
            }

            // Retorna um limiar centrado em 0, no intervalo [-0.5, 0.5]
            float BayerThreshold(float2 pixel)
            {
            #if defined(_DITHER_BAYER2)
                uint2 p = (uint2)pixel;
                uint a = (p.x ^ p.y), b = p.y;
                uint idx = ((b & 1u) << 1) | (a & 1u);
                return (idx + 0.5) / 4.0 - 0.5;
            #elif defined(_DITHER_BAYER4)
                uint2 p = (uint2)pixel;
                uint a = (p.x ^ p.y), b = p.y;
                uint idx = ((b & 1u) << 3) | ((a & 1u) << 2) | (b & 2u) | ((a & 2u) >> 1);
                return (idx + 0.5) / 16.0 - 0.5;
            #elif defined(_DITHER_BAYER8)
                uint2 p = (uint2)pixel;
                uint a = (p.x ^ p.y), b = p.y;
                uint idx = ((b & 1u) << 5) | ((a & 1u) << 4)
                         | ((b & 2u) << 2) | ((a & 2u) << 1)
                         | ((b & 4u) >> 1) | ((a & 4u) >> 2);
                return (idx + 0.5) / 64.0 - 0.5;
            #else
                return 0.0;
            #endif
            }

            float3 NearestPaletteColor(float3 c)
            {
                int n = max((int)_PaletteTex_TexelSize.z, 1);
                float best = 1e10;
                float3 result = c;

                [loop]
                for (int k = 0; k < n; k++)
                {
                    float3 p = LOAD_TEXTURE2D(_PaletteTex, int2(k, 0)).rgb;
                    float3 d = c - p;
                    float rm = 0.5 * (c.r + p.r);
                    // distância "redmean": mais próxima da percepção humana que RGB puro
                    float dist = (2.0 + rm) * d.r * d.r + 4.0 * d.g * d.g + (3.0 - rm) * d.b * d.b;
                    if (dist < best)
                    {
                        best = dist;
                        result = p;
                    }
                }
                return result;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 1) Pixelização (grade alinhada à resolução da textura)
                float2 texSize = _MainTex_TexelSize.zw;
                float2 px      = floor(i.uv * texSize / _PixelSize);
                float2 uv      = (px + 0.5) * _PixelSize / texSize;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 col = tex * i.color;
                float3 c  = col.rgb;

                // 2) Dithering + 3) Quantização
                float steps = max(_Levels - 1.0, 1.0);
                float t     = BayerThreshold(px) * _DitherStrength;
                float3 q    = floor(c * steps + t + 0.5) / steps;
                q = saturate(q);

                // 4) Paleta
            #if defined(_USE_PALETTE)
                float3 pal = NearestPaletteColor(q);
                q = lerp(q, pal, _PaletteStrength);
            #endif

                half4 result = half4(q, col.a);

            #ifdef UNITY_UI_CLIP_RECT
                float2 inside = step(_ClipRect.xy, i.worldPos.xy) * step(i.worldPos.xy, _ClipRect.zw);
                result.a *= inside.x * inside.y;
            #endif

            #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
            #endif

                return result;
            }
            ENDHLSL
        }
    }
}
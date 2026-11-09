// Shader para RawImage (Canvas) que processa a Render Texture da câmera.
// Ordem do pipeline:
//   1) Dithering (Bayer)  -> desloca a cor antes de ela ser "arredondada"
//   2) Quantização        -> limita os níveis por canal
//   3) Paleta             -> troca pela cor mais próxima da paleta (textura Nx1)
//   4) Layers de saturação-> divide a saturação do pixel em N camadas e
//                            aplica o multiplicador de cada camada na cor final
Shader "UI/Dither Quantize Palette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Render Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dithering)]
        [Toggle(_DITHER_ON)] _DitherEnabled ("Ativar Dithering", Float) = 1
        [Enum(Bayer2x2,0,Bayer4x4,1,Bayer8x8,2)] _DitherMatrix ("Matriz", Float) = 2
        _DitherStrength ("Intensidade", Range(0, 1)) = 1
        [IntRange] _DitherScale ("Tamanho do pixel do dither", Range(1, 16)) = 1

        [Header(Quantization)]
        [Toggle(_QUANT_ON)] _QuantEnabled ("Ativar Quantizacao", Float) = 1
        [IntRange] _QuantizationLevel ("Niveis por canal", Range(2, 64)) = 8
        _QuantizationStrength ("Intensidade", Range(0, 1)) = 1

        [Header(Palette)]
        [Toggle(_PALETTE_ON)] _PaletteEnabled ("Ativar Paleta", Float) = 0
        _PaletteTex ("Paleta (Nx1, Point, sem compressao)", 2D) = "white" {}
        _PaletteStrength ("Intensidade", Range(0, 1)) = 1

        [Header(Saturation Layers)]
        [Toggle(_SATLAYERS_ON)] _SatLayersEnabled ("Ativar Layers de Saturacao", Float) = 1
        [IntRange] _SatLayers ("Numero de layers", Range(1, 8)) = 4
        _SatMin ("Saturacao da layer mais baixa", Range(0, 1.5)) = 0.25
        _SatMax ("Saturacao da layer mais alta", Range(0, 1.5)) = 1
        _SatLayersStrength ("Intensidade", Range(0, 1)) = 1

        [Header(Color Space)]
        [Toggle] _WorkInGamma ("Operar em espaco Gamma (projeto Linear)", Float) = 1

        // --- Necessarios para UI (Mask / RectMask2D / alpha clip) ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ _DITHER_ON
            #pragma multi_compile_local _ _QUANT_ON
            #pragma multi_compile_local _ _PALETTE_ON
            #pragma multi_compile_local _ _SATLAYERS_ON

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // zw = largura/altura da render texture
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _DitherMatrix, _DitherStrength, _DitherScale;
            float _QuantizationLevel, _QuantizationStrength;
            sampler2D _PaletteTex;
            float4 _PaletteTex_TexelSize;
            float _PaletteStrength;
            float _SatLayers, _SatMin, _SatMax, _SatLayersStrength;
            float _WorkInGamma;

            static const float3 LUMA = float3(0.299, 0.587, 0.114);

            // ---------- Bayer (calculado, sem textura) ----------
            int Bayer2(int x, int y)
            {
                return ((x & 1) * 2) ^ ((y & 1) * 3);
            }

            // Retorna limiar em (0,1)
            float BayerThreshold(int2 p, int type)
            {
                int x = p.x;
                int y = p.y;
                if (type == 0)
                    return (Bayer2(x, y) + 0.5) / 4.0;
                if (type == 1)
                    return (4 * Bayer2(x, y) + Bayer2(x >> 1, y >> 1) + 0.5) / 16.0;
                return (16 * Bayer2(x, y) + 4 * Bayer2(x >> 1, y >> 1) + Bayer2(x >> 2, y >> 2) + 0.5) / 64.0;
            }

            // ---------- Saturacao (HSV) ----------
            float SaturationOf(float3 c)
            {
                float mx = max(c.r, max(c.g, c.b));
                float mn = min(c.r, min(c.g, c.b));
                return mx > 1e-5 ? (mx - mn) / mx : 0.0;
            }

            float3 ToWorkSpace(float3 c)
            {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                if (_WorkInGamma > 0.5) return LinearToGammaSpace(c);
            #endif
                return c;
            }

            float3 FromWorkSpace(float3 c)
            {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                if (_WorkInGamma > 0.5) return GammaToLinearSpace(c);
            #endif
                return c;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float4 src = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float3 original = ToWorkSpace(saturate(src.rgb));
                float3 c = original;

                // --- Limiar de dithering (-0.5 .. 0.5) ---
                float ditherOffset = 0.0;
                #if defined(_DITHER_ON)
                    float2 pix = floor(IN.texcoord * _MainTex_TexelSize.zw / max(_DitherScale, 1.0));
                    int2 ip = int2(pix);
                    ditherOffset = (BayerThreshold(ip, (int)(_DitherMatrix + 0.5)) - 0.5) * _DitherStrength;
                #endif

                // O degrau da quantizacao define a amplitude do dither
                float levels = max(round(_QuantizationLevel), 2.0);
                float step = 1.0 / (levels - 1.0);

                // 1) Dithering
                c = saturate(c + ditherOffset * step);

                // 2) Quantizacao
                #if defined(_QUANT_ON)
                    float3 q = floor(c * (levels - 1.0) + 0.5) / (levels - 1.0);
                    c = lerp(c, q, _QuantizationStrength);
                #endif

                // 3) Paleta
                #if defined(_PALETTE_ON)
                {
                    float w = _PaletteTex_TexelSize.z;
                    int count = (int)min(w, 256.0);
                    float best = 1e9;
                    float3 bestCol = c;

                    [loop]
                    for (int i = 0; i < count; i++)
                    {
                        float4 pc = tex2Dlod(_PaletteTex, float4((i + 0.5) / w, 0.5, 0, 0));
                        if (pc.a < 0.5) continue; // pixels transparentes sao ignorados
                        float3 pcol = ToWorkSpace(pc.rgb);
                        float3 d = c - pcol;
                        float dist = dot(d * d, LUMA);
                        if (dist < best)
                        {
                            best = dist;
                            bestCol = pcol;
                        }
                    }
                    c = lerp(c, bestCol, _PaletteStrength);
                }
                #endif

                // 4) Layers de saturacao
                #if defined(_SATLAYERS_ON)
                {
                    float n = max(round(_SatLayers), 1.0);
                    float s = SaturationOf(original);
                    float t = saturate(s + ditherOffset / n);
                    float idx = min(floor(t * n), n - 1.0);
                    float mult = (n <= 1.0) ? _SatMax : lerp(_SatMin, _SatMax, idx / (n - 1.0));

                    float gray = dot(c, LUMA);
                    float3 layered = saturate(lerp(gray.xxx, c, mult));
                    c = lerp(c, layered, _SatLayersStrength);
                }
                #endif

                c = FromWorkSpace(c);

                fixed4 color = fixed4(c, src.a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

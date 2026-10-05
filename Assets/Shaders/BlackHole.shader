// Black Hole - Unity URP (Unity 6 compatible)
// Put on a Sphere. The sphere is the "lensing volume"; the event horizon is a fraction of its radius.
// Requirements: URP Asset > Opaque Texture = ON.
Shader "Custom/URP/BlackHole"
{
    Properties
    {
        [Header(Horizon)]
        _HorizonRadius   ("Horizon Radius (0-1 of sphere)", Range(0.05, 0.6)) = 0.28
        _HorizonSoftness ("Horizon Softness", Range(0.001, 0.2)) = 0.03

        [Header(Lensing)]
        _Distortion      ("Distortion Strength", Range(0, 2)) = 0.9
        _DistortionPower ("Distortion Falloff", Range(0.5, 6)) = 2.0

        [Header(Glow)]
        [HDR] _GlowColor  ("Outer Glow Color", Color) = (1.0, 0.35, 0.08, 1)
        [HDR] _InnerColor ("Inner Hot Color",  Color) = (1.0, 0.85, 0.55, 1)
        _GlowIntensity   ("Glow Intensity", Range(0, 8)) = 2.0
        _GlowFalloff     ("Glow Falloff", Range(0.5, 8)) = 3.0
        _RingRadius      ("Photon Ring Radius", Range(0.1, 0.9)) = 0.34
        _RingWidth       ("Photon Ring Width", Range(0.005, 0.3)) = 0.05
        _RingIntensity   ("Photon Ring Intensity", Range(0, 6)) = 1.5

        [Header(Animation)]
        _SwirlArms       ("Swirl Arms (integer)", Range(1, 8)) = 3
        _SwirlTwist      ("Swirl Twist", Range(0, 30)) = 12
        _SwirlSpeed      ("Swirl Speed", Range(-5, 5)) = 1.2
        _SwirlStrength   ("Swirl Strength", Range(0, 1)) = 0.6
        _PulseSpeed      ("Pulse Speed", Range(0, 6)) = 1.5

        [Header(Noise (optional))]
        [NoScaleOffset] _NoiseTex ("Noise Texture (leave empty = off)", 2D) = "white" {}
        _NoiseStrength   ("Noise Brightness Strength", Range(0, 1)) = 0.7
        _NoiseWarp       ("Noise Swirl Warp", Range(0, 6)) = 1.5
        _NoiseScale      ("Noise Scale (X = angular, use integer)", Vector) = (3, 1, 0, 0)
        _NoiseSpeed      ("Noise Speed", Range(-2, 2)) = 0.15
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
            Name "BlackHole"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend Off // we output the final color (background already sampled)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _HorizonRadius, _HorizonSoftness;
                float _Distortion, _DistortionPower;
                float4 _GlowColor, _InnerColor;
                float _GlowIntensity, _GlowFalloff;
                float _RingRadius, _RingWidth, _RingIntensity;
                float _SwirlArms, _SwirlTwist, _SwirlSpeed, _SwirlStrength;
                float _PulseSpeed;
                float _NoiseStrength, _NoiseWarp, _NoiseSpeed;
                float4 _NoiseScale;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Sphere center / radius from the object matrix (default Unity sphere radius = 0.5)
                float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float  R = 0.5 * length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));

                // --- View ray (works for perspective and orthographic cameras)
                float3 rayDir = lerp(normalize(IN.positionWS - _WorldSpaceCameraPos),
                                     -UNITY_MATRIX_V[2].xyz, unity_OrthoParams.w);

                // --- Point of the ray closest to the center (on the plane facing the camera)
                float3 P = IN.positionWS + rayDir * dot(center - IN.positionWS, rayDir);
                float3 v = P - center;
                float  b = length(v);
                float  d = saturate(b / R);                // 0 = center, 1 = sphere silhouette
                float3 dir = v * rcp(max(b, 1e-5));
                float  t = 1.0 - d;

                float time = _Time.y;
                float pulse = 1.0 + 0.06 * sin(time * _PulseSpeed + d * 6.0);

                // --- Gravitational lensing: pull the sampled background toward the center
                float falloff = pow(t, _DistortionPower);
                float shift = R * _Distortion * falloff * rcp(d + 0.25) * pulse;
                float3 Pbent = P - dir * shift;

                float4 clip = TransformWorldToHClip(Pbent);
                float4 sp = ComputeScreenPos(clip);
                float2 uv = sp.xy / max(sp.w, 1e-4);
                float3 bg = SampleSceneColor(uv);

                // --- Swirl (screen-facing, cheap: one atan2, one sin)
                float2 p = float2(dot(v, UNITY_MATRIX_V[0].xyz), dot(v, UNITY_MATRIX_V[1].xyz));
                float angle = atan2(p.y, p.x);

                // --- Optional noise (polar coordinates). Empty slot = "white" = value 1 = no effect.
                // LOD 0 avoids the mip seam caused by the atan2 discontinuity.
                float2 nUV = float2(angle * 0.15915494 + 0.5, d) * _NoiseScale.xy;
                nUV += float2(time * _NoiseSpeed, time * _NoiseSpeed * 0.5);
                float noise = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_LinearRepeat, nUV, 0).r;

                float phase = angle * round(_SwirlArms) + d * _SwirlTwist - time * _SwirlSpeed
                              + (noise - 1.0) * _NoiseWarp;
                float swirlPattern = sin(phase) * 0.5 + 0.5;
                float swirl = lerp(1.0, swirlPattern, _SwirlStrength) * lerp(1.0, noise, _NoiseStrength);

                // --- Glow: soft outer halo + photon ring
                float outer = pow(t, _GlowFalloff) * swirl;
                float rx = (d - _RingRadius) / _RingWidth;
                float ring = exp(-rx * rx) * _RingIntensity;
                float edgeFade = smoothstep(0.0, 0.15, t);   // glow is exactly 0 at the silhouette -> seamless
                float glow = (outer * 0.6 + ring) * pulse * edgeFade;

                float heat = saturate(1.0 - (d - _HorizonRadius) / max(1.0 - _HorizonRadius, 1e-3));
                float3 glowCol = lerp(_GlowColor.rgb, _InnerColor.rgb, heat * heat);

                float3 color = bg + glowCol * (glow * _GlowIntensity);

                // --- Event horizon (black)
                float hole = 1.0 - smoothstep(_HorizonRadius - _HorizonSoftness, _HorizonRadius, d);
                color *= (1.0 - hole);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}

Shader "Custom/URP/PlanetAtmosphere"
{
    // ------------------------------------------------------------------
    // Atmosfera físico-inspirada (Rayleigh + Mie) via ray-sphere intersection
    // Pensado para planetas de raio grande (ex.: 5000 unidades).
    // Renderiza numa esfera "casca" (shell) maior que o planeta, com
    // Cull Front para funcionar mesmo com a câmera dentro da atmosfera.
    // ------------------------------------------------------------------
    Properties
    {
        // Centro do planeta em World Space. É setado via script todo frame
        // (não dá pra confiar em um valor fixo porque o planeta pode se mover).
        _PlanetCenter ("Planet Center (World)", Vector) = (0,0,0,0)
        _PlanetRadius ("Planet Radius", Float) = 5000
        _AtmosphereRadius ("Atmosphere Radius", Float) = 5250

        // Wavelengths em nanômetros (RGB). Padrão ~ luz visível (vermelho, verde, azul).
        // Diminuir o valor aumenta o espalhamento daquele canal (ar terrestre = céu azul
        // porque λ menor espalha mais, seguindo 1/λ^4).
        _WaveLengthR ("Wavelength R (nm)", Range(380, 780)) = 700
        _WaveLengthG ("Wavelength G (nm)", Range(380, 780)) = 530
        _WaveLengthB ("Wavelength B (nm)", Range(380, 780)) = 440

        _ScatteringStrength ("Rayleigh Scattering Strength", Float) = 20
        _DensityFalloff ("Density Falloff", Range(0.1, 20)) = 8

        _MieCoefficient ("Mie Coefficient", Float) = 4
        _MieG ("Mie Anisotropy (g)", Range(-0.999, 0.999)) = 0.76

        // Steps do raymarch. Mantenha baixo — é o principal custo de performance.
        _InScatteringPoints ("In-Scatter Steps (view ray)", Range(2, 16)) = 8
        _OpticalDepthPoints ("Optical Depth Steps (sun ray)", Range(2, 8)) = 4

        _SunIntensity ("Sun Intensity", Float) = 20
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Atmosphere"
            Tags { "LightMode"="UniversalForward" }

            // Cull Front: desenhamos a face de trás da esfera-casca.
            // Isso garante que o efeito continue visível mesmo com a
            // câmera dentro da atmosfera (comum em planetas grandes).
            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            float4 _PlanetCenter;
            float  _PlanetRadius;
            float  _AtmosphereRadius;

            float _WaveLengthR;
            float _WaveLengthG;
            float _WaveLengthB;

            float _ScatteringStrength;
            float _DensityFalloff;

            float _MieCoefficient;
            float _MieG;

            int _InScatteringPoints;
            int _OpticalDepthPoints;

            float _SunIntensity;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            // Interseção analítica raio-esfera.
            // Retorna float2(dstToSphere, dstThroughSphere).
            // Se não intersecta: dstToSphere = grande, dstThroughSphere = 0.
            float2 RaySphere(float3 sphereCenter, float sphereRadius, float3 rayOrigin, float3 rayDir)
            {
                float3 offset = rayOrigin - sphereCenter;
                float a = 1.0; // rayDir é normalizado
                float b = 2.0 * dot(offset, rayDir);
                float c = dot(offset, offset) - sphereRadius * sphereRadius;
                float d = b * b - 4.0 * a * c;

                if (d > 0.0)
                {
                    float s = sqrt(d);
                    float dstNear = max(0.0, (-b - s) / (2.0 * a));
                    float dstFar = (-b + s) / (2.0 * a);

                    if (dstFar >= 0.0)
                    {
                        return float2(dstNear, dstFar - dstNear);
                    }
                }
                return float2(1.#INF, 0.0);
            }

            // Densidade exponencial de acordo com a altura acima da superfície,
            // normalizada por (0..1) em relação à espessura da atmosfera.
            // Isso mantém a matemática estável independente da escala do planeta.
            float DensityAtPoint(float3 samplePoint)
            {
                float heightAboveSurface = length(samplePoint - _PlanetCenter.xyz) - _PlanetRadius;
                float atmosphereThickness = max(_AtmosphereRadius - _PlanetRadius, 0.0001);
                float height01 = saturate(heightAboveSurface / atmosphereThickness);
                float localDensity = exp(-height01 * _DensityFalloff) * (1.0 - height01);
                return localDensity;
            }

            // Optical depth (espessura óptica) integrada ao longo de um raio curto,
            // usado tanto para o raio até o Sol quanto acumulado para o raio de visão.
            float OpticalDepth(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                int steps = max(_OpticalDepthPoints, 1);
                float stepSize = rayLength / steps;
                float3 samplePoint = rayOrigin + rayDir * (stepSize * 0.5);

                float opticalDepth = 0.0;
                [loop]
                for (int i = 0; i < steps; i++)
                {
                    opticalDepth += DensityAtPoint(samplePoint) * stepSize;
                    samplePoint += rayDir * stepSize;
                }
                return opticalDepth;
            }

            // Fase de Rayleigh
            float PhaseRayleigh(float cosAngle)
            {
                return (3.0 / (16.0 * PI)) * (1.0 + cosAngle * cosAngle);
            }

            // Fase de Mie (Henyey-Greenstein)
            float PhaseMie(float cosAngle, float g)
            {
                float g2 = g * g;
                float denom = 1.0 + g2 - 2.0 * g * cosAngle;
                denom = pow(max(denom, 1e-4), 1.5);
                return (1.0 - g2) / (4.0 * PI * denom);
            }

            float3 CalculateScattering(float3 rayOrigin, float3 rayDir, float rayLength, float3 sunDir, float3 scatterCoefficients)
            {
                int inScatterSteps = max(_InScatteringPoints, 1);
                float stepSize = rayLength / inScatterSteps;
                float3 samplePoint = rayOrigin + rayDir * (stepSize * 0.5);

                float3 totalScatteredLight = float3(0, 0, 0);
                float viewRayOpticalDepth = 0.0;

                float cosAngle = dot(rayDir, sunDir);
                float rayleighPhase = PhaseRayleigh(cosAngle);
                float miePhase = PhaseMie(cosAngle, _MieG);

                [loop]
                for (int i = 0; i < inScatterSteps; i++)
                {
                    float2 sunHit = RaySphere(_PlanetCenter.xyz, _AtmosphereRadius, samplePoint, sunDir);
                    float sunRayLength = sunHit.y;

                    float sunOpticalDepth = OpticalDepth(samplePoint, sunDir, sunRayLength);
                    viewRayOpticalDepth = OpticalDepth(rayOrigin, rayDir, stepSize * (i + 0.5));

                    float3 transmittance = exp(-(sunOpticalDepth + viewRayOpticalDepth) * scatterCoefficients);
                    float localDensity = DensityAtPoint(samplePoint);

                    // Rayleigh (colorido, dependente de wavelength) + Mie (quase acromático, forward scattering do sol)
                    totalScatteredLight += transmittance * localDensity * scatterCoefficients * rayleighPhase * stepSize;
                    totalScatteredLight += transmittance * localDensity * (_MieCoefficient * 0.001) * miePhase * stepSize;

                    samplePoint += rayDir * stepSize;
                }

                return totalScatteredLight * _SunIntensity;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(IN.positionWS - rayOrigin);

                // Profundidade da cena (para a atmosfera respeitar terreno/objetos na frente)
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                bool isSky = rawDepth <= 0.0001;

                float3 viewVec = IN.positionWS - rayOrigin;
                float sceneDist = isSky ? 1e9 : sceneEyeDepth * length(viewVec) / abs(dot(viewVec, GetViewForwardDir()));

                float2 atmosphereHit = RaySphere(_PlanetCenter.xyz, _AtmosphereRadius, rayOrigin, rayDir);
                float dstToAtmosphere = atmosphereHit.x;
                float dstThroughAtmosphere = min(atmosphereHit.y, sceneDist - dstToAtmosphere);

                if (dstThroughAtmosphere <= 0.0)
                {
                    return float4(0, 0, 0, 0);
                }

                // Corta a atmosfera atrás do próprio planeta (não precisa espalhar luz
                // do lado escuro/oculto atrás da esfera sólida).
                float2 planetHit = RaySphere(_PlanetCenter.xyz, _PlanetRadius, rayOrigin, rayDir);
                if (planetHit.y > 0.0)
                {
                    dstThroughAtmosphere = min(dstThroughAtmosphere, planetHit.x - dstToAtmosphere);
                }

                if (dstThroughAtmosphere <= 0.0)
                {
                    return float4(0, 0, 0, 0);
                }

                float3 pointInAtmosphere = rayOrigin + rayDir * dstToAtmosphere;

                float3 sunDir = normalize(_MainLightPosition.xyz);

                // Coeficientes de espalhamento Rayleigh por wavelength: ~ 1/λ^4
                float3 wavelengths = float3(_WaveLengthR, _WaveLengthG, _WaveLengthB);
                float3 scatterCoefficients = pow(400.0 / wavelengths, 4) * _ScatteringStrength * 0.001;

                float3 light = CalculateScattering(pointInAtmosphere, rayDir, dstThroughAtmosphere, sunDir, scatterCoefficients);

                float alpha = saturate(1.0 - exp(-(light.r + light.g + light.b)));
                return float4(light, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

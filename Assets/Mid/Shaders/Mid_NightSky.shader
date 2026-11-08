// Mid_NightSky.shader
// Mid_SkyDome(하늘 구)을 칠하는 밤하늘 셰이더
// 하늘 구의 가운데에서 이 픽셀 쪽으로 가는 방향 dir 하나로 색을 계산
//   1. 그라데이션: 지평선(안개 색) → 짙은 남색 → 머리 위의 거의 검은색
//   2. 은하수: dir이 띠의 큰 원에서 얼마나 벗어났는지(dot(dir, 띠 법선))로 띠 모양을 만들고,
//      노이즈로 구름처럼 뭉친 빛과 가운데를 가르는 어두운 먼지띠를 더함 (S13 SDF처럼 "식으로 모양 만들기")
//   3. 성운: 하늘 전체에 아주 옅은 보라·청록 빛
// 지평선의 색을 장면의 안개 색과 같게 두면 정자와 땅이 하늘로 자연스럽게 녹아듦
Shader "CG/Mid_NightSky"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.004, 0.006, 0.02, 1)
        _MidColor ("Mid Color", Color) = (0.025, 0.032, 0.09, 1)
        _HorizonColor ("Horizon Color", Color) = (0.07, 0.085, 0.17, 1)

        _BandNormal ("Milky Way Band Normal", Vector) = (0, 1, 0, 0)
        _BandWidth ("Milky Way Width", Range(0.02, 0.5)) = 0.17
        _BandIntensity ("Milky Way Intensity", Range(0, 3)) = 1
        _BandCoreColor ("Milky Way Core Color", Color) = (0.5, 0.42, 0.42, 1)
        _BandEdgeColor ("Milky Way Edge Color", Color) = (0.16, 0.18, 0.42, 1)

        _NebulaColorA ("Nebula Color A", Color) = (0.35, 0.1, 0.45, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.05, 0.3, 0.4, 1)
        _NebulaIntensity ("Nebula Intensity", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _ZenithColor, _MidColor, _HorizonColor;
            float4 _BandNormal;
            float _BandWidth, _BandIntensity;
            float4 _BandCoreColor, _BandEdgeColor;
            float4 _NebulaColorA, _NebulaColorB;
            float _NebulaIntensity;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;          // 하늘 구 가운데에서 이 정점으로 가는 방향
            };

            v2f vert(float3 positionOS : POSITION)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(positionOS);
                o.dir = positionOS;
                return o;
            }

            // ---- 노이즈: 3D 격자 꼭짓점마다 무작위 값을 두고 그 사이를 부드럽게 섞음 ----
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash(i + float3(0, 0, 0)), hash(i + float3(1, 0, 0)), f.x),
                                 lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), f.x),
                                 lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            // 크기가 다른 노이즈를 겹쳐 구름 같은 무늬를 만듦
            float fbm(float3 p)
            {
                float v = 0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    v += a * noise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;

                // 1. 그라데이션
                float t = saturate(h);
                float3 col = lerp(_HorizonColor.rgb, _MidColor.rgb, smoothstep(0, 0.25, t));
                col = lerp(col, _ZenithColor.rgb, smoothstep(0.25, 1, t));

                // 2. 은하수
                float3 n = normalize(_BandNormal.xyz);
                float d = dot(dir, n);                                     // 띠 가운데에서 벗어난 정도
                float w = _BandWidth;
                float band = exp(-d * d / (2 * w * w));                    // 가운데가 가장 밝은 띠
                float clouds = fbm(dir * 3.0);
                float fine = fbm(dir * 9.0 + 7.1);
                float dustNoise = smoothstep(0.45, 0.72, fbm(dir * 6.0 + 3.3));
                float dust = dustNoise * exp(-d * d / (2 * (w * 0.35) * (w * 0.35)));   // 가운데를 가르는 어두운 먼지띠
                float glow = band * (0.3 + 1.5 * clouds * fine) * (1 - 0.8 * dust);
                float3 bandCol = lerp(_BandEdgeColor.rgb, _BandCoreColor.rgb, exp(-d * d / (2 * (w * 0.4) * (w * 0.4))));

                // 3. 성운
                float neb = smoothstep(0.45, 0.8, fbm(dir * 1.7 + 11.0));
                float3 nebCol = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, fbm(dir * 2.3 - 5.0)) * neb * _NebulaIntensity;

                float horizonFade = smoothstep(-0.02, 0.25, h);           // 지평선 가까이는 옅게
                col += (bandCol * glow * _BandIntensity + nebCol) * horizonFade;

                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}

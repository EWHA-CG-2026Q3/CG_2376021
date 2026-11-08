// Mid_StarField.shader
// Mid_StarField 메시(별 하나 = 사각형)를 둥글게 빛나는 별로 칠하는 셰이더 (S16_MyCoal 바탕 + 시간 uniform)
// 사각형의 모서리 좌표(uv: -1 ~ 1)로 가운데에서 얼마나 떨어졌는지 재서 모양을 만듦 (S13 SDF와 같은 발상)
//   가운데: 작고 밝은 점 / 둘레: 옅은 빛무리 / 밝은 별: 십자 빛살
// 반짝임: 밝기 × sin(시간 × 속도 + 별마다 다른 위상)
// 지평선 가까이의 별은 흐려짐 (실제로도 공기를 더 두껍게 지나와서 어두워짐)
// Blend One One(더하기): 뒤의 밤하늘에 별빛을 더함. 1을 넘는 밝은 별은 Bloom으로 번짐
Shader "CG/Mid_StarField"
{
    Properties
    {
        _Brightness ("Brightness", Float) = 1
        _Sharpness ("Core Sharpness", Float) = 1
        _HaloStrength ("Halo Strength", Range(0, 1)) = 0.18
        _SpikeStrength ("Spike Strength", Range(0, 2)) = 0.7
        _TwinkleSpeed ("Twinkle Speed", Float) = 2.2
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _Brightness;
            float _Sharpness;
            float _HaloStrength;
            float _SpikeStrength;
            float _TwinkleSpeed;
            float _TwinkleAmount;

            struct appdata
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;          // 별의 색
                float2 corner : TEXCOORD0;     // 사각형 모서리 (-1 ~ 1)
                float4 data : TEXCOORD1;       // x = 위상, y = 밝기, z = 빛살 세기, w = 가운데 점의 날카로움
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 corner : TEXCOORD0;
                float4 data : TEXCOORD1;
                float height : TEXCOORD2;      // 하늘 구 위의 높이 (-1 ~ 1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.color = v.color;
                o.corner = v.corner;
                o.data = v.data;
                o.height = normalize(v.positionOS).y;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.corner;
                float r2 = dot(p, p);                          // 가운데에서의 거리²

                float core = exp(-r2 * i.data.w * _Sharpness);              // 가운데 점
                float halo = exp(-r2 * 4) * _HaloStrength * saturate(i.data.z + 0.3); // 옅은 빛무리 (밝은 별일수록)
                // 가로·세로로 가늘게 뻗는 빛살
                float spikes = exp(-abs(p.y) * 70) * saturate(1 - abs(p.x))
                             + exp(-abs(p.x) * 70) * saturate(1 - abs(p.y));
                spikes *= i.data.z * _SpikeStrength;

                // _Time.y = Play 이후 흐른 시간(초). 별마다 위상과 빠르기를 조금씩 다르게
                float wave = 0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed * (0.7 + i.data.x * 0.1) + i.data.x);
                float twinkle = lerp(1 - _TwinkleAmount, 1, wave);

                float horizon = saturate(i.height * 6);        // 지평선 근처는 어둡게

                float intensity = (core + halo + spikes) * i.data.y * twinkle * horizon * _Brightness;
                return float4(i.color.rgb * intensity, 1);
            }
            ENDHLSL
        }
    }
}

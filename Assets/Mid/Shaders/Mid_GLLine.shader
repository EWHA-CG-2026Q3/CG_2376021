// Mid_GLLine.shader
// Mid_Constellations가 GL.LINES로 넘긴 별자리 선을 칠하는 셰이더 (S06_OpenGL_Shader 바탕)
// 색 = 정점 색(GL.Color) × _Color
// Blend One One(더하기): 뒤의 밤하늘 색에 선의 색을 더해, 가늘고 은은하게 빛나는 선이 됨
Shader "CG/Mid_GLLine"
{
    Properties
    {
        _Color ("Color", Color) = (0.22, 0.32, 0.6, 1)
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

            float4 _Color;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.color.rgb * _Color.rgb, 1);
            }
            ENDHLSL
        }
    }
}

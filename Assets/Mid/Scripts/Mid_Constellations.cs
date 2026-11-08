// Mid_Constellations.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Sky (Mid_StarField와 함께)
// 중간 프로젝트 선택 요소 ③ OpenGL 프로그래밍 — GL 클래스로 별자리 선을 그림 (S06 immediate mode)
// 별자리 데이터: 실제 별의 적경·적위를 별자리 가운데 기준 각도(°)로 바꾼 값과 실제 밝기(등급)
//   북두칠성, 카시오페이아, 오리온
// 별자리 별 자체는 Mid_StarField가 이 데이터를 읽어 다른 별과 같은 방식으로 그림
// 선은 별에 닿지 않도록 양 끝을 조금 띄움 (별자리 앱처럼)
using System;
using UnityEngine;

[ExecuteAlways]
public class Mid_Constellations : MonoBehaviour
{
    [Serializable]
    public class Star
    {
        public string name;
        public Vector2 offset;      // 별자리 가운데에서 (오른쪽, 위쪽)으로 몇 도
        public float magnitude;     // 등급: 작을수록 밝음
        public Color tint = Color.white;
    }

    [Serializable]
    public class Constellation
    {
        public string name;
        public float azimuth;       // 방위각(°): 0 = +z, 90 = +x
        public float elevation;     // 고도(°): 0 = 지평선, 90 = 머리 위
        public float rotation;      // 별자리를 하늘에서 돌리는 각도(°)
        public Star[] stars;
        public Vector2Int[] edges;  // 이을 별 번호 두 개씩
    }

    [SerializeField] float radius = 398f;                // 별(Mid_StarField, 400)보다 살짝 안쪽
    [SerializeField] Material lineMaterial;              // Mid_GLLine 셰이더 재질
    [SerializeField] Color lineColor = Color.white;
    [SerializeField] float gapDegrees = 0.7f;            // 선 끝과 별 사이의 틈(°)
    [SerializeField] Constellation[] constellations = Defaults();

    public Constellation[] Items => constellations;

    // 방위각·고도 → 하늘 구 위의 방향 (길이 1)
    public static Vector3 SkyDirection(float azimuthDeg, float elevationDeg)
    {
        float az = azimuthDeg * Mathf.Deg2Rad;
        float el = elevationDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az));
    }

    // 별자리 가운데 방향을 기준으로 (오른쪽, 위쪽)으로 offset만큼 떨어진 별의 방향
    public static Vector3 StarDirection(Constellation c, Vector2 offsetDeg)
    {
        Vector3 center = SkyDirection(c.azimuth, c.elevation);
        Vector3 right = Vector3.Cross(Vector3.up, center).normalized;   // 가운데를 바라볼 때 오른쪽
        Vector3 up = Vector3.Cross(center, right);                      // 가운데를 바라볼 때 위쪽

        float r = c.rotation * Mathf.Deg2Rad;
        float x = offsetDeg.x * Mathf.Cos(r) - offsetDeg.y * Mathf.Sin(r);   // 2D 회전 (S08)
        float y = offsetDeg.x * Mathf.Sin(r) + offsetDeg.y * Mathf.Cos(r);

        return (center + right * Mathf.Tan(x * Mathf.Deg2Rad) + up * Mathf.Tan(y * Mathf.Deg2Rad)).normalized;
    }

    void OnRenderObject()
    {
        // 이 GameObject가 활성화되어 있으면, 매 카메라 렌더링마다 Unity가 자동으로 호출함 (S06)
        if (lineMaterial == null || constellations == null) return;

        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        // "지금부터 정점 2개씩 묶어서 선으로 그리겠다"고 GPU에 선언
        GL.Color(lineColor);

        foreach (Constellation c in constellations)
        {
            if (c.stars == null || c.edges == null) continue;
            foreach (Vector2Int e in c.edges)
            {
                if (e.x < 0 || e.y < 0 || e.x >= c.stars.Length || e.y >= c.stars.Length) continue;

                Vector3 a = StarDirection(c, c.stars[e.x].offset);
                Vector3 b = StarDirection(c, c.stars[e.y].offset);

                // 양 끝을 gapDegrees만큼 안쪽으로 당김
                float angle = Vector3.Angle(a, b);
                if (angle <= gapDegrees * 2f) continue;
                float t = gapDegrees / angle;
                Vector3 p = ToWorld(Vector3.Slerp(a, b, t));
                Vector3 q = ToWorld(Vector3.Slerp(a, b, 1f - t));

                GL.Vertex3(p.x, p.y, p.z);
                GL.Vertex3(q.x, q.y, q.z);
            }
        }

        GL.End();
    }

    // 하늘 구 위의 방향 → 월드 좌표 (Mid_Sky의 위치와 회전을 따름)
    Vector3 ToWorld(Vector3 dir)
    {
        return transform.position + transform.rotation * (dir * radius);
    }

    // 실제 별의 적경·적위로 계산한 별자리 모양 (가운데 기준, 단위 °)
    static Constellation[] Defaults()
    {
        Color orange = new Color(1f, 0.62f, 0.38f);
        Color blue = new Color(0.72f, 0.82f, 1f);

        return new[]
        {
            new Constellation
            {
                name = "북두칠성 (Big Dipper)", azimuth = 20f, elevation = 42f, rotation = 0f,
                stars = new[]
                {
                    new Star { name = "Dubhe",  offset = new Vector2(  9.5f,  6.75f), magnitude = 1.8f, tint = new Color(1f, 0.85f, 0.65f) },
                    new Star { name = "Merak",  offset = new Vector2( 11.4f,  1.38f), magnitude = 2.4f },
                    new Star { name = "Phecda", offset = new Vector2(  4.4f, -1.31f), magnitude = 2.4f },
                    new Star { name = "Megrez", offset = new Vector2(  1.1f,  2.03f), magnitude = 3.3f },
                    new Star { name = "Alioth", offset = new Vector2( -4.2f,  0.96f), magnitude = 1.8f },
                    new Star { name = "Mizar",  offset = new Vector2( -8.6f, -0.07f), magnitude = 2.2f },
                    new Star { name = "Alkaid", offset = new Vector2(-13.3f, -5.69f), magnitude = 1.9f, tint = blue },
                },
                edges = new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 0),
                    new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(5, 6),
                },
            },
            new Constellation
            {
                name = "카시오페이아 (Cassiopeia)", azimuth = -50f, elevation = 48f, rotation = 15f,
                stars = new[]
                {
                    new Star { name = "Caph",    offset = new Vector2( 6.5f, -0.85f), magnitude = 2.3f },
                    new Star { name = "Schedar", offset = new Vector2( 2.7f, -3.46f), magnitude = 2.2f, tint = new Color(1f, 0.8f, 0.6f) },
                    new Star { name = "Navi",    offset = new Vector2( 0.4f,  0.72f), magnitude = 2.4f, tint = blue },
                    new Star { name = "Ruchbah", offset = new Vector2(-3.2f,  0.24f), magnitude = 2.7f },
                    new Star { name = "Segin",   offset = new Vector2(-6.1f,  3.67f), magnitude = 3.4f, tint = blue },
                },
                edges = new[] { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 4) },
            },
            new Constellation
            {
                name = "오리온 (Orion)", azimuth = 180f, elevation = 38f, rotation = 0f,
                stars = new[]
                {
                    new Star { name = "Betelgeuse", offset = new Vector2(-4.76f,  8.41f), magnitude = 0.5f, tint = orange },
                    new Star { name = "Bellatrix",  offset = new Vector2( 2.68f,  7.35f), magnitude = 1.6f, tint = blue },
                    new Star { name = "Alnitak",    offset = new Vector2(-1.20f, -0.94f), magnitude = 1.8f, tint = blue },
                    new Star { name = "Alnilam",    offset = new Vector2( 0.00f, -0.20f), magnitude = 1.7f, tint = blue },
                    new Star { name = "Mintaka",    offset = new Vector2( 1.05f,  0.70f), magnitude = 2.2f, tint = blue },
                    new Star { name = "Saiph",      offset = new Vector2(-2.96f, -8.67f), magnitude = 2.1f, tint = blue },
                    new Star { name = "Rigel",      offset = new Vector2( 5.35f, -7.20f), magnitude = 0.1f, tint = blue },
                },
                edges = new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 4),
                    new Vector2Int(2, 3), new Vector2Int(3, 4), new Vector2Int(2, 5), new Vector2Int(4, 6),
                },
            },
        };
    }
}

// Mid_StarField.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Sky
// 밤하늘의 별 수천 개를 반복문으로 만든 메시 하나에 담음 (별 하나 = 하늘 구 가운데를 향한 작은 사각형 = 삼각형 2개)
// 별 종류
//   1. 하늘 전체에 흩어진 별: 어두운 별이 훨씬 많고 밝은 별은 드묾 (실제 별의 등급 분포를 흉내)
//   2. 은하수 띠를 따라 몰린 희미한 별
//   3. 별자리 별: Mid_Constellations의 실제 별 데이터
// 별의 색(온도), 밝기, 반짝이는 박자는 정점 데이터로 넘기고, 모양(둥근 빛, 십자 빛살)은 Mid_StarField 셰이더가 그림
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_StarField : MonoBehaviour
{
    [Header("별 개수")]
    [SerializeField] int scatteredStars = 2600;
    [SerializeField] int milkyWayStars = 2200;
    [SerializeField] int seed = 2376021;              // 같은 seed = 매번 같은 하늘

    [Header("하늘 구")]
    [SerializeField] float radius = 400f;

    [Header("은하수 띠")]
    [SerializeField] float bandTilt = 70f;            // 띠가 가장 높이 올라가는 고도(°)
    [SerializeField] float bandYaw = 35f;             // 띠의 방향(°)
    [SerializeField] float bandWidth = 6f;            // 띠의 두께(°)
    [SerializeField] Material skyMaterial;            // 하늘 돔 재질에 같은 띠 방향을 알려 줌

    [Header("별 크기 (°)")]
    [SerializeField] float minSize = 0.12f;
    [SerializeField] float maxSize = 0.7f;

    Mesh mesh;
    bool dirty;

    // 은하수 띠가 이루는 큰 원의 법선
    public Vector3 BandNormal => Quaternion.Euler(0f, bandYaw, bandTilt) * Vector3.up;

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<Color> colors = new List<Color>();
    readonly List<Vector2> corners = new List<Vector2>();
    readonly List<Vector4> data = new List<Vector4>();
    readonly List<int> tris = new List<int>();

    void OnEnable()
    {
        mesh = new Mesh { name = "Mid_StarField", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        GetComponent<MeshFilter>().sharedMesh = mesh;
        Rebuild();
    }

    void OnValidate() { dirty = true; }

    void Update()
    {
        if (!dirty) return;
        dirty = false;
        Rebuild();
    }

    public void Rebuild()
    {
        if (mesh == null) return;
        verts.Clear(); colors.Clear(); corners.Clear(); data.Clear(); tris.Clear();
        System.Random rng = new System.Random(seed);

        // 1. 하늘 전체에 흩어진 별
        for (int i = 0; i < scatteredStars; i++)
        {
            Vector3 dir = RandomDirection(rng);
            if (dir.y < -0.05f) continue;                                   // 땅 밑은 건너뜀
            float m = 6.5f - 5.5f * Mathf.Pow((float)rng.NextDouble(), 6f); // 대부분 5~6.5등급, 드물게 1등급
            AddStar(dir, m, StarColor(rng), (float)rng.NextDouble());
        }

        // 2. 은하수 띠를 따라 몰린 별
        Vector3 n = BandNormal;
        Vector3 a = Vector3.Cross(n, Vector3.forward).normalized;
        if (a.sqrMagnitude < 0.01f) a = Vector3.Cross(n, Vector3.right).normalized;
        Vector3 b = Vector3.Cross(n, a);
        for (int i = 0; i < milkyWayStars; i++)
        {
            float theta = (float)rng.NextDouble() * 2f * Mathf.PI;
            // 띠를 따라 짙은 곳과 옅은 곳이 번갈아 나오게 함
            float clump = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(3f * theta + 1.3f)) * (0.5f + 0.5f * Mathf.Sin(7f * theta));
            if (rng.NextDouble() > clump) continue;

            float off = Gaussian(rng) * bandWidth * Mathf.Deg2Rad;          // 띠 가운데에서 벗어난 각도
            Vector3 onCircle = a * Mathf.Cos(theta) + b * Mathf.Sin(theta);
            Vector3 dir = (onCircle * Mathf.Cos(off) + n * Mathf.Sin(off)).normalized;
            if (dir.y < -0.05f) continue;

            float m = 6.8f - 2.8f * Mathf.Pow((float)rng.NextDouble(), 4f);  // 대부분 아주 희미함
            AddStar(dir, m, Color.Lerp(Color.white, new Color(0.75f, 0.82f, 1f), (float)rng.NextDouble()), (float)rng.NextDouble());
        }

        // 3. 별자리 별
        Mid_Constellations constellations = GetComponent<Mid_Constellations>();
        if (constellations != null && constellations.Items != null)
        {
            foreach (Mid_Constellations.Constellation c in constellations.Items)
            {
                if (c.stars == null) continue;
                foreach (Mid_Constellations.Star s in c.stars)
                    AddStar(Mid_Constellations.StarDirection(c, s.offset), s.magnitude, s.tint, (float)rng.NextDouble());
            }
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetUVs(0, corners);
        mesh.SetUVs(1, data);
        mesh.SetTriangles(tris, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);

        if (skyMaterial != null) skyMaterial.SetVector("_BandNormal", BandNormal);
    }

    // 별 하나 = 하늘 구 위의 작은 사각형 (정점 4개, 삼각형 2개)
    void AddStar(Vector3 dir, float magnitude, Color tint, float random01)
    {
        // 등급이 5 작아지면 밝기는 100배 (실제 별의 밝기 규칙). 6등급 = 1
        float flux = Mathf.Pow(10f, -0.4f * (magnitude - 6f));
        float intensity = 0.08f * Mathf.Pow(flux, 0.75f);                    // 너무 차이 나지 않게 조금 눌러 줌
        float bright01 = Mathf.Clamp01(Mathf.Log10(flux) / 2.6f);           // 0 = 아주 희미, 1 = 아주 밝음
        float size = Mathf.Lerp(minSize, maxSize, bright01);
        // 희미한 별은 사각형이 몇 픽셀뿐이라 부드러운 점으로, 밝은 별은 날카로운 점 + 빛무리로
        float sharpness = Mathf.Lerp(3f, 22f, bright01);
        float spikes = Mathf.Clamp01((2.3f - magnitude) / 2f);               // 2등급보다 밝은 별만 십자 빛살

        // 별 자리에서 하늘 구에 붙은 오른쪽·위쪽 방향
        Vector3 right = Vector3.Cross(Mathf.Abs(dir.y) > 0.99f ? Vector3.forward : Vector3.up, dir).normalized;
        Vector3 up = Vector3.Cross(dir, right);
        float half = radius * Mathf.Tan(size * Mathf.Deg2Rad);
        Vector3 c = dir * radius;

        int start = verts.Count;
        verts.Add(c - right * half - up * half); corners.Add(new Vector2(-1f, -1f));
        verts.Add(c - right * half + up * half); corners.Add(new Vector2(-1f,  1f));
        verts.Add(c + right * half + up * half); corners.Add(new Vector2( 1f,  1f));
        verts.Add(c + right * half - up * half); corners.Add(new Vector2( 1f, -1f));

        // x = 반짝이는 박자(위상), y = 밝기, z = 빛살 세기, w = 가운데 점의 날카로움
        Vector4 d = new Vector4(random01 * 2f * Mathf.PI, intensity, spikes, sharpness);
        for (int k = 0; k < 4; k++) { colors.Add(tint); data.Add(d); }

        tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
        tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
    }

    // 구 위에 고르게 퍼진 방향
    static Vector3 RandomDirection(System.Random rng)
    {
        float z = (float)rng.NextDouble() * 2f - 1f;
        float phi = (float)rng.NextDouble() * 2f * Mathf.PI;
        float r = Mathf.Sqrt(1f - z * z);
        return new Vector3(r * Mathf.Cos(phi), z, r * Mathf.Sin(phi));
    }

    // 평균 0, 표준편차 1인 정규분포 (Box-Muller)
    static float Gaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
    }

    // 별의 온도에 따른 색: 푸른 흰색이 많고, 노란색·주황색은 적음
    static Color StarColor(System.Random rng)
    {
        double p = rng.NextDouble();
        if (p < 0.30) return new Color(0.72f, 0.82f, 1f);     // 푸른 흰색
        if (p < 0.70) return new Color(1f, 1f, 1f);           // 흰색
        if (p < 0.90) return new Color(1f, 0.93f, 0.78f);     // 노란 흰색
        return new Color(1f, 0.74f, 0.5f);                    // 주황색
    }
}

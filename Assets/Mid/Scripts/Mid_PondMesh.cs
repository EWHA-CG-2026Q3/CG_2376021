// Mid_PondMesh.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Ground > Mid_Pond
// 정자 앞의 연못 — 반복문으로 만든 수면 + 테두리 돌
//   수면: 가운데 점 + 둘레 점 segments개로 만든 부채꼴. 둘레의 반지름을 sin 몇 개로 흔들어 자연스러운 모양
//   테두리 돌: 둘레를 따라 stoneCount개. 돌 하나 = 윗면이 좁은 n각기둥(등롱 몸통과 같은 방식)을 무작위로 찌그러뜨림
// 서브메시 0 = 수면, 서브메시 1 = 돌 → MeshRenderer의 Materials에 재질 2개
// 기말: 수면 재질을 직접 쓴 물 셰이더로 바꾸고, 연꽃(곡면)을 띄울 자리
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_PondMesh : MonoBehaviour
{
    [Header("수면")]
    [SerializeField] float radius = 3.2f;
    [SerializeField, Range(8, 128)] int segments = 64;
    [SerializeField] float waterHeight = 0.03f;      // 땅보다 살짝 위

    [Header("테두리 돌")]
    [SerializeField] int stoneCount = 28;
    [SerializeField] float stoneSize = 0.42f;
    [SerializeField, Range(4, 12)] int stoneSides = 7;
    [SerializeField] int seed = 11;

    bool dirty;

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> water = new List<int>();
    readonly List<int> stones = new List<int>();

    void OnEnable() { Build(); }

    // Inspector 값이 바뀌면 다음 Update에서 다시 만듦 (OnValidate 안에서는 메시를 바로 바꾸지 않음)
    void OnValidate() { dirty = true; }

    void Update()
    {
        if (!dirty) return;
        dirty = false;
        Build();
    }

    // 각도 a에서 연못 둘레까지의 반지름
    float Outline(float a)
    {
        return radius * (1f + 0.14f * Mathf.Sin(3f * a + 0.7f) + 0.08f * Mathf.Sin(5f * a + 2.1f) + 0.04f * Mathf.Sin(9f * a));
    }

    void Build()
    {
        verts.Clear(); water.Clear(); stones.Clear();

        // 1. 수면: 위에서 봤을 때 시계 방향
        Vector3 center = new Vector3(0f, waterHeight, 0f);
        for (int i = 0; i < segments; i++)
        {
            float a0 = 2f * Mathf.PI * i / segments;
            float a1 = 2f * Mathf.PI * (i + 1) / segments;
            Vector3 p0 = Dir(a0) * Outline(a0) + Vector3.up * waterHeight;
            Vector3 p1 = Dir(a1) * Outline(a1) + Vector3.up * waterHeight;
            Tri(water, center, p1, p0);
        }

        // 2. 테두리 돌
        System.Random rng = new System.Random(seed);
        for (int s = 0; s < stoneCount; s++)
        {
            float a = 2f * Mathf.PI * (s + (float)rng.NextDouble() * 0.4f) / stoneCount;
            float size = stoneSize * (0.7f + 0.6f * (float)rng.NextDouble());
            Vector3 pos = Dir(a) * (Outline(a) + size * 0.25f);
            AddStone(pos, size, (float)rng.NextDouble() * 360f, rng);
        }

        Mesh mesh = new Mesh { name = "Mid_Pond" };
        mesh.SetVertices(verts);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(water, 0);
        mesh.SetTriangles(stones, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // 돌 하나 = 윗면이 좁은 n각기둥, 둘레의 점마다 반지름과 높이를 조금씩 다르게
    void AddStone(Vector3 pos, float size, float spinDeg, System.Random rng)
    {
        float height = size * (0.35f + 0.25f * (float)rng.NextDouble());
        float yBottom = -0.05f;                            // 땅에 살짝 묻힘
        float spin = spinDeg * Mathf.Deg2Rad;

        Vector3[] bottom = new Vector3[stoneSides];
        Vector3[] top = new Vector3[stoneSides];
        for (int i = 0; i < stoneSides; i++)
        {
            float a = spin + 2f * Mathf.PI * i / stoneSides;
            float r = size * 0.5f * (0.75f + 0.5f * (float)rng.NextDouble());
            float topScale = 0.55f + 0.2f * (float)rng.NextDouble();
            bottom[i] = pos + Dir(a) * r + Vector3.up * yBottom;
            top[i] = pos + Dir(a) * (r * topScale) + Vector3.up * (height * (0.85f + 0.3f * (float)rng.NextDouble()));
        }
        Vector3 topCenter = pos + Vector3.up * height;

        for (int i = 0; i < stoneSides; i++)
        {
            int j = (i + 1) % stoneSides;
            // 옆면: 바깥에서 봤을 때 시계 방향
            Tri(stones, bottom[i], top[i], top[j]);
            Tri(stones, bottom[i], top[j], bottom[j]);
            // 윗면: 위에서 봤을 때 시계 방향
            Tri(stones, topCenter, top[j], top[i]);
        }
    }

    // 삼각형마다 정점을 따로 두어 면마다 평평한 법선 (돌의 각진 면이 보이게)
    void Tri(List<int> sub, Vector3 a, Vector3 b, Vector3 c)
    {
        sub.Add(verts.Count); verts.Add(a);
        sub.Add(verts.Count); verts.Add(b);
        sub.Add(verts.Count); verts.Add(c);
    }

    static Vector3 Dir(float a) { return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); }
}

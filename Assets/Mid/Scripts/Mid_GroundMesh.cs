// Mid_GroundMesh.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Ground > Mid_Earth
// 정자 둘레의 어두운 흙바닥 — 반복문으로 만든 큰 원판 (가운데 점 + 동심원 rings개 × 원 위의 점 segments개)
//   정자 둘레(flatRadius 안)는 평평하게, 그 바깥은 노이즈로 완만한 언덕
//   가장자리로 갈수록 땅이 내려가 지평선 아래로 숨음 → 땅의 끝선이 하늘과 맞닿지 않음
// 원은 가운데 쪽일수록 촘촘하게 나눔 (가까운 곳은 자세히, 먼 곳은 성기게)
// 같은 메시를 MeshCollider에도 넣어 SceneWalker가 위를 걸을 수 있게 함
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_GroundMesh : MonoBehaviour
{
    [Header("원판")]
    [SerializeField] float radius = 70f;
    [SerializeField, Range(4, 80)] int rings = 48;
    [SerializeField, Range(8, 256)] int segments = 96;

    [Header("높낮이")]
    [SerializeField] float flatRadius = 13f;       // 이 안은 평평 (정자, 연못 자리)
    [SerializeField] float hillHeight = 1.2f;      // 언덕 높이
    [SerializeField] float hillScale = 0.06f;      // 언덕 무늬의 촘촘함
    [SerializeField] float edgeDrop = 4f;          // 가장자리에서 내려가는 깊이

    bool dirty;

    void OnEnable() { Build(); }

    // Inspector 값이 바뀌면 다음 Update에서 다시 만듦 (OnValidate 안에서는 메시를 바로 바꾸지 않음)
    void OnValidate() { dirty = true; }

    void Update()
    {
        if (!dirty) return;
        dirty = false;
        Build();
    }

    void Build()
    {
        // 0번 = 가운데, 그 뒤로 동심원마다 segments개
        Vector3[] vertices = new Vector3[1 + rings * segments];
        vertices[0] = Vector3.zero;
        for (int k = 1; k <= rings; k++)
        {
            float r = radius * Mathf.Pow((float)k / rings, 1.6f);   // 가운데 쪽이 촘촘
            for (int s = 0; s < segments; s++)
            {
                float a = 2f * Mathf.PI * s / segments;
                float x = Mathf.Cos(a) * r;
                float z = Mathf.Sin(a) * r;
                vertices[1 + (k - 1) * segments + s] = new Vector3(x, Height(x, z, r), z);
            }
        }

        // 위에서 봤을 때 시계 방향 = 앞면 (땅이 위를 향함)
        int[] triangles = new int[segments * 3 + (rings - 1) * segments * 6];
        int t = 0;
        for (int s = 0; s < segments; s++)                      // 가운데 부채꼴
        {
            int p0 = 1 + s;
            int p1 = 1 + (s + 1) % segments;
            triangles[t++] = 0; triangles[t++] = p1; triangles[t++] = p0;
        }
        for (int k = 1; k < rings; k++)                         // 동심원 사이의 사각형
        {
            for (int s = 0; s < segments; s++)
            {
                int in0 = 1 + (k - 1) * segments + s;
                int in1 = 1 + (k - 1) * segments + (s + 1) % segments;
                int out0 = in0 + segments;
                int out1 = in1 + segments;
                triangles[t++] = in0; triangles[t++] = in1; triangles[t++] = out1;
                triangles[t++] = in0; triangles[t++] = out1; triangles[t++] = out0;
            }
        }

        Mesh mesh = new Mesh { name = "Mid_Ground" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null) col.sharedMesh = mesh;
    }

    // 점 (x, z)의 높이: 평평한 안쪽 → 완만한 언덕 → 가장자리에서 내려감
    float Height(float x, float z, float r)
    {
        float hills = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flatRadius, flatRadius + 15f, r));
        float noise = Mathf.PerlinNoise(x * hillScale + 100f, z * hillScale + 100f) * 2f - 1f;
        float drop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius * 0.6f, radius, r));
        return hills * noise * hillHeight - drop * edgeDrop;
    }
}

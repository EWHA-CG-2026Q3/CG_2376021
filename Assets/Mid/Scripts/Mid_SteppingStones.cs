// Mid_SteppingStones.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Ground > Mid_Path
// 정자 계단에서 연못까지 놓인 디딤돌 — 시작점과 끝점 사이에 stoneCount개를 고르게 놓음
// 디딤돌 하나 = 납작한 n각기둥, 둘레의 점마다 반지름을 조금씩 다르게 해 자연스러운 돌 모양
// 길은 살짝 휘어지게: 가운데에서 가장 많이 옆으로 밀리고 양 끝에서는 그대로 (sin(π t))
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_SteppingStones : MonoBehaviour
{
    [SerializeField] Vector3 start = new Vector3(0f, 0f, -5.3f);   // 월드 좌표 (Mid_Path의 Transform은 기본값)
    [SerializeField] Vector3 end = new Vector3(-2.6f, 0f, -6.2f);
    [SerializeField] float bend = 0.4f;                          // 길이 옆으로 휘는 정도
    [SerializeField] int stoneCount = 5;
    [SerializeField] float stoneSize = 0.5f;
    [SerializeField, Range(4, 12)] int sides = 8;
    [SerializeField] int seed = 5;

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> tris = new List<int>();
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

    // 길 위의 점 (t = 0 시작, 1 끝)
    public Vector3 PointOnPath(float t)
    {
        Vector3 along = end - start;
        Vector3 side = Vector3.Cross(Vector3.up, along).normalized;
        return start + along * t + side * (Mathf.Sin(Mathf.PI * t) * bend);
    }

    void Build()
    {
        verts.Clear(); tris.Clear();
        System.Random rng = new System.Random(seed);

        for (int s = 0; s < stoneCount; s++)
        {
            float t = stoneCount == 1 ? 0.5f : (float)s / (stoneCount - 1);
            Vector3 c = PointOnPath(t);
            float size = stoneSize * (0.8f + 0.4f * (float)rng.NextDouble());
            float spin = (float)rng.NextDouble() * 2f * Mathf.PI;
            float h = 0.05f + 0.03f * (float)rng.NextDouble();

            Vector3[] bottom = new Vector3[sides];
            Vector3[] top = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = spin + 2f * Mathf.PI * i / sides;
                float r = size * 0.5f * (0.8f + 0.35f * (float)rng.NextDouble());
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                bottom[i] = c + dir * r + Vector3.up * -0.02f;
                top[i] = c + dir * (r * 0.92f) + Vector3.up * h;
            }
            Vector3 topCenter = c + Vector3.up * h;

            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Tri(bottom[i], top[i], top[j]);        // 옆면: 바깥에서 봤을 때 시계 방향
                Tri(bottom[i], top[j], bottom[j]);
                Tri(topCenter, top[j], top[i]);        // 윗면: 위에서 봤을 때 시계 방향
            }
        }

        Mesh mesh = new Mesh { name = "Mid_SteppingStones" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void Tri(Vector3 a, Vector3 b, Vector3 c)
    {
        tris.Add(verts.Count); verts.Add(a);
        tris.Add(verts.Count); verts.Add(b);
        tris.Add(verts.Count); verts.Add(c);
    }
}

// Mid_StoneLampMesh.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Ground > Mid_Path > Mid_StoneLamp_*
// 길과 연못가를 밝히는 석등 — 위아래 반지름이 다른 n각기둥(절두체) 여러 층을 반복문으로 쌓음
//   받침 → 기둥 → 받침돌 → 불창(빛나는 부분) → 지붕 → 꼭지
// 기준점(0, 0, 0) = 바닥 가운데. 불창의 가운데 높이(LightHeight)에 Point Light를 둠
// 서브메시 0 = 돌, 서브메시 1 = 불창 → MeshRenderer의 Materials에 재질 2개
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_StoneLampMesh : MonoBehaviour
{
    [SerializeField, Range(3, 16)] int sides = 8;

    // 층마다: 아래 반지름, 위 반지름, 높이, 서브메시(0 = 돌, 1 = 불창)
    static readonly Vector4[] layers =
    {
        new Vector4(0.22f, 0.18f, 0.08f, 0),   // 받침
        new Vector4(0.07f, 0.07f, 0.45f, 0),   // 기둥
        new Vector4(0.07f, 0.16f, 0.06f, 0),   // 받침돌
        new Vector4(0.12f, 0.12f, 0.16f, 1),   // 불창
        new Vector4(0.21f, 0.04f, 0.12f, 0),   // 지붕
        new Vector4(0.04f, 0.01f, 0.05f, 0),   // 꼭지
    };

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<int> stone = new List<int>();
    readonly List<int> glow = new List<int>();

    // 불창의 가운데 높이
    public static float LightHeight => layers[0].z + layers[1].z + layers[2].z + layers[3].z * 0.5f;

    void OnEnable() { Build(); }

    void Build()
    {
        verts.Clear(); stone.Clear(); glow.Clear();

        float y = 0f;
        foreach (Vector4 layer in layers)
        {
            AddFrustum(y, layer.x, layer.y, layer.z, layer.w < 0.5f ? stone : glow);
            y += layer.z;
        }

        Mesh mesh = new Mesh { name = "Mid_StoneLamp" };
        mesh.SetVertices(verts);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(stone, 0);
        mesh.SetTriangles(glow, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // 높이 y에서 시작하는 n각 절두체 한 층: 옆면 + 아랫면 + 윗면
    void AddFrustum(float y, float rBottom, float rTop, float h, List<int> sub)
    {
        Vector3 bottomCenter = new Vector3(0f, y, 0f);
        Vector3 topCenter = new Vector3(0f, y + h, 0f);
        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * (i + 1) / sides;
            Vector3 b0 = Ring(rBottom, a0, y), b1 = Ring(rBottom, a1, y);
            Vector3 t0 = Ring(rTop, a0, y + h), t1 = Ring(rTop, a1, y + h);

            // 바깥에서 봤을 때 시계 방향 = 앞면
            Tri(sub, b0, t0, t1);
            Tri(sub, b0, t1, b1);
            Tri(sub, bottomCenter, b0, b1);   // 아랫면: 아래에서 봤을 때 시계 방향
            Tri(sub, topCenter, t1, t0);      // 윗면: 위에서 봤을 때 시계 방향
        }
    }

    void Tri(List<int> sub, Vector3 a, Vector3 b, Vector3 c)
    {
        sub.Add(verts.Count); verts.Add(a);
        sub.Add(verts.Count); verts.Add(b);
        sub.Add(verts.Count); verts.Add(c);
    }

    static Vector3 Ring(float r, float a, float y)
    {
        return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
    }
}

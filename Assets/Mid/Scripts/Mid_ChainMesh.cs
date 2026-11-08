// Mid_ChainMesh.cs
// 주마등의 부품(등롱, 회전부, 고정부)이 함께 쓰는 부모 클래스
//   - 자식 클래스는 BuildShape()에서 Tri / AddFrustum / AddRod로 원본 정점과 삼각형을 채움
//   - 원본 정점(BaseVertices)은 부품 자신을 기준으로 잰 값. Mid_MatrixChain이 체인 행렬을 곱해 SetVertices로 넘김
//     (S10 DiamondMesh_Rewinded와 같은 역할 나눔: 이 클래스는 메시를 만들고 보관만, 움직임은 체인이 담당)
//   - 삼각형마다 정점을 따로 두어 면마다 평평한 법선 (각진 나무·종이 느낌)
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public abstract class Mid_ChainMesh : MonoBehaviour
{
    readonly List<Vector3> verts = new List<Vector3>();
    List<int>[] subs;

    Vector3[] baseVertices;
    int[][] submeshTriangles;
    Mesh mesh;
    bool dirty;

    public Vector3[] BaseVertices => baseVertices;

    // Mid_MatrixChain이 follower(Point Light 등)를 옮길 자리 (부품 기준)
    public virtual Vector3 FollowPoint => Vector3.zero;

    // 서브메시 개수 = MeshRenderer의 Materials 개수
    protected abstract int SubmeshCount { get; }

    // 자식 클래스가 모양을 만드는 곳
    protected abstract void BuildShape();

    void OnEnable()
    {
        mesh = new Mesh { name = GetType().Name };
        GetComponent<MeshFilter>().sharedMesh = mesh;
        Rebuild();
    }

    // Inspector 값이 바뀌면 다음 Update에서 다시 만듦 (OnValidate 안에서는 메시를 바로 바꾸지 않음)
    void OnValidate() { dirty = true; }

    void Update()
    {
        if (!dirty || mesh == null) return;
        dirty = false;
        Rebuild();
    }

    void Rebuild()
    {
        verts.Clear();
        subs = new List<int>[SubmeshCount];
        for (int i = 0; i < subs.Length; i++) subs[i] = new List<int>();

        BuildShape();

        baseVertices = verts.ToArray();
        submeshTriangles = new int[subs.Length][];
        for (int i = 0; i < subs.Length; i++) submeshTriangles[i] = subs[i].ToArray();
        SetVertices(baseVertices);
    }

    public void SetVertices(Vector3[] v)
    {
        if (mesh == null || submeshTriangles == null) return;
        mesh.Clear();
        mesh.vertices = v;
        mesh.subMeshCount = submeshTriangles.Length;
        for (int i = 0; i < submeshTriangles.Length; i++) mesh.SetTriangles(submeshTriangles[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // ---------- 모양을 만드는 도구 ----------

    // 정점 3개를 추가하고 그 번호를 서브메시 sub의 삼각형 목록에 넣음
    protected void Tri(int sub, Vector3 a, Vector3 b, Vector3 c)
    {
        subs[sub].Add(verts.Count); verts.Add(a);
        subs[sub].Add(verts.Count); verts.Add(b);
        subs[sub].Add(verts.Count); verts.Add(c);
    }

    // 앞뒤 양면 삼각형 (얇은 판처럼 양쪽에서 보이는 면)
    protected void DoubleTri(int sub, Vector3 a, Vector3 b, Vector3 c)
    {
        Tri(sub, a, b, c);
        Tri(sub, a, c, b);
    }

    // 앞뒤 양면 사각형 (얇은 판처럼 양쪽에서 보이는 면)
    protected void DoubleQuad(int sub, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Tri(sub, p0, p1, p2); Tri(sub, p0, p2, p3);   // 한쪽 면
        Tri(sub, p0, p2, p1); Tri(sub, p0, p3, p2);   // 반대쪽 면 (감는 순서를 뒤집음)
    }

    // 세로로 선 n각 절두체: 바닥 가운데 center, 아래 반지름 rBottom, 위 반지름 rTop, 높이 h
    protected void AddFrustum(int sub, Vector3 center, float rBottom, float rTop, float h, int sides)
    {
        AddPrism(sub, center, center + Vector3.up * h, rBottom, rTop, sides);
    }

    // 두 점 사이를 잇는 가는 n각기둥 (막대, 줄)
    protected void AddRod(int sub, Vector3 from, Vector3 to, float radius, int sides)
    {
        AddPrism(sub, from, to, radius, radius, sides);
    }

    // from에서 to 방향으로 선 n각 절두체: 옆면 + 양 끝 뚜껑
    void AddPrism(int sub, Vector3 from, Vector3 to, float rFrom, float rTo, int sides)
    {
        Vector3 axis = (to - from).normalized;
        // 축에 수직인 두 방향 (원을 그릴 평면)
        // u × v = −axis가 되게 골라야 아래 감는 순서가 "바깥에서 시계 방향"이 됨
        // (축이 위쪽일 때 u = −x, v = −z → 등롱의 Ring(cos, sin)과 같은 방향으로 돎)
        Vector3 u = Vector3.Cross(Mathf.Abs(axis.y) > 0.99f ? Vector3.forward : Vector3.up, axis).normalized;
        Vector3 v = Vector3.Cross(u, axis);

        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides;
            float a1 = 2f * Mathf.PI * (i + 1) / sides;
            Vector3 d0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
            Vector3 d1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
            Vector3 b0 = from + d0 * rFrom, b1 = from + d1 * rFrom;
            Vector3 t0 = to + d0 * rTo, t1 = to + d1 * rTo;

            // 바깥에서 봤을 때 시계 방향 = 앞면
            Tri(sub, b0, t0, t1);
            Tri(sub, b0, t1, b1);
            if (rFrom > 0f) Tri(sub, from, b0, b1);   // from 쪽 뚜껑
            if (rTo > 0f) Tri(sub, to, t1, t0);       // to 쪽 뚜껑
        }
    }
}

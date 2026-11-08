// Mid_WindBellMesh.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_WindBells > Mid_WindBell_*_Bell
// 처마 끝에 매다는 풍경의 종 — 옆모습(반지름, 높이) 점들을 축을 따라 한 바퀴 돌려 만든 회전체 (반복문 2겹)
//   바깥 면 + 안쪽 면(아래에서 올려다봐도 속이 보이도록) + 아래 가장자리 + 위의 고리
// 기준점(0, 0, 0) = 고리 꼭대기(처마에 거는 자리). 종은 그 아래(y < 0)로 내려감
// 체인: Anchor → BellSwing → 이 종 (Mid_MatrixChain)
// 서브메시 0 = 청동, 서브메시 1 = 끈
using UnityEngine;

public class Mid_WindBellMesh : Mid_ChainMesh
{
    [SerializeField, Range(6, 32)] int sides = 16;
    [SerializeField] float hookLength = 0.06f;       // 고리에서 종 꼭대기까지
    [SerializeField] float wallThickness = 0.006f;

    // 종의 옆모습: (반지름, 종 꼭대기에서 내려간 거리), 위에서 아래로
    static readonly Vector2[] profile =
    {
        new Vector2(0.000f, 0.000f),
        new Vector2(0.030f, 0.004f),
        new Vector2(0.042f, 0.018f),
        new Vector2(0.046f, 0.050f),
        new Vector2(0.050f, 0.095f),
        new Vector2(0.056f, 0.125f),
        new Vector2(0.066f, 0.145f),
    };

    // 추를 매다는 자리 (종 안쪽 위, 이 종의 기준점에서 잰 거리) → Mid_WindBell_i_ClapperPivot의 Position y
    public static float ClapperPivotDepth => 0.06f + 0.03f;

    protected override int SubmeshCount => 2;

    protected override void BuildShape()
    {
        float top = -hookLength;   // 종 꼭대기 높이

        for (int k = 0; k < profile.Length - 1; k++)
        {
            Vector2 upper = profile[k];
            Vector2 lower = profile[k + 1];
            for (int i = 0; i < sides; i++)
            {
                float a0 = 2f * Mathf.PI * i / sides;
                float a1 = 2f * Mathf.PI * (i + 1) / sides;

                // 바깥 면: 바깥에서 봤을 때 시계 방향
                Vector3 t0 = Ring(upper.x, a0, top - upper.y), t1 = Ring(upper.x, a1, top - upper.y);
                Vector3 b0 = Ring(lower.x, a0, top - lower.y), b1 = Ring(lower.x, a1, top - lower.y);
                Tri(0, b0, t0, t1);
                Tri(0, b0, t1, b1);

                // 안쪽 면: 조금 작은 반지름, 감는 순서를 뒤집어 안에서 보이게
                float ru = Mathf.Max(0f, upper.x - wallThickness);
                float rl = Mathf.Max(0f, lower.x - wallThickness);
                Vector3 it0 = Ring(ru, a0, top - upper.y), it1 = Ring(ru, a1, top - upper.y);
                Vector3 ib0 = Ring(rl, a0, top - lower.y), ib1 = Ring(rl, a1, top - lower.y);
                Tri(0, ib0, it1, it0);
                Tri(0, ib0, ib1, it1);

                // 아래 가장자리: 바깥 면과 안쪽 면 사이를 막음 (아래에서 봤을 때 시계 방향)
                if (k == profile.Length - 2)
                {
                    Tri(0, ib0, b0, b1);
                    Tri(0, ib0, b1, ib1);
                }
            }
        }

        // 고리와 꼭지
        AddRod(1, Vector3.zero, new Vector3(0f, top, 0f), 0.005f, 4);
        AddFrustum(0, new Vector3(0f, top - 0.004f, 0f), 0.014f, 0.008f, 0.012f, 8);
    }

    static Vector3 Ring(float r, float a, float y)
    {
        return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
    }
}

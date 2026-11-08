// Mid_MatrixChain.cs
// 씬: Mid_Landscape / 오브젝트: 풍경의 부품마다 (Mid_ChainMesh를 상속한 메시 스크립트와 함께)
// 중간 프로젝트 ④ 변환 — Hierarchy 대신 코드로 부모-자식 관계를 만듦 (S10_DiamondChain_Finish 방식)
// chain의 노드는 Hierarchy에서 서로 부모-자식으로 묶지 않음. 이 오브젝트의 Transform은 기본값으로 둠
// 자식이 그려지는 곳 = M_부모 × … × M_자식 × 꼭짓점
// 부품마다 체인의 길이가 다름 → 같은 부모를 공유하는 만큼 함께 움직임
//   종:     Anchor → BellSwing                               → 처마에 매달려 흔들림
//   바람판: Anchor → BellSwing → ClapperPivot → ClapperSwing → 종을 따라 흔들리면서 자기 흔들림을 더함
using UnityEngine;

[ExecuteAlways]
public class Mid_MatrixChain : MonoBehaviour
{
    // 부모부터 차례로 (예: Mid_WindBell_i_Anchor, _BellSwing, _ClapperPivot, _ClapperSwing)
    [SerializeField] Transform[] chain;

    [Header("함께 따라갈 오브젝트 (없으면 비워 둠)")]
    [SerializeField] Transform follower;

    [SerializeField] float axisLength = 0.15f;   // Scene 뷰에 그리는 축의 길이

    // Mid_NodeMotion이 Update에서 값을 넣은 뒤에 실행
    void LateUpdate()
    {
        if (chain == null) return;

        // 1. 부모부터 차례로 오른쪽에 곱해 행렬 하나로 합침
        Matrix4x4 M = Matrix4x4.identity;
        foreach (Transform node in chain)
        {
            if (node == null) continue;
            M = M * Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale);
        }

        // 2. 꼭짓점마다 한 번 곱함
        Mid_ChainMesh part = GetComponent<Mid_ChainMesh>();
        if (part == null) return;
        Vector3[] baseVertices = part.BaseVertices;
        if (baseVertices == null) return;
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = M.MultiplyPoint(baseVertices[i]);
        part.SetVertices(verts);

        // 3. 부품 기준으로 잰 점(등롱 몸통 한가운데)에 같은 행렬을 곱해 조명을 옮김
        if (follower != null)
            follower.position = M.MultiplyPoint(part.FollowPoint);
    }

    // 곱셈 단계마다: 4열 = 기준점, 1–3열 = 축 세 개 (S10과 같음)
    void OnDrawGizmos()
    {
        if (chain == null) return;

        Matrix4x4 M = Matrix4x4.identity;
        Vector3 prevOrigin = Vector3.zero;
        bool first = true;

        foreach (Transform node in chain)
        {
            if (node == null) continue;
            M = M * Matrix4x4.TRS(node.localPosition, node.localRotation, node.localScale);

            Vector3 origin = M.GetColumn(3);                     // 4열: 기준점
            Vector3 e1 = ((Vector3)M.GetColumn(0)).normalized;   // 1열: e₁의 방향
            Vector3 e2 = ((Vector3)M.GetColumn(1)).normalized;   // 2열: e₂의 방향
            Vector3 e3 = ((Vector3)M.GetColumn(2)).normalized;   // 3열: e₃의 방향

            Gizmos.color = Color.red;   Gizmos.DrawLine(origin, origin + e1 * axisLength);
            Gizmos.color = Color.green; Gizmos.DrawLine(origin, origin + e2 * axisLength);
            Gizmos.color = Color.blue;  Gizmos.DrawLine(origin, origin + e3 * axisLength);

            // 부모의 기준점에서 이 단계의 기준점까지 잇는 선
            Gizmos.color = Color.yellow;
            if (!first) Gizmos.DrawLine(prevOrigin, origin);
            Gizmos.DrawWireSphere(origin, axisLength * 0.15f);

            prevOrigin = origin;
            first = false;
        }
    }
}

// Mid_WindPlateMesh.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_WindBells > Mid_WindBell_*_Plate
// 풍경의 추 + 물고기 바람판 — 바람판이 바람을 받아 흔들리면 추가 종 안쪽을 쳐서 소리가 남
//   끈 → 추(종 안의 작은 원기둥) → 긴 끈 → 물고기 모양 판(가운데 점 + 둘레 점들로 만든 부채꼴, 양면)
// 기준점(0, 0, 0) = 추를 매다는 자리(종 안쪽 위)
// 체인: Anchor → BellSwing → ClapperPivot → ClapperSwing → 이 판
//   부모(종)가 흔들리면 그대로 따라가면서, 자기 흔들림(ClapperSwing)을 한 번 더 더함
// 서브메시 0 = 청동, 서브메시 1 = 끈
using UnityEngine;

public class Mid_WindPlateMesh : Mid_ChainMesh
{
    [SerializeField] float clapperDepth = 0.08f;     // 기준점에서 추 윗면까지
    [SerializeField] float plateDepth = 0.24f;       // 기준점에서 물고기 머리까지
    [SerializeField] float plateWidth = 0.05f;       // 물고기 몸통 반폭
    [SerializeField] float plateLength = 0.11f;      // 물고기 몸통 길이 (머리 ~ 꼬리 시작)
    [SerializeField, Range(6, 32)] int outlinePoints = 14;

    protected override int SubmeshCount => 2;

    protected override void BuildShape()
    {
        // 추
        AddRod(1, Vector3.zero, Vector3.down * clapperDepth, 0.003f, 4);
        AddFrustum(0, Vector3.down * (clapperDepth + 0.025f), 0.016f, 0.016f, 0.025f, 8);

        // 추에서 바람판까지 긴 끈
        AddRod(1, Vector3.down * (clapperDepth + 0.025f), Vector3.down * plateDepth, 0.003f, 4);

        // 물고기 몸통: 타원의 둘레 점들 (머리가 위, 꼬리 쪽은 좁게)
        Vector3 center = Vector3.down * (plateDepth + plateLength * 0.5f);
        Vector3[] outline = new Vector3[outlinePoints];
        for (int i = 0; i < outlinePoints; i++)
        {
            float a = 2f * Mathf.PI * i / outlinePoints;              // 0 = 오른쪽, 90° = 위(머리)
            float x = Mathf.Cos(a) * plateWidth;
            float y = Mathf.Sin(a) * plateLength * 0.5f;
            if (y < 0f) x *= 1f + y / (plateLength * 0.5f) * 0.55f;  // 아래(꼬리 쪽)로 갈수록 좁아짐
            outline[i] = center + new Vector3(x, y, 0f);
        }
        for (int i = 0; i < outlinePoints; i++)
            DoubleTri(0, center, outline[i], outline[(i + 1) % outlinePoints]);

        // 꼬리: 몸통 아래 끝에서 양옆으로 벌어진 지느러미 두 장
        Vector3 tailRoot = center + Vector3.down * (plateLength * 0.5f);
        Vector3 tailL = tailRoot + new Vector3(-plateWidth * 0.8f, -plateLength * 0.35f, 0f);
        Vector3 tailR = tailRoot + new Vector3( plateWidth * 0.8f, -plateLength * 0.35f, 0f);
        Vector3 notch = tailRoot + new Vector3(0f, -plateLength * 0.18f, 0f);
        DoubleTri(0, tailRoot, tailL, notch);
        DoubleTri(0, tailRoot, notch, tailR);
    }
}

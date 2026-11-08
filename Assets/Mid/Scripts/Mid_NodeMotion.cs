// Mid_NodeMotion.cs
// 씬: Mid_Landscape / 오브젝트: Mid_Mobile_Spin, Mid_Lantern_*_Swing
// S10에서 Animation 클립이 하던 일(Rotation 값을 시간에 따라 바꾸기)을 코드로 함
// 이 오브젝트의 localRotation만 바꿈 — 실제로 등롱을 움직이는 것은 Mid_MatrixChain의 행렬 곱
// 빙글 돌기: 각도 = 속도 × 시간 / 흔들리기(진자): 각도 = 진폭 × sin(2π × 시간 / 주기 + 위상)
using UnityEngine;

public class Mid_NodeMotion : MonoBehaviour
{
    [Header("빙글 돌기")]
    [SerializeField] Vector3 spinAxis = Vector3.up;
    [SerializeField] float spinSpeed = 0f;          // 초당 각도(°)

    [Header("흔들리기 (진자)")]
    [SerializeField] Vector3 swingAxis = Vector3.forward;
    [SerializeField] float swingAngle = 0f;         // 최대 기울기(°)
    [SerializeField] float swingPeriod = 2.5f;      // 한 번 왕복하는 데 걸리는 시간(초)
    [SerializeField] float swingPhase = 0f;         // 등롱마다 박자를 다르게(°)

    Quaternion baseRotation;

    void OnEnable() { baseRotation = transform.localRotation; }
    void OnDisable() { transform.localRotation = baseRotation; }

    void Update()
    {
        float t = Time.time;
        float spin = spinSpeed * t;
        float swing = swingAngle * Mathf.Sin(2f * Mathf.PI * t / swingPeriod + swingPhase * Mathf.Deg2Rad);

        transform.localRotation = baseRotation
                                * Quaternion.AngleAxis(spin, spinAxis)
                                * Quaternion.AngleAxis(swing, swingAxis);
    }
}

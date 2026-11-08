// Mid_SkyDome.cs
// 씬: Mid_Landscape / 오브젝트: Mid > Mid_Sky > Mid_SkyDome
// 장면 전체를 감싸는 하늘 구 — 반복문으로 위도(rings) × 경도(segments) 격자를 만들어 사각형마다 삼각형 2개
// 안쪽에서 보는 구이므로 삼각형을 "안쪽에서 봤을 때 시계 방향"으로 감음
// 색(밤하늘 그라데이션, 은하수, 성운)은 Mid_NightSky 셰이더가 방향마다 계산
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_SkyDome : MonoBehaviour
{
    [SerializeField] float radius = 450f;            // 별(400)보다 바깥
    [SerializeField, Range(8, 64)] int rings = 32;   // 위도 방향 칸 수
    [SerializeField, Range(8, 128)] int segments = 64; // 경도 방향 칸 수

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
        Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
        for (int r = 0; r <= rings; r++)
        {
            float lat = Mathf.PI * r / rings - Mathf.PI * 0.5f;      // -90° ~ 90°
            for (int s = 0; s <= segments; s++)
            {
                float lon = 2f * Mathf.PI * s / segments;
                vertices[r * (segments + 1) + s] = new Vector3(
                    Mathf.Cos(lat) * Mathf.Cos(lon),
                    Mathf.Sin(lat),
                    Mathf.Cos(lat) * Mathf.Sin(lon)) * radius;
            }
        }

        int[] triangles = new int[rings * segments * 6];
        int t = 0;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int bl = r * (segments + 1) + s;       // 아래 왼쪽 (바깥에서 봤을 때)
                int br = bl + 1;
                int tl = bl + segments + 1;
                int tr = tl + 1;
                // 바깥에서 봤을 때의 시계 방향(bl, tl, tr)을 거꾸로 감아 안쪽이 앞면이 되게 함
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = tl;
                triangles[t++] = bl; triangles[t++] = br; triangles[t++] = tr;
            }
        }

        Mesh mesh = new Mesh { name = "Mid_SkyDome" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}

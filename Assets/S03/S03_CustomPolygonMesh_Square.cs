using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Square : MonoBehaviour
{
    void Start()
    {
        // TODO 1: 원하는 다각형의 정점 좌표를 채우세요 (최소 4개)
        Vector3[] vertices = new Vector3[]
        {
              new Vector3(0f, 0f, 0f), //0
              new Vector3(1f, 0f, 0f), //1
              new Vector3(1f, 1f, 0f), //2
              new Vector3(0f, 1f, 0f), //3
              new Vector3(0.5f, 1.5f, 0f) //4
        };

        // TODO 2: 정점 3개씩 묶어 삼각형들을 구성하세요
        int[] triangles = new int[]
        {
              2, 1, 0,
              3, 2, 0,
              2, 3, 4 //winding order 주의!!
        }; //과제 : 집 모양 다각형 생성

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
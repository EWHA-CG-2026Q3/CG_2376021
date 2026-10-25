using System.Linq;
using UnityEngine;

// S03-S04 방식으로 정점과 삼각형을 직접 정의한 정물 메시.
// 씬은 아래 정의로 만든 Meshes 폴더의 저장된 메시를 사용한다.
public static class S12_CustomMeshes
{
    public static Mesh CreateTetrahedron()
    {
        return CreateMesh("S12_Tetrahedron", new[] {
            new Vector3(-.6f,0,-.4f), new Vector3(.6f,0,-.4f),
            new Vector3(0,0,.65f), new Vector3(0,1.05f,0)
        }, new[] {0,1,2, 0,3,1, 1,3,2, 2,3,0});
    }

    public static Mesh CreatePyramid()
    {
        return CreateMesh("S12_Pyramid", new[] {
            new Vector3(-.55f,0,-.55f), new Vector3(.55f,0,-.55f),
            new Vector3(.55f,0,.55f), new Vector3(-.55f,0,.55f),
            new Vector3(0,1.15f,0)
        }, new[] {0,1,2,0,2,3, 0,4,1,1,4,2,2,4,3,3,4,0});
    }

    public static Mesh CreateDiamond()
    {
        return CreateMesh("S12_Diamond", new[] {
            new Vector3(-.5f,.65f,0), new Vector3(0,.65f,-.5f),
            new Vector3(.5f,.65f,0), new Vector3(0,.65f,.5f),
            new Vector3(0,1.4f,0), Vector3.zero
        }, new[] {0,4,1,1,4,2,2,4,3,3,4,0, 0,1,5,1,2,5,2,3,5,3,0,5});
    }

    static Mesh CreateMesh(string name, Vector3[] vertices, int[] triangles)
    {
        // 삼각형마다 정점을 분리하여 평평한 면 법선을 만든다.
        var mesh = new Mesh { name = name };
        mesh.vertices = triangles.Select(i => vertices[i]).ToArray();
        mesh.triangles = Enumerable.Range(0, triangles.Length).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

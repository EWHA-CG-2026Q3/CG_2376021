// Mid_SceneBuilder.cs
// 메뉴: CG > Mid > Rebuild Mid Objects
// 지금 열려 있는 씬(Mid_Landscape)의 중간 프로젝트 오브젝트를 지우고 다시 만듦
// 모든 오브젝트는 Mid 하나 아래에 정리됨
//   Mid
//   ├ Mid_WindBells      — 처마 네 귀퉁이의 풍경: 종 + 추·물고기 바람판
//   │                       ① 반복문 메시 + ④ 코드로 만든 부모-자식(Mid_MatrixChain)
//   │                       (정리용 묶음일 뿐, 노드끼리는 Hierarchy로 잇지 않고 모두 같은 단계)
//   ├ Mid_Sky            — 별 수천 개(Mid_StarField) + ③ GL 별자리 선(Mid_Constellations)
//   │  └ Mid_SkyDome     — 밤하늘 그라데이션과 은하수
//   └ Mid_Ground         — 흙바닥(Mid_Earth), 연못(Mid_Pond), 수면 반사(Mid_PondReflection),
//                          디딤돌 길과 석등(Mid_Path)
// Mid, Mid_LanternMobile의 Transform은 기본값으로 둘 것 (chain은 노드의 local 값만 곱하기 때문)
// 다시 만들 때, 기존 Mid 아래에서 직접 옮겨 둔 위치·회전·크기는 이름이 같은 오브젝트에 되살림
// (컴포넌트의 값은 기본값으로 돌아감)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Mid_SceneBuilder
{
    const string MaterialFolder = "Assets/Mid/Materials";

    // 주마등(Mid_LanternMobile 묶음)만 지우고, 처마 네 귀퉁이에 풍경 4개를 추가함
    // 다른 오브젝트(석등, 화로, 숯, 조명, 하늘, 바닥 등)는 건드리지 않음
    [MenuItem("CG/Mid/Replace Lantern Mobile With Wind Bells")]
    static void ReplaceLanternMobile()
    {
        GameObject mid = GameObject.Find("Mid");
        if (mid == null) { Debug.LogWarning("[Mid] Mid 오브젝트가 없습니다."); return; }
        if (mid.transform.Find("Mid_WindBells") != null) { Debug.LogWarning("[Mid] 이미 Mid_WindBells가 있습니다."); return; }

        Transform mobile = mid.transform.Find("Mid_LanternMobile");
        if (mobile != null) Undo.DestroyObjectImmediate(mobile.gameObject);

        BuildWindBells(mid.transform, LoadBronze(), LoadCord());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Mid] 주마등을 지우고 처마 네 귀퉁이에 풍경을 달았습니다. 위치는 Mid_WindBell_*_Anchor로 맞추세요.");
    }

    [MenuItem("CG/Mid/Rebuild Mid Objects")]
    static void Rebuild()
    {
        // ---------- 기존 값 기억 ----------
        Vector3 hub = EstimatePavilionCenter();
        GameObject oldSky = GameObject.Find("Mid_Sky");
        Quaternion skyRotation = oldSky != null ? oldSky.transform.rotation : Quaternion.identity;

        // 직접 옮긴 위치·회전·크기를 이름으로 기억 (이름이 하나뿐인 오브젝트만)
        var savedTransforms = new System.Collections.Generic.Dictionary<string, (Vector3 p, Quaternion r, Vector3 s)>();
        var duplicateNames = new System.Collections.Generic.HashSet<string>();
        GameObject oldMid = GameObject.Find("Mid");
        if (oldMid != null)
        {
            foreach (Transform t in oldMid.GetComponentsInChildren<Transform>(true))
            {
                if (savedTransforms.ContainsKey(t.name)) duplicateNames.Add(t.name);
                else savedTransforms[t.name] = (t.localPosition, t.localRotation, t.localScale);
            }
        }

        // ---------- 기존 Mid 오브젝트 지우기 (Ctrl+Z로 되돌릴 수 있음) ----------
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Mid" || root.name.StartsWith("Mid_"))
                Undo.DestroyObjectImmediate(root);

        // ---------- 재질 ----------
        Material paper = LoadOrCreateLit("Mat_Mid_LanternPaper",
            baseColor: new Color(1f, 0.93f, 0.8f), metallic: 0f, smoothness: 0.2f,
            emission: new Color(1f, 0.55f, 0.18f) * 2.5f);
        Material starMat = LoadOrCreate("Mat_Mid_StarField", "CG/Mid_StarField");
        Material skyMat = LoadOrCreate("Mat_Mid_NightSky", "CG/Mid_NightSky");
        Material lineMat = LoadOrCreate("Mat_Mid_ConstellationLine", "CG/Mid_GLLine");

        // ---------- 묶음 ----------
        Transform mid = CreateNode("Mid", Vector3.zero, null);

        // ---------- 1. 풍경 ----------
        BuildWindBells(mid, LoadBronze(), LoadCord());

        // ---------- 2. 밤하늘 ----------
        Transform sky = CreateNode("Mid_Sky", new Vector3(hub.x, 0f, hub.z), mid);
        sky.rotation = skyRotation;   // Y 회전으로 별자리와 은하수의 방향을 한꺼번에 돌릴 수 있음
        GameObject skyGo = sky.gameObject;
        skyGo.AddComponent<MeshFilter>();
        MeshRenderer starRenderer = skyGo.AddComponent<MeshRenderer>();
        starRenderer.sharedMaterial = starMat;
        MakeBackgroundRenderer(starRenderer);

        Mid_Constellations constellations = skyGo.AddComponent<Mid_Constellations>();
        SerializedObject cso = new SerializedObject(constellations);
        cso.FindProperty("lineMaterial").objectReferenceValue = lineMat;
        cso.ApplyModifiedPropertiesWithoutUndo();

        Mid_StarField starField = skyGo.AddComponent<Mid_StarField>();
        SerializedObject sso = new SerializedObject(starField);
        sso.FindProperty("skyMaterial").objectReferenceValue = skyMat;
        sso.ApplyModifiedPropertiesWithoutUndo();
        starField.Rebuild();   // 별자리 데이터와 하늘 재질이 연결된 뒤 다시 만듦

        Transform dome = CreateNode("Mid_SkyDome", Vector3.zero, sky);
        dome.gameObject.AddComponent<MeshFilter>();
        MeshRenderer domeRenderer = dome.gameObject.AddComponent<MeshRenderer>();
        domeRenderer.sharedMaterial = skyMat;
        MakeBackgroundRenderer(domeRenderer);
        dome.gameObject.AddComponent<Mid_SkyDome>();

        // ---------- 3. 흙바닥과 연못 ----------
        Material earthMat = LoadOrCreateLit("Mat_Mid_Earth",
            baseColor: new Color(0.07f, 0.06f, 0.05f), metallic: 0f, smoothness: 0.12f, emission: Color.black);
        Material waterMat = LoadOrCreateLit("Mat_Mid_PondWater",
            baseColor: new Color(0.01f, 0.015f, 0.03f), metallic: 0.3f, smoothness: 0.96f, emission: Color.black);
        Material stoneMat = LoadOrCreateLit("Mat_Mid_PondStone",
            baseColor: new Color(0.2f, 0.19f, 0.18f), metallic: 0f, smoothness: 0.25f, emission: Color.black);

        Transform ground = CreateNode("Mid_Ground", Vector3.zero, mid);

        Transform earth = CreateNode("Mid_Earth", new Vector3(hub.x, 0f, hub.z), ground);
        earth.gameObject.AddComponent<MeshFilter>();
        earth.gameObject.AddComponent<MeshRenderer>().sharedMaterial = earthMat;
        earth.gameObject.AddComponent<MeshCollider>();   // SceneWalker가 밟고 걷는 땅
        earth.gameObject.AddComponent<Mid_GroundMesh>();

        Vector3 pondPos = new Vector3(-6f, 0f, -7f);       // 정자 계단 앞 왼쪽
        Transform pond = CreateNode("Mid_Pond", pondPos, ground);
        pond.gameObject.AddComponent<MeshFilter>();
        pond.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { waterMat, stoneMat };
        pond.gameObject.AddComponent<Mid_PondMesh>();

        // 수면에 등롱 불빛, 별, 은하수가 비치도록 연못 위에서 주변을 매 프레임 찍어 둠
        Transform probeNode = CreateNode("Mid_PondReflection", pondPos + Vector3.up * 1f, ground);
        ReflectionProbe probe = probeNode.gameObject.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = 256;
        probe.size = new Vector3(9f, 4f, 9f);
        probe.farClipPlane = 1000f;   // 하늘 돔(450)까지 찍히도록

        // 디딤돌 길과 석등
        Transform path = CreateNode("Mid_Path", Vector3.zero, ground);
        path.gameObject.AddComponent<MeshFilter>();
        path.gameObject.AddComponent<MeshRenderer>().sharedMaterial = stoneMat;
        Mid_SteppingStones steps = path.gameObject.AddComponent<Mid_SteppingStones>();
        // 디딤돌은 정자(계단 포함)가 끝나는 곳 바로 앞에서 시작
        SerializedObject pso = new SerializedObject(steps);
        Vector3 pathStart = new Vector3(0f, 0f, PavilionFrontZ() - 0.5f);
        Vector3 pathEnd = pondPos + (pathStart - pondPos).normalized * 4.0f;   // 연못 테두리 돌 바로 바깥
        pso.FindProperty("start").vector3Value = pathStart;
        pso.FindProperty("end").vector3Value = pathEnd;
        pso.ApplyModifiedPropertiesWithoutUndo();

        // 석등 자리: 연못 둘레 네 곳 (정자·계단과 겹치지 않게)
        Vector3[] lampSpots =
        {
            pondPos + new Vector3(Mathf.Cos( 70f * Mathf.Deg2Rad), 0f, Mathf.Sin( 70f * Mathf.Deg2Rad)) * 4.4f,
            pondPos + new Vector3(Mathf.Cos(160f * Mathf.Deg2Rad), 0f, Mathf.Sin(160f * Mathf.Deg2Rad)) * 4.4f,
            pondPos + new Vector3(Mathf.Cos(250f * Mathf.Deg2Rad), 0f, Mathf.Sin(250f * Mathf.Deg2Rad)) * 4.4f,
            pondPos + new Vector3(Mathf.Cos(340f * Mathf.Deg2Rad), 0f, Mathf.Sin(340f * Mathf.Deg2Rad)) * 4.4f,
        };
        for (int i = 0; i < lampSpots.Length; i++)
        {
            Transform lamp = CreateNode($"Mid_StoneLamp_{i}", lampSpots[i], path);
            lamp.gameObject.AddComponent<MeshFilter>();
            lamp.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { stoneMat, paper };
            lamp.gameObject.AddComponent<Mid_StoneLampMesh>();

            Transform lampLight = CreateNode("Light", new Vector3(0f, Mid_StoneLampMesh.LightHeight, 0f), lamp);
            Light l = lampLight.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.66f, 0.36f);
            l.intensity = 1.2f;
            l.range = 4.5f;
            l.shadows = LightShadows.None;   // 불창 안에 있으므로 그림자를 켜면 석등 자신에 가려짐
        }

        // 예전 회색 Plane은 끔 (지우지 않음)
        GameObject oldPlane = GameObject.Find("Plane");
        if (oldPlane != null)
        {
            Undo.RecordObject(oldPlane, "Mid Build");
            oldPlane.SetActive(false);
        }

        // ---------- 4. 안개와 카메라 배경을 밤하늘의 지평선 색으로 ----------
        Color horizon = skyMat.GetColor("_HorizonColor");
        RenderSettings.fogColor = horizon;
        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(cam, "Mid Build");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = horizon;
        }

        // ---------- 5. 직접 옮겨 둔 위치·회전·크기 되살리기 ----------
        foreach (Transform t in mid.GetComponentsInChildren<Transform>(true))
        {
            if (duplicateNames.Contains(t.name) || !savedTransforms.TryGetValue(t.name, out var saved)) continue;
            t.localPosition = saved.p;
            t.localRotation = saved.r;
            t.localScale = saved.s;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = mid.gameObject;
        Debug.Log($"[Mid] Mid 오브젝트를 다시 만들었습니다. 모빌 중심: {hub}. 하늘 방향은 Mid_Sky의 Rotation Y로 돌리세요.");
    }

    // 정자(계단 포함)의 가장 앞(−z) 끝
    static float PavilionFrontZ()
    {
        GameObject pavilion = GameObject.Find("Pavilion");
        if (pavilion == null) return -5f;
        float minZ = float.MaxValue;
        foreach (Renderer r in pavilion.GetComponentsInChildren<Renderer>())
            minZ = Mathf.Min(minZ, r.bounds.min.z);
        return minZ == float.MaxValue ? -5f : minZ;
    }

    // Pavilion이 있으면 그 가운데 (하늘과 바닥의 중심으로 씀)
    static Vector3 EstimatePavilionCenter()
    {
        Vector3 hub = new Vector3(0f, 4.2f, 0f);
        GameObject pavilion = GameObject.Find("Pavilion");
        if (pavilion != null)
        {
            Renderer[] renderers = pavilion.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
                hub = new Vector3(b.center.x, b.min.y + b.size.y * 0.55f, b.center.z);
            }
        }
        GameObject lamp = GameObject.Find("Lamp");
        if (lamp != null) hub.y = lamp.transform.position.y;
        return hub;
    }

    static Transform CreateNode(string name, Vector3 localPosition, Transform group)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Mid Build");
        if (group != null) go.transform.SetParent(group, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    // 풍경 4개를 Mid 아래 Mid_WindBells 묶음에 만듦 (노드끼리는 Hierarchy로 잇지 않고 모두 같은 단계)
    // 풍경 하나의 체인
    //   종:   Anchor → BellSwing
    //   바람판: Anchor → BellSwing → ClapperPivot → ClapperSwing
    static void BuildWindBells(Transform mid, Material bronze, Material cord)
    {
        Transform group = CreateNode("Mid_WindBells", Vector3.zero, mid);
        Vector3[] corners = EaveCorners();

        for (int i = 0; i < corners.Length; i++)
        {
            Transform anchor = CreateNode($"Mid_WindBell_{i}_Anchor", corners[i], group);

            Transform bellSwing = CreateNode($"Mid_WindBell_{i}_BellSwing", Vector3.zero, group);
            Mid_NodeMotion bellMotion = bellSwing.gameObject.AddComponent<Mid_NodeMotion>();
            SetMotion(bellMotion, spinSpeed: 0f, swingAngle: 6f, swingPeriod: 2.6f + 0.3f * i, swingPhase: 70f * i);
            SetSwingAxis(bellMotion, Quaternion.Euler(0f, 45f * i, 0f) * Vector3.forward);

            Transform clapperPivot = CreateNode($"Mid_WindBell_{i}_ClapperPivot",
                new Vector3(0f, -Mid_WindBellMesh.ClapperPivotDepth, 0f), group);

            Transform clapperSwing = CreateNode($"Mid_WindBell_{i}_ClapperSwing", Vector3.zero, group);
            Mid_NodeMotion clapperMotion = clapperSwing.gameObject.AddComponent<Mid_NodeMotion>();
            SetMotion(clapperMotion, spinSpeed: 0f, swingAngle: 16f, swingPeriod: 1.7f + 0.2f * i, swingPhase: 40f + 50f * i);
            SetSwingAxis(clapperMotion, Quaternion.Euler(0f, 45f * i + 60f, 0f) * Vector3.forward);

            CreateChainPart($"Mid_WindBell_{i}_Bell", group, new[] { bronze, cord },
                new[] { anchor, bellSwing }).AddComponent<Mid_WindBellMesh>();
            CreateChainPart($"Mid_WindBell_{i}_Plate", group, new[] { bronze, cord },
                new[] { anchor, bellSwing, clapperPivot, clapperSwing }).AddComponent<Mid_WindPlateMesh>();
        }
    }

    // 지붕(재질 이름에 Roof가 들어간 메시)의 네 모서리 바로 아래. 정확한 위치는 Anchor로 직접 맞춤
    static Vector3[] EaveCorners()
    {
        Bounds b = new Bounds(new Vector3(0f, 4f, 1f), new Vector3(5f, 1f, 5f));
        GameObject pavilion = GameObject.Find("Pavilion");
        if (pavilion != null)
        {
            bool found = false;
            foreach (Renderer r in pavilion.GetComponentsInChildren<Renderer>())
            {
                bool isRoof = false;
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.name.Contains("Roof")) isRoof = true;
                if (!isRoof) continue;
                if (!found) { b = r.bounds; found = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        const float inset = 0.15f;
        float y = b.min.y - 0.02f;
        return new[]
        {
            new Vector3(b.min.x + inset, y, b.min.z + inset),
            new Vector3(b.max.x - inset, y, b.min.z + inset),
            new Vector3(b.max.x - inset, y, b.max.z - inset),
            new Vector3(b.min.x + inset, y, b.max.z - inset),
        };
    }

    static Material LoadBronze()
    {
        return LoadOrCreateLit("Mat_Mid_Bronze",
            baseColor: new Color(0.72f, 0.52f, 0.3f), metallic: 0.35f, smoothness: 0.55f, emission: Color.black);
    }

    static Material LoadCord()
    {
        return LoadOrCreateLit("Mat_Mid_LanternFrame",
            baseColor: new Color(0.45f, 0.06f, 0.05f), metallic: 0.2f, smoothness: 0.5f, emission: Color.black);
    }

    static void SetSwingAxis(Mid_NodeMotion motion, Vector3 axis)
    {
        SerializedObject so = new SerializedObject(motion);
        so.FindProperty("swingAxis").vector3Value = axis;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 체인 부품 하나: 메시 + 재질 + Mid_MatrixChain(체인과 따라갈 조명). Transform은 기본값
    // 메시 스크립트(Mid_ChainMesh를 상속)는 돌려준 GameObject에 붙임
    static GameObject CreateChainPart(string name, Transform group, Material[] materials, Transform[] chainNodes, Transform follower = null)
    {
        GameObject go = CreateNode(name, Vector3.zero, group).gameObject;
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        Mid_MatrixChain chain = go.AddComponent<Mid_MatrixChain>();

        SerializedObject so = new SerializedObject(chain);
        SetTransformArray(so.FindProperty("chain"), chainNodes);
        so.FindProperty("follower").objectReferenceValue = follower;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // 하늘은 그림자, 조명 계산과 상관없음
    static void MakeBackgroundRenderer(MeshRenderer r)
    {
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    static void SetMotion(Mid_NodeMotion motion, float spinSpeed, float swingAngle, float swingPeriod, float swingPhase)
    {
        SerializedObject so = new SerializedObject(motion);
        so.FindProperty("spinSpeed").floatValue = spinSpeed;
        so.FindProperty("swingAngle").floatValue = swingAngle;
        so.FindProperty("swingPeriod").floatValue = swingPeriod;
        so.FindProperty("swingPhase").floatValue = swingPhase;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetTransformArray(SerializedProperty prop, Transform[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    static Material LoadOrCreate(string name, string shaderName)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        EnsureFolder();
        m = new Material(Shader.Find(shaderName)) { name = name };
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material LoadOrCreateLit(string name, Color baseColor, float metallic, float smoothness, Color emission)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        EnsureFolder();
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", baseColor);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Mid", "Materials");
    }
}

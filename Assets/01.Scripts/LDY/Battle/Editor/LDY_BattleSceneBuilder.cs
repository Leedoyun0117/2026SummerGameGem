using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 전투 패널(회전 링 + 적) 씬을 필요한 요소까지 한 번에 만들어주는 자동화 툴.
// Enemy 태그 생성 -> 아트 스프라이트 임포트 설정 -> 적 프리팹 3종 생성 ->
// 동심원 형태의 원형 링 4겹 생성·설정(안쪽 링부터 순서대로 반지름이 커짐) ->
// 각 링에 스포너 붙이고 데모 적 배치 -> BattleBoardManager/SelectionManager로 묶기까지 메뉴 클릭 한 번으로 처리한다.
public static class LDY_BattleSceneBuilder
{
    private const string ScriptRoot = "Assets/01.Scripts/LDY/Battle";
    private const string PrefabFolder = ScriptRoot + "/Prefabs";
    private const string ArtFolder = "Assets/02.Assets/LDY/Battle";
    private const string EnemyTag = "Enemy";

    // 안쪽부터 바깥쪽 순서. 4겹 전부 12칸으로 통일해서 방사형 경계선이 안쪽부터 바깥까지 일직선으로 정확히 이어지게 한다.
    private static readonly float[] LayerRadii = { 3f, 6f, 9f, 12f };
    private static readonly int[] LayerSegmentCounts = { 12, 12, 12, 12 };
    private static readonly int[] LayerDemoStep = { 3, 2, 2, 2 };
    // 안쪽 레이어는 꽉 찬 파이 휠(Wheel_12), 바깥 3겹은 도넛(annulus) 모양 파이 휠 - 전부 이어붙여서 하나의 큰 다이얼처럼 보이게 한다.
    private static readonly string[] LayerWheelFiles = { "Wheel_12.png", "Wheel_Ring2.png", "Wheel_Ring3.png", "Wheel_Ring4.png" };


    [MenuItem("LDY/Battle/전투 패널 씬 자동 생성")]
    private static void BuildScene()
    {
        if (GameObject.Find("BattleBoard") != null || GameObject.Find("BattleUICanvas") != null)
        {
            // 예전엔 "계속"을 누르면 기존 BattleBoard를 안 지우고 새 세트를 똑같은 위치에 하나 더 만들었는데,
            // 그러면 두 보드가 완전히 겹쳐서 한쪽만 회전하고 다른 쪽은 그대로 남아 "적이 복사된 것처럼" 보이는
            // 문제가 생긴다. 그래서 기본 동작을 "기존 것 지우고 새로 만들기"로 바꾼다.
            int choice = EditorUtility.DisplayDialogComplex(
                "전투 패널 씬 자동 생성",
                "씬에 이미 'BattleBoard'(또는 UI) 오브젝트가 있습니다.\n" +
                "(예전에 여러 번 생성했다면 보드가 겹쳐 있어서 회전 시 적이 복사된 것처럼 보일 수 있습니다)",
                "기존 것 지우고 새로 만들기",
                "취소",
                "그대로 두고 추가로 생성");

            if (choice == 1) return; // 취소

            if (choice == 0) // 기존 것 지우고 새로 만들기
            {
                GameObject board;
                while ((board = GameObject.Find("BattleBoard")) != null)
                {
                    Undo.DestroyObjectImmediate(board);
                }

                GameObject ui;
                while ((ui = GameObject.Find("BattleUICanvas")) != null)
                {
                    Undo.DestroyObjectImmediate(ui);
                }
            }
            // choice == 2(그대로 두고 추가로 생성)면 아무것도 지우지 않고 기존 동작대로 진행한다.
        }

        EnsureFolder(PrefabFolder);
        EnsureTag(EnemyTag);

        ImportAsSprite(ArtFolder + "/Enemy_Slime.png");
        ImportAsSprite(ArtFolder + "/Enemy_Goblin.png");
        ImportAsSprite(ArtFolder + "/Enemy_Boss.png");
        foreach (string wheelFile in LayerWheelFiles)
        {
            ImportAsSprite(ArtFolder + "/" + wheelFile);
        }
        ImportAsSprite(ArtFolder + "/SelectionRing.png");

        // 슬롯 간격(대략 2~2.8 유닛)에 맞춰 적 크기를 잡는다. 보스는 조금 더 크게.
        GameObject slimePrefab = FindOrCreateEnemyPrefab("Enemy_Slime", ArtFolder + "/Enemy_Slime.png", targetDiameter: 1.2f);
        GameObject goblinPrefab = FindOrCreateEnemyPrefab("Enemy_Goblin", ArtFolder + "/Enemy_Goblin.png", targetDiameter: 1.3f);
        GameObject bossPrefab = FindOrCreateEnemyPrefab("Enemy_Boss", ArtFolder + "/Enemy_Boss.png", targetDiameter: 1.8f);
        GameObject[] enemyPrefabs = { slimePrefab, goblinPrefab, bossPrefab };

        GameObject boardGO = new GameObject("BattleBoard");
        Undo.RegisterCreatedObjectUndo(boardGO, "Build Battle Scene");
        boardGO.AddComponent<LDY_BattleBoardManager>();
        boardGO.AddComponent<LDY_RingSelectionManager>();

        var ringsInnerToOuter = new List<LDY_RingController>();
        var ringInnerBounds = new List<float>();
        var ringOuterBounds = new List<float>();

        for (int layer = 0; layer < LayerRadii.Length; layer++)
        {
            float radius = LayerRadii[layer];
            int segmentCount = LayerSegmentCounts[layer];

            LDY_RingController ring = CreateRing(boardGO.transform, $"Ring_Layer{layer + 1}", radius, segmentCount);

            // 안쪽 경계: 바로 안쪽 레이어와의 중간 지점(맨 안쪽 레이어는 0, 즉 구멍 없는 꽉 찬 원판).
            // 바깥 경계: 바로 바깥 레이어와의 중간 지점(맨 바깥 레이어는 여유를 두고 확장).
            // 탭 판정은 이 outerBound를 반지름으로 하는 CircleCollider2D 하나로만 처리하고,
            // 동심원 겹침은 LDY_RingSelectionManager가 "반지름이 가장 작은 링"을 고르는 방식으로 해결한다.
            float innerBound = layer == 0 ? 0f : (LayerRadii[layer - 1] + radius) * 0.5f;
            float outerBound = layer == LayerRadii.Length - 1 ? radius + 1.5f : (radius + LayerRadii[layer + 1]) * 0.5f;
            ring.SetTapRadius(outerBound);

            // 안쪽 레이어는 꽉 찬 파이 휠, 바깥 3겹은 도넛(annulus) 파이 휠 - 전부 outerBound 기준으로 스케일하면
            // 이전 레이어의 바깥 경계와 이번 레이어의 안쪽 구멍이 자연스럽게 맞물려서 하나의 큰 다이얼처럼 이어진다.
            AddWheelVisual(ring, outerBound, LayerWheelFiles[layer]);
            // 도넛 모양 링(2번째 이후)은 안쪽 경계선과 바깥쪽 경계선을 둘 다 강조해야 "이 두 줄 사이 칸 전체"가
            // 움직인다는 게 확실히 보인다 (맨 안쪽 링은 안쪽 경계가 없어서 바깥쪽 하나만 강조).
            AddSelectionHighlight(ring, innerBound, outerBound);

            GameObject[] layerPrefabs = { enemyPrefabs[layer % 3], enemyPrefabs[(layer + 1) % 3] };
            PopulateDemoEnemies(ring, layerPrefabs, LayerDemoStep[layer]);
            SetEnemyPalette(ring, enemyPrefabs); // 슬라임/고블린/보스 전부를 팔레트로 등록 -> 씬 뷰에서 바로 자유 배치 가능

            ringsInnerToOuter.Add(ring);
            ringInnerBounds.Add(innerBound);
            ringOuterBounds.Add(outerBound);
        }

        // 지름선(스포크) 컨트롤러: 4겹을 전부 관통하는 지름선을 좌/우로 고르고 위/아래로 미는 기능.
        CreateRadialLine(boardGO.transform, ringsInnerToOuter, LayerRadii[LayerRadii.Length - 1] + 1.5f);

        // 무기 공격 범위 조준 컨트롤러: 4개 링을 "행", 세그먼트를 "열"로 보고 세로1x4/사각형2x2/가로4x1 범위를 계산한다.
        LDY_AttackTargetController attackController = CreateAttackTargetController(boardGO.transform, ringsInnerToOuter, ringInnerBounds, ringOuterBounds);

        // 왼쪽 상단 "무기" 버튼 -> 3칸 팔레트 -> 공격 버튼 UI.
        EnsureEventSystem();
        Canvas uiCanvas = CreateWeaponCanvas();
        Canvas hintCanvas = CreateHintOverlayCanvas();
        CreateWeaponUI(uiCanvas.transform, attackController, hintCanvas.transform);

        float outermostVisualRadius = LayerRadii[LayerRadii.Length - 1] + 2f;
        Bounds contentBounds = new Bounds(boardGO.transform.position,
            new Vector3(outermostVisualRadius * 2f, outermostVisualRadius * 2f, 0f));

        SetupCamera(contentBounds);

        Selection.activeGameObject = boardGO;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[LDY_BattleSceneBuilder] 전투 패널 씬 자동 생성 완료.");

        EditorUtility.DisplayDialog("전투 패널 씬 자동 생성 완료",
            "생성됨:\n" +
            "· Enemy 태그\n" +
            "· 적 프리팹 3종 (Battle/Prefabs 폴더)\n" +
            "· BattleBoard (동심원 4겹: Ring_Layer1~4, 전부 12칸씩 -> 방사형 경계선이 안쪽부터 바깥까지 일직선으로 이어짐)\n" +
            "· 지름선(스포크) 컨트롤러 추가 - 오리가미킹처럼 1시~7시 방향 같은 지름선을 좌/우로 고르고 위/아래로 밀면 " +
            "적이 링과 링 사이로 이동합니다 (지름선은 6개, 12칸이 반씩 짝지어짐)\n" +
            "· 각 링에 데모 적 배치 완료 (슬롯 간격에 맞게 적 크기 조절됨)\n" +
            "· 메인 카메라를 보드 전체가 화면 안에 들어오도록 자동 조절\n\n" +
            "선택 방법 (둘 다 가능):\n" +
            "· 숫자키 1/2/3/4 = Ring_Layer1~4를 바로 선택, Shift = 지름선 모드 선택 (항상 확실하게 동작)\n" +
            "· 링 클릭(탭) = 클릭 지점의 콜라이더 중 반지름이 가장 작은(=가장 안쪽) 링을 자동으로 선택\n\n" +
            "선택된 것은 노란 표시로 강조됩니다 (링은 경계선, 지름선은 회전하는 막대).\n\n" +
            "Play를 눌러 숫자키/클릭으로 선택한 뒤:\n" +
            "· 원형 링 선택 중: 방향키(네 방향 다 인식) 또는 화면 스와이프로 회전\n" +
            "· 지름선(5번) 선택 중: 방향키 ←/→로 어느 지름선인지 고르고, ↑/↓로 그 줄을 밀어서 적을 링 사이로 이동\n\n" +
            "각 링(Ring_Layer1~4)을 선택한 채로 Inspector를 보면 위쪽에 적 팔레트(슬라임/고블린/보스 아이콘)가 있습니다.\n" +
            "원하는 적을 클릭해서 고른 뒤, 씬 뷰의 타일 구슬을 클릭하면 자유롭게 배치/교체/제거할 수 있습니다.\n\n" +
            "무기 UI: 화면 왼쪽 위 '무기' 버튼 -> 3칸(세로1x4/사각형2x2/가로4x1) 중 하나 선택 -> 방향키로 " +
            "공격 대상 위치(빨간 하이라이트)를 옮긴 뒤 '공격' 버튼 또는 스페이스바로 확정합니다. 무기 선택 중에는 " +
            "방향키가 링 회전 대신 이 조준 커서를 옮기는 데 쓰입니다. 무기를 선택해 공격 가능한 상태가 되면 화면 " +
            "아래에 'SPACE : 공격' 힌트가 뜹니다. (실제 데미지 로직은 적 종류가 정해지면 추가 예정)",
            "확인");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string newFolderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, newFolderName);
    }

    private static void EnsureTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    private static void ImportAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType == TextureImporterType.Sprite) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }

    private static GameObject FindOrCreateEnemyPrefab(string name, string spritePath, float targetDiameter)
    {
        string prefabPath = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (existing != null)
        {
            // 이미 있던 프리팹이어도(예전 버전에서 만들어져 스케일이 안 맞을 수 있음) 크기는 최신 기준으로 다시 맞춰준다.
            Sprite existingSprite = existing.GetComponent<SpriteRenderer>()?.sprite;
            if (existingSprite != null) ScaleToWorldDiameter(existing.transform, existingSprite, targetDiameter);
            PrefabUtility.SavePrefabAsset(existing);
            return existing;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        root.tag = EnemyTag;
        ScaleToWorldDiameter(root.transform, sprite, targetDiameter);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset;
    }

    private static LDY_RingController CreateRing(Transform parent, string name, float radius, int segmentCount)
    {
        GameObject ringGO = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(ringGO, "Build Battle Scene");
        ringGO.transform.SetParent(parent);
        ringGO.transform.localPosition = Vector3.zero;

        // AddComponent가 RequireComponent(CircleCollider2D)를 자동으로 같이 붙여준다 -> 이후 SetTapRadius로 다시 세팅
        LDY_RingController controller = ringGO.AddComponent<LDY_RingController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("ringId").stringValue = name;
        so.FindProperty("shape").enumValueIndex = (int)LDY_RingShape.Circle;
        so.FindProperty("plane").enumValueIndex = (int)LDY_RingPlane.XY;
        so.FindProperty("circleRadius").floatValue = radius;
        so.FindProperty("circleTileCount").intValue = Mathf.Max(segmentCount, 3);
        so.ApplyModifiedProperties();

        ringGO.AddComponent<LDY_RingEnemySpawner>();

        return controller;
    }

    // 지름선(스포크) 컨트롤러를 만든다. 4겹 링을 안쪽->바깥쪽 순서로 등록해두고,
    // 맨 바깥 링 테두리에 강조용 곡선(LineRenderer) 2개(이쪽 편 + 반대편)를 붙인다.
    // 실제 슬롯 이동 로직은 LDY_RadialLineController가 이 링들의 BattleRing 슬롯을 그때그때 이어붙여서 처리한다.
    private static LDY_RadialLineController CreateRadialLine(Transform parent, List<LDY_RingController> ringsInnerToOuter, float outermostBoundRadius)
    {
        GameObject radialGO = new GameObject("RadialLine");
        Undo.RegisterCreatedObjectUndo(radialGO, "Build Battle Scene");
        radialGO.transform.SetParent(parent);
        radialGO.transform.localPosition = Vector3.zero;

        LDY_RadialLineController controller = radialGO.AddComponent<LDY_RadialLineController>();

        LineRenderer arcA = CreateHighlightArcRenderer(radialGO.transform, "HighlightArc_A");
        LineRenderer arcB = CreateHighlightArcRenderer(radialGO.transform, "HighlightArc_B");

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty ringsProp = so.FindProperty("ringsInnerToOuter");
        ringsProp.arraySize = ringsInnerToOuter.Count;
        for (int i = 0; i < ringsInnerToOuter.Count; i++)
        {
            ringsProp.GetArrayElementAtIndex(i).objectReferenceValue = ringsInnerToOuter[i];
        }

        SerializedProperty arcsProp = so.FindProperty("highlightArcs");
        arcsProp.arraySize = 2;
        arcsProp.GetArrayElementAtIndex(0).objectReferenceValue = arcA;
        arcsProp.GetArrayElementAtIndex(1).objectReferenceValue = arcB;
        so.FindProperty("highlightArcRadius").floatValue = outermostBoundRadius;
        so.ApplyModifiedProperties();

        return controller;
    }

    // PNG 스프라이트 대신 LineRenderer로 곡선을 직접 그린다 - 스케일/투명도 관련 임포트 문제를 원천 차단한다.
    // 실제 점(포지션)들은 LDY_RadialLineController가 선택된 칸에 맞춰 매번 다시 계산해서 넣어준다.
    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타(핑크)로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다.
    private static LineRenderer CreateHighlightArcRenderer(Transform parent, string name)
    {
        GameObject arcGO = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(arcGO, "Build Battle Scene");
        arcGO.transform.SetParent(parent);
        arcGO.transform.localPosition = Vector3.zero;

        LineRenderer line = arcGO.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = 0.3f;
        line.numCapVertices = 4;
        line.sortingOrder = 15; // 적/링 하이라이트보다도 위에

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);

        Color gold = new Color(1f, 0.84f, 0.36f, 0.95f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", gold);
        if (material.HasProperty("_Color")) material.SetColor("_Color", gold);

        line.material = material;
        line.startColor = gold;
        line.endColor = gold;

        arcGO.SetActive(false);
        return line;
    }

    // 무기 공격 범위 조준 컨트롤러. 4개 링을 안쪽->바깥쪽 순서로 등록해두면, 세로1x4/사각형2x2/가로4x1을
    // "행=링, 열=세그먼트" 격자 오프셋으로 계산해서 빨간 하이라이트(칸 테두리)를 보여준다.
    // ringInnerBounds/ringOuterBounds는 각 링을 만들 때 이미 계산해둔 안쪽/바깥쪽 경계 반지름을 그대로 재사용한다
    // (지름선/선택 하이라이트가 쓰는 것과 같은 값이라 칸 테두리가 실제 링 경계와 정확히 맞물린다).
    private static LDY_AttackTargetController CreateAttackTargetController(Transform parent,
        List<LDY_RingController> ringsInnerToOuter, List<float> ringInnerBounds, List<float> ringOuterBounds)
    {
        GameObject go = new GameObject("AttackTargetController");
        Undo.RegisterCreatedObjectUndo(go, "Build Battle Scene");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;

        LDY_AttackTargetController controller = go.AddComponent<LDY_AttackTargetController>();

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty ringsProp = so.FindProperty("ringsInnerToOuter");
        SerializedProperty innerProp = so.FindProperty("ringInnerBounds");
        SerializedProperty outerProp = so.FindProperty("ringOuterBounds");

        ringsProp.arraySize = ringsInnerToOuter.Count;
        innerProp.arraySize = ringInnerBounds.Count;
        outerProp.arraySize = ringOuterBounds.Count;

        for (int i = 0; i < ringsInnerToOuter.Count; i++)
        {
            ringsProp.GetArrayElementAtIndex(i).objectReferenceValue = ringsInnerToOuter[i];
            innerProp.GetArrayElementAtIndex(i).floatValue = ringInnerBounds[i];
            outerProp.GetArrayElementAtIndex(i).floatValue = ringOuterBounds[i];
        }

        so.ApplyModifiedProperties();

        return controller;
    }

    // uGUI는 EventSystem이 있어야 버튼 클릭을 받을 수 있다. 이 프로젝트는 New Input System을 쓰므로
    // InputSystemUIInputModule을 붙인다(LDY_MapSceneBuilder와 동일한 패턴).
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "Build Battle Scene");

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Canvas CreateWeaponCanvas()
    {
        GameObject canvasGO = new GameObject("BattleUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Battle Scene");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    // 월드 스페이스 공격 하이라이트(LDY_AttackTargetController)가 렌더 큐/카메라 거리 순서 때문에
    // Screen Space - Camera인 BattleUICanvas를 가릴 수 있어서(공격 이펙트는 ForceRenderOnTop으로 이미
    // 대응돼 있음), "SPACE : 공격" 힌트만큼은 렌더 큐 계산과 무관하게 항상 맨 위에 합성되는
    // Screen Space - Overlay 캔버스에 따로 둔다.
    private static Canvas CreateHintOverlayCanvas()
    {
        GameObject canvasGO = new GameObject("HintOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Battle Scene");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 다른 모든 UI/월드 렌더링보다 확실하게 위

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    // 왼쪽 상단 "무기" 토글 버튼 -> 무기 3칸 팔레트(세로1x4/사각형2x2/가로4x1) -> "공격" 확정 버튼까지 전부 만들고
    // LDY_WeaponUIController에 연결한다. hintParent(Overlay 캔버스)에는 공격 힌트 라벨만 따로 둔다.
    private static void CreateWeaponUI(Transform canvasParent, LDY_AttackTargetController attackController, Transform hintParent)
    {
        Button toggleButton = CreateUIButton(canvasParent, "WeaponToggleButton", "무기",
            new Vector2(20f, -30f), new Vector2(140f, 60f));

        GameObject panelGO = new GameObject("WeaponPanel", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGO, "Build Battle Scene");
        panelGO.transform.SetParent(canvasParent, false);

        RectTransform panelRt = (RectTransform)panelGO.transform;
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(20f, -100f);
        panelRt.sizeDelta = new Vector2(340f, 90f);

        HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        string[] labels = { "세로\n1x4", "사각형\n2x2", "가로\n4x1" };
        LDY_WeaponAttackShape[] shapes =
        {
            LDY_WeaponAttackShape.Vertical1x4, LDY_WeaponAttackShape.Square2x2, LDY_WeaponAttackShape.Horizontal4x1,
        };

        Button[] weaponButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            weaponButtons[i] = CreateUIButton(panelGO.transform, $"WeaponSlot_{i}", labels[i], Vector2.zero, new Vector2(100f, 80f), useLayout: true);
        }

        Button attackButton = CreateUIButton(canvasParent, "AttackConfirmButton", "공격",
            new Vector2(20f, -200f), new Vector2(140f, 60f));

        GameObject attackHintGO = CreateUILabel(hintParent, "AttackHintLabel", "SPACE : 공격",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 60f));

        GameObject uiControllerGO = new GameObject("WeaponUIController");
        Undo.RegisterCreatedObjectUndo(uiControllerGO, "Build Battle Scene");
        uiControllerGO.transform.SetParent(canvasParent, false);

        LDY_WeaponUIController uiController = uiControllerGO.AddComponent<LDY_WeaponUIController>();
        SerializedObject so = new SerializedObject(uiController);
        so.FindProperty("toggleButton").objectReferenceValue = toggleButton;
        so.FindProperty("weaponPanel").objectReferenceValue = panelGO;

        SerializedProperty buttonsProp = so.FindProperty("weaponButtons");
        buttonsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = weaponButtons[i];
        }

        SerializedProperty weaponsProp = so.FindProperty("weapons");
        weaponsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty weaponProp = weaponsProp.GetArrayElementAtIndex(i);
            weaponProp.FindPropertyRelative("weaponName").stringValue = labels[i].Replace("\n", " ");
            weaponProp.FindPropertyRelative("shape").enumValueIndex = (int)shapes[i];
        }

        so.FindProperty("attackButton").objectReferenceValue = attackButton;
        so.FindProperty("attackHintUI").objectReferenceValue = attackHintGO;
        so.ApplyModifiedProperties();
    }

    // 왼쪽 위 기준 앵커의 단순 UI 버튼(배경 이미지 + TMP 텍스트 라벨)을 하나 만든다.
    // useLayout이 true면 부모의 HorizontalLayoutGroup이 위치를 대신 잡아주므로 LayoutElement로 크기만 지정한다.
    private static Button CreateUIButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, bool useLayout = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Build Battle Scene");
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        if (useLayout)
        {
            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.85f, 0.75f, 0.55f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRt = (RectTransform)textGO.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.fontSize = 22f;
        text.raycastTarget = false;

        return button;
    }

    // 클릭할 수 없는 단순 안내 라벨(배경 이미지 + TMP 텍스트)을 하나 만든다. 기본은 비활성 상태로 시작한다.
    private static GameObject CreateUILabel(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Build Battle Scene");
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        image.raycastTarget = false;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRt = (RectTransform)textGO.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 26f;
        text.raycastTarget = false;

        go.SetActive(false);
        return go;
    }

    // step칸 간격으로 데모용 적을 번갈아 배치한다 (실제 배치는 씬 뷰 툴로 자유롭게 다시 편집 가능).
    private static void PopulateDemoEnemies(LDY_RingController controller, GameObject[] enemyPrefabs, int step = 1)
    {
        LDY_RingEnemySpawner spawner = controller.GetComponent<LDY_RingEnemySpawner>();
        if (spawner == null || enemyPrefabs.Length == 0) return;

        spawner.brushPrefab = enemyPrefabs[0];

        List<Vector3> positions = controller.ComputeTilePositions();
        for (int i = 0; i < positions.Count; i += step)
        {
            GameObject prefab = enemyPrefabs[(i / step) % enemyPrefabs.Length];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Build Battle Scene");
            instance.transform.SetParent(controller.transform);
            instance.transform.position = positions[i];

            spawner.SetEntry(i, prefab, instance);
        }

        EditorUtility.SetDirty(spawner);
    }

    // 씬 뷰 자유 배치 툴(LDY_RingEnemySpawnerEditor)의 팔레트에 적 프리팹들을 등록해준다.
    private static void SetEnemyPalette(LDY_RingController controller, GameObject[] palette)
    {
        LDY_RingEnemySpawner spawner = controller.GetComponent<LDY_RingEnemySpawner>();
        if (spawner == null) return;

        spawner.enemyPalette = palette;
        EditorUtility.SetDirty(spawner);
    }

    // 레이어 배경으로 파이 휠(또는 도넛 파이 휠) 스프라이트를 깔아준다 - 몇 칸으로 나뉘어 있는지 눈에 보이게.
    // outerBound 기준으로 스케일하므로, 이전 레이어의 바깥 경계와 이번 레이어의 안쪽 구멍이 맞물려서 이어진다.
    // 적 스프라이트보다 뒤에 그려지도록 sortingOrder를 낮게 잡는다.
    private static void AddWheelVisual(LDY_RingController controller, float outerBound, string wheelFileName)
    {
        Sprite wheelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + wheelFileName);
        if (wheelSprite == null) return;

        GameObject visualGO = new GameObject("WheelVisual");
        Undo.RegisterCreatedObjectUndo(visualGO, "Build Battle Scene");
        visualGO.transform.SetParent(controller.transform);
        visualGO.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = visualGO.AddComponent<SpriteRenderer>();
        renderer.sprite = wheelSprite;
        renderer.sortingOrder = -10;

        ScaleToWorldDiameter(visualGO.transform, wheelSprite, outerBound * 2f);

        // 회전할 때 이 그림도 슬롯 한 칸만큼 같이 돌아서 "판 자체가 도는" 것처럼 보이게 연결해준다.
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("wheelVisualTransform").objectReferenceValue = visualGO.transform;
        so.ApplyModifiedProperties();
    }

    // 이 링이 선택됐을 때 활성화되는 노란 테두리들을 만들어서 컨트롤러의 selectionHighlightRoot 필드에 연결한다.
    // 도넛 모양 링은 안쪽 경계선(innerBound)과 바깥쪽 경계선(outerBound) 둘 다에 노란 원을 겹쳐 그려서,
    // "이 두 줄 사이 칸 전체"가 지금 회전 대상이라는 걸 확실히 보여준다. 맨 안쪽 레이어는 안쪽 경계가 없으므로
    // (innerBound <= 0) 바깥쪽 하나만 그린다.
    // 적/휠 비주얼보다 위에 그려지도록 sortingOrder를 높게 잡고, 평소엔 꺼둔다(LDY_RingController.Awake에서도 다시 꺼줌).
    private static void AddSelectionHighlight(LDY_RingController controller, float innerBound, float outerBound)
    {
        Sprite highlightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/SelectionRing.png");
        if (highlightSprite == null) return;

        GameObject rootGO = new GameObject("SelectionHighlight");
        Undo.RegisterCreatedObjectUndo(rootGO, "Build Battle Scene");
        rootGO.transform.SetParent(controller.transform);
        rootGO.transform.localPosition = Vector3.zero;

        AddHighlightCircle(rootGO.transform, highlightSprite, outerBound, "Outer");
        if (innerBound > 0.01f)
        {
            AddHighlightCircle(rootGO.transform, highlightSprite, innerBound, "Inner");
        }

        rootGO.SetActive(false);

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("selectionHighlightRoot").objectReferenceValue = rootGO;
        so.ApplyModifiedProperties();
    }

    private static void AddHighlightCircle(Transform parent, Sprite sprite, float boundRadius, string name)
    {
        GameObject circleGO = new GameObject($"Highlight_{name}");
        Undo.RegisterCreatedObjectUndo(circleGO, "Build Battle Scene");
        circleGO.transform.SetParent(parent);
        circleGO.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = circleGO.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        ScaleToWorldDiameter(circleGO.transform, sprite, boundRadius * 2f);
    }

    // 스프라이트 원본 픽셀 크기와 무관하게, 원하는 월드 지름(diameter)에 맞춰 localScale을 계산해준다.
    private static void ScaleToWorldDiameter(Transform t, Sprite sprite, float targetDiameter)
    {
        float nativeSize = sprite.bounds.size.x;
        if (nativeSize <= 0f) return;
        float scale = targetDiameter / nativeSize;
        t.localScale = new Vector3(scale, scale, 1f);
    }

    // 메인 카메라를 contentBounds 중심에 맞추고, 그 전체(동심원 보드 + 줄 패널 등)가 화면 안에 다 들어오도록
    // orthographicSize를 계산해준다. 메인 카메라가 없으면 새로 만든다.
    private static void SetupCamera(Bounds contentBounds)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Build Battle Scene");
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        Undo.RecordObject(cam, "Build Battle Scene");
        Undo.RecordObject(cam.transform, "Build Battle Scene");

        cam.orthographic = true;

        float aspect = cam.aspect > 0f ? cam.aspect : 1.7f;
        float halfHeight = contentBounds.extents.y;
        float halfWidth = contentBounds.extents.x;
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);

        Vector3 pos = cam.transform.position;
        float camZ = pos.z != 0f ? pos.z : -10f;
        cam.transform.position = new Vector3(contentBounds.center.x, contentBounds.center.y, camZ);

        EditorUtility.SetDirty(cam);
    }
}

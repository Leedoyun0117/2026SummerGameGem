using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 보스 전투 씬(KTH_BossScene)을 필요한 요소까지 한 번에 만들어주는 자동화 툴.
// LDY_BattleSceneBuilder(링보드 전투)와는 완전히 별개 - 보스는 대상이 하나뿐이라 링/조준 시스템이
// 필요 없고, "보스 스프라이트 + 체력바 + 무기 3버튼 + 공격 버튼"만 있으면 된다.
public static class KTH_BossSceneBuilder
{
    private const string ArtFolder = "Assets/02.Assets/LDY/Battle";
    private const string BossSpriteFile = "Enemy_Boss.png";

    [MenuItem("KTH/Boss/보스 전투 씬 자동 생성")]
    private static void BuildScene()
    {
        if (GameObject.Find("BossRoot") != null)
        {
            bool clear = EditorUtility.DisplayDialog(
                "보스 전투 씬 자동 생성",
                "씬에 이미 'BossRoot' 오브젝트가 있습니다. 지우고 새로 만들까요?",
                "기존 것 지우고 새로 만들기", "취소");

            if (!clear) return;

            Undo.DestroyObjectImmediate(GameObject.Find("BossRoot"));
        }

        GameObject bossRoot = new GameObject("BossRoot");
        Undo.RegisterCreatedObjectUndo(bossRoot, "Build Boss Scene");

        GameObject boss = CreateBoss(bossRoot.transform);
        //BossTurn bossTurn = bossRoot.AddComponent<BossTurn>();

        EnsureEventSystem();
        Canvas canvas = CreateCanvas();

        (Image healthFill, TextMeshProUGUI healthText) = CreateHealthBar(canvas.transform);
        KTH_BossController controller = boss.GetComponent<KTH_BossController>();
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("healthBarFill").objectReferenceValue = healthFill;
        controllerSO.FindProperty("healthText").objectReferenceValue = healthText;
        controllerSO.ApplyModifiedProperties();

        CreateWeaponUI(canvas.transform, boss.transform);

        SetupCamera(boss.transform.position);

        Selection.activeGameObject = bossRoot;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[KTH_BossSceneBuilder] 보스 전투 씬 자동 생성 완료.");
        EditorUtility.DisplayDialog("보스 전투 씬 자동 생성 완료",
            "생성됨:\n" +
            "· 보스 오브젝트(KTH_BossController) + BossTurn(공격 타이머/승리 처리)\n" +
            "· 체력바 UI\n" +
            "· 무기 3버튼(활/폭발/검) + 공격 버튼(KTH_BossWeaponUIController)\n\n" +
            "무기 버튼의 LDY_Weapon 항목(공격 이펙트 프리팹 등)은 Inspector에서 직접 채워 넣어야 합니다.\n" +
            "BossTurn의 '승리 후 돌아갈 맵 씬 이름'이 실제 맵 씬 이름과 일치하는지 확인하세요.",
            "확인");
    }

    private static GameObject CreateBoss(Transform parent)
    {
        GameObject boss = new GameObject("Boss");
        Undo.RegisterCreatedObjectUndo(boss, "Build Boss Scene");
        boss.transform.SetParent(parent);
        boss.transform.localPosition = Vector3.zero;

        Sprite sprite = LoadBossSprite();
        if (sprite != null)
        {
            SpriteRenderer renderer = boss.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            float nativeSize = sprite.bounds.size.x;
            if (nativeSize > 0f)
            {
                float scale = 3f / nativeSize; // 보스는 일반 전투 적보다 훨씬 크게(지름 3유닛)
                boss.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        boss.AddComponent<KTH_BossController>();
        return boss;
    }

    private static Sprite LoadBossSprite()
    {
        string path = ArtFolder + "/" + BossSpriteFile;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "Build Boss Scene");

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasGO = new GameObject("BossUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Boss Scene");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private static (Image, TextMeshProUGUI) CreateHealthBar(Transform canvasParent)
    {
        GameObject barGO = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(barGO, "Build Boss Scene");
        barGO.transform.SetParent(canvasParent, false);

        RectTransform barRt = (RectTransform)barGO.transform;
        barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = new Vector2(0f, -40f);
        barRt.sizeDelta = new Vector2(600f, 40f);

        Image background = barGO.GetComponent<Image>();
        background.color = new Color(0.15f, 0.1f, 0.1f, 0.9f);

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(fillGO, "Build Boss Scene");
        fillGO.transform.SetParent(barGO.transform, false);

        RectTransform fillRt = (RectTransform)fillGO.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);

        Image fill = fillGO.GetComponent<Image>();
        fill.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;

        GameObject textGO = new GameObject("HealthText", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textGO, "Build Boss Scene");
        textGO.transform.SetParent(barGO.transform, false);

        RectTransform textRt = (RectTransform)textGO.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 24f;
        text.raycastTarget = false;

        return (fill, text);
    }

    private static void CreateWeaponUI(Transform canvasParent, Transform bossTransform)
    {
        GameObject panelGO = new GameObject("WeaponPanel", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGO, "Build Boss Scene");
        panelGO.transform.SetParent(canvasParent, false);

        RectTransform panelRt = (RectTransform)panelGO.transform;
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 30f);
        panelRt.sizeDelta = new Vector2(360f, 90f);

        HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        string[] labels = { "활", "폭발", "검" };
        LDY_WeaponAttackShape[] shapes =
        {
            LDY_WeaponAttackShape.Vertical1x4, LDY_WeaponAttackShape.Square2x2, LDY_WeaponAttackShape.Horizontal4x1,
        };

        Button[] weaponButtons = new Button[3];
        GameObject[] highlights = new GameObject[3];

        for (int i = 0; i < 3; i++)
        {
            weaponButtons[i] = CreateUIButton(panelGO.transform, $"WeaponSlot_{i}", labels[i], new Vector2(100f, 80f));

            GameObject highlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(highlight, "Build Boss Scene");
            highlight.transform.SetParent(weaponButtons[i].transform, false);
            RectTransform hRt = (RectTransform)highlight.transform;
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = new Vector2(-4f, -4f);
            hRt.offsetMax = new Vector2(4f, 4f);
            Image hImg = highlight.GetComponent<Image>();
            hImg.color = new Color(1f, 0.85f, 0.3f, 0.6f);
            hImg.raycastTarget = false;
            highlight.transform.SetAsFirstSibling();
            highlight.SetActive(false);
            highlights[i] = highlight;
        }

        Button attackButton = CreateUIButton(canvasParent, "AttackButton", "공격", new Vector2(140f, 60f));
        RectTransform attackRt = (RectTransform)attackButton.transform;
        attackRt.anchorMin = attackRt.anchorMax = new Vector2(0.5f, 0f);
        attackRt.pivot = new Vector2(0.5f, 0f);
        attackRt.anchoredPosition = new Vector2(0f, 130f);

        GameObject descBoxGO = new GameObject("DescriptionBox", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(descBoxGO, "Build Boss Scene");
        descBoxGO.transform.SetParent(canvasParent, false);
        RectTransform descRt = (RectTransform)descBoxGO.transform;
        descRt.anchorMin = descRt.anchorMax = new Vector2(0.5f, 0f);
        descRt.pivot = new Vector2(0.5f, 0f);
        descRt.anchoredPosition = new Vector2(0f, 200f);
        descRt.sizeDelta = new Vector2(500f, 80f);
        Image descBg = descBoxGO.GetComponent<Image>();
        descBg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        descBg.raycastTarget = false;

        GameObject descTextGO = new GameObject("DescriptionText", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(descTextGO, "Build Boss Scene");
        descTextGO.transform.SetParent(descBoxGO.transform, false);
        RectTransform descTextRt = (RectTransform)descTextGO.transform;
        descTextRt.anchorMin = Vector2.zero;
        descTextRt.anchorMax = Vector2.one;
        descTextRt.offsetMin = new Vector2(10f, 6f);
        descTextRt.offsetMax = new Vector2(-10f, -6f);
        TextMeshProUGUI descText = descTextGO.AddComponent<TextMeshProUGUI>();
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = Color.white;
        descText.fontSize = 22f;
        descText.raycastTarget = false;

        GameObject controllerGO = new GameObject("BossWeaponUIController");
        Undo.RegisterCreatedObjectUndo(controllerGO, "Build Boss Scene");
        controllerGO.transform.SetParent(canvasParent, false);

        KTH_BossWeaponUIController uiController = controllerGO.AddComponent<KTH_BossWeaponUIController>();
        SerializedObject so = new SerializedObject(uiController);

        SerializedProperty buttonsProp = so.FindProperty("weaponButtons");
        buttonsProp.arraySize = 3;
        for (int i = 0; i < 3; i++) buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = weaponButtons[i];

        SerializedProperty highlightsProp = so.FindProperty("weaponHighlights");
        highlightsProp.arraySize = 3;
        for (int i = 0; i < 3; i++) highlightsProp.GetArrayElementAtIndex(i).objectReferenceValue = highlights[i];

        SerializedProperty weaponsProp = so.FindProperty("weapons");
        weaponsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty weaponProp = weaponsProp.GetArrayElementAtIndex(i);
            weaponProp.FindPropertyRelative("weaponName").stringValue = labels[i];
            weaponProp.FindPropertyRelative("shape").enumValueIndex = (int)shapes[i];
            weaponProp.FindPropertyRelative("damage").intValue = 10;
        }

        so.FindProperty("attackButton").objectReferenceValue = attackButton;
        so.FindProperty("descriptionText").objectReferenceValue = descText;
        so.FindProperty("effectOrigin").objectReferenceValue = bossTransform;
        so.ApplyModifiedProperties();
    }

    private static Button CreateUIButton(Transform parent, string name, string label, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Build Boss Scene");
        go.transform.SetParent(parent, false);

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.85f, 0.75f, 0.55f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(textGO, "Build Boss Scene");
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
        text.fontSize = 24f;
        text.raycastTarget = false;

        return button;
    }

    private static void SetupCamera(Vector3 focusPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Build Boss Scene");
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        Undo.RecordObject(cam, "Build Boss Scene");
        Undo.RecordObject(cam.transform, "Build Boss Scene");

        cam.orthographic = true;
        cam.orthographicSize = 5f;

        Vector3 pos = cam.transform.position;
        float camZ = pos.z != 0f ? pos.z : -10f;
        cam.transform.position = new Vector3(focusPosition.x, focusPosition.y, camZ);

        EditorUtility.SetDirty(cam);
    }
}

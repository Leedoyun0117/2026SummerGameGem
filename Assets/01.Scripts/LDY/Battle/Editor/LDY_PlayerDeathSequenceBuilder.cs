using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 사망 연출(LDY_PlayerDeathSequence)에 필요한 씬 구성(아이리스용 RawImage, Defeat 텍스트 UI)을 메뉴 한 번으로
// 자동 생성하고 컴포넌트 필드까지 알아서 연결해준다. 몇 번을 눌러도 이름으로 기존 오브젝트를 찾아서 재사용하므로
// 중복 생성되지 않는다 - playerHealth/playerImage는 LDY_PlayerDeathSequence가 런타임에 스스로 찾으므로
// (KTH_PlayerHealth.Instance / 씬의 LDY_PlayerHealthUI 하트) 여기서는 건드리지 않는다.
public static class LDY_PlayerDeathSequenceBuilder
{
    [MenuItem("LDY/Battle/Player Death Sequence 생성")]
    public static void Build()
    {
        GameObject sequenceGO = GameObject.Find("PlayerDeathSequence");
        if (sequenceGO == null)
        {
            sequenceGO = new GameObject("PlayerDeathSequence");
            Undo.RegisterCreatedObjectUndo(sequenceGO, "Create PlayerDeathSequence");
        }

        LDY_PlayerDeathSequence sequence = sequenceGO.GetComponent<LDY_PlayerDeathSequence>();
        if (sequence == null) sequence = Undo.AddComponent<LDY_PlayerDeathSequence>(sequenceGO);

        Canvas canvas = FindOrCreateOverlayCanvas("DeathSequenceCanvas", 5000);

        RawImage iris = FindOrCreateFullScreenRawImage(canvas.transform, "IrisOverlay");
        iris.gameObject.SetActive(false);

        GameObject defeatRoot = FindOrCreateChild(canvas.transform, "DefeatRoot");
        StretchFull(defeatRoot.GetComponent<RectTransform>());

        CanvasGroup group = defeatRoot.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(defeatRoot);
        group.alpha = 0f;

        TextMeshProUGUI text = defeatRoot.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
        {
            GameObject textGO = new GameObject("DefeatText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create DefeatText");
            textGO.transform.SetParent(defeatRoot.transform, false);
            StretchFull(textGO.GetComponent<RectTransform>());

            text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Defeat";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 96;
            text.color = Color.white;
        }

        defeatRoot.SetActive(false);

        SerializedObject so = new SerializedObject(sequence);
        so.FindProperty("irisOverlay").objectReferenceValue = iris;
        so.FindProperty("defeatCanvasRoot").objectReferenceValue = defeatRoot;
        so.FindProperty("defeatGroup").objectReferenceValue = group;
        so.FindProperty("defeatText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = sequenceGO;
        Debug.Log("[LDY_PlayerDeathSequenceBuilder] Player Death Sequence 생성/연결 완료. " +
            "playerImage(연출 대상)만 원하는 오브젝트로 직접 지정해주면 됨 - 비워두면 하트 이미지를 자동으로 씀.");
    }

    private static Canvas FindOrCreateOverlayCanvas(string name, int sortOrder)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortOrder;

        return canvas;
    }

    private static RawImage FindOrCreateFullScreenRawImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : null;
        if (go == null)
        {
            go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
        }

        RawImage image = go.GetComponent<RawImage>();
        if (image == null) image = go.AddComponent<RawImage>();
        image.color = Color.white; // 실제 색은 텍스처(Paint)가 이미 담고 있으므로 틴트는 흰색 그대로 둠

        StretchFull(go.GetComponent<RectTransform>());
        return image;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

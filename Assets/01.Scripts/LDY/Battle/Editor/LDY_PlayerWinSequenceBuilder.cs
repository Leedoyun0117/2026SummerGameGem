using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 승리 연출(LDY_PlayerWinSequence)에 필요한 씬 구성(Win 텍스트 UI)을 메뉴 한 번으로 자동 생성하고
// 컴포넌트 필드까지 알아서 연결해준다. 몇 번을 눌러도 이름으로 기존 오브젝트를 찾아서 재사용하므로
// 중복 생성되지 않는다.
public static class LDY_PlayerWinSequenceBuilder
{
    [MenuItem("LDY/Battle/Player Win Sequence 생성")]
    public static void Build()
    {
        GameObject sequenceGO = GameObject.Find("PlayerWinSequence");
        if (sequenceGO == null)
        {
            sequenceGO = new GameObject("PlayerWinSequence");
            Undo.RegisterCreatedObjectUndo(sequenceGO, "Create PlayerWinSequence");
        }

        LDY_PlayerWinSequence sequence = sequenceGO.GetComponent<LDY_PlayerWinSequence>();
        if (sequence == null) sequence = Undo.AddComponent<LDY_PlayerWinSequence>(sequenceGO);

        Canvas canvas = FindOrCreateOverlayCanvas("WinSequenceCanvas", 5000);

        GameObject winRoot = FindOrCreateChild(canvas.transform, "WinRoot");
        StretchFull(winRoot.GetComponent<RectTransform>());

        CanvasGroup group = winRoot.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(winRoot);
        group.alpha = 0f;

        TextMeshProUGUI text = winRoot.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
        {
            GameObject textGO = new GameObject("WinText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create WinText");
            textGO.transform.SetParent(winRoot.transform, false);
            StretchFull(textGO.GetComponent<RectTransform>());

            text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Win";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 96;
            text.color = Color.white;
        }

        winRoot.SetActive(false);

        SerializedObject so = new SerializedObject(sequence);
        so.FindProperty("winCanvasRoot").objectReferenceValue = winRoot;
        so.FindProperty("winGroup").objectReferenceValue = group;
        so.FindProperty("winText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = sequenceGO;
        Debug.Log("[LDY_PlayerWinSequenceBuilder] Player Win Sequence 생성/연결 완료. " +
            "destinationSceneName(기본 LDY_MapScene)만 실제 맵 씬 이름과 맞는지 확인하면 됨.");
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

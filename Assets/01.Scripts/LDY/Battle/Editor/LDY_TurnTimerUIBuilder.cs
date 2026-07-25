using TMPro;
using UnityEditor;
using UnityEngine;

// 시계 이미지/애니메이터 없이, 남은 시간을 "20.00" 형식으로 보여주는 텍스트만 있는 최소 구성으로
// LDY_TurnTimerUI를 자동 생성/연결한다. 기존 씬에 있던 시계 오브젝트 계층(HealthUI 아래 얽혀있던 것)과는
// 완전히 별개의 새 Canvas/Text를 만들어서 그 쪽 문제와 무관하게 항상 동작하게 함. 여러 번 눌러도
// 이름으로 기존 오브젝트를 찾아서 재사용하므로 중복 생성 안 됨.
public static class LDY_TurnTimerUIBuilder
{
    [MenuItem("LDY/Battle/Turn Timer UI 생성")]
    public static void Build()
    {
        Canvas canvas = FindOrCreateOverlayCanvas("TurnTimerCanvas", 100);

        GameObject textGO = FindOrCreateChild(canvas.transform, "TurnTimerText");
        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(220f, 70f);

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "20:00";
        text.fontSize = 48f;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.color = Color.white;

        GameObject managerGO = GameObject.Find("LDY_TurnTimerUI");
        if (managerGO == null)
        {
            managerGO = new GameObject("LDY_TurnTimerUI");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create LDY_TurnTimerUI");
        }

        LDY_TurnTimerUI timerUI = managerGO.GetComponent<LDY_TurnTimerUI>();
        if (timerUI == null) timerUI = Undo.AddComponent<LDY_TurnTimerUI>(managerGO);

        SerializedObject so = new SerializedObject(timerUI);
        so.FindProperty("timerText").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = managerGO;
        Debug.Log("[LDY_TurnTimerUIBuilder] Turn Timer UI 생성/연결 완료 (시계 이미지 없이 텍스트만).");
    }

    private static Canvas FindOrCreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

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
}

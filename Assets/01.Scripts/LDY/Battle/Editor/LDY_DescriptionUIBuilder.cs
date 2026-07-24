using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 무기 설명이 나올 배경 박스 + 텍스트를 씬에 만들고, 씬에 있는 LDY_WeaponUIController를 찾아
// Description Box(슬라이드 대상)/Description Text 필드에 자동으로 연결해준다.
// 이미 직접 만든 무기 UI(액션 패널 등)에 설명창만 따로 추가하고 싶을 때 사용하는 보조 툴.
public static class LDY_DescriptionUIBuilder
{
    [MenuItem("LDY/Battle/무기 설명창 UI 생성")]
    public static void CreateDescriptionUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("설명창 생성 실패", "씬에 Canvas가 없습니다. 먼저 UI Canvas를 만들어주세요.", "확인");
            return;
        }

        GameObject boxGO = new GameObject("WeaponDescriptionBox", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(boxGO, "Create Weapon Description UI");
        boxGO.transform.SetParent(canvas.transform, false);

        RectTransform boxRt = (RectTransform)boxGO.transform;
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0f, 1f);
        boxRt.pivot = new Vector2(0f, 1f);
        boxRt.anchoredPosition = new Vector2(20f, -300f);
        boxRt.sizeDelta = new Vector2(340f, 110f);

        Image bg = boxGO.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.05f, 0.85f);

        GameObject textGO = new GameObject("DescriptionText", typeof(RectTransform));
        textGO.transform.SetParent(boxGO.transform, false);
        RectTransform textRt = (RectTransform)textGO.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 10f);
        textRt.offsetMax = new Vector2(-14f, -10f);

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.color = Color.white;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        LDY_WeaponUIController controller = Object.FindFirstObjectByType<LDY_WeaponUIController>();
        string message;
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("descriptionBox").objectReferenceValue = boxRt;
            so.FindProperty("descriptionText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
            message = "설명창(배경 박스 + 텍스트)을 만들고 LDY_WeaponUIController의 " +
                "Description Box / Description Text 필드에 자동으로 연결했습니다.\n\n" +
                "지금 위치는 임시 위치입니다 - 하이어라키의 'WeaponDescriptionBox'를 원하는 최종(펼쳐졌을 때) " +
                "위치로 옮기기만 하면, 그 위치가 곧 '펼쳐진 상태' 기준점이 되어 무기를 고를 때 그 자리로 " +
                "슬라이드되어 나타납니다.";
        }
        else
        {
            message = "설명창을 만들었지만 씬에서 LDY_WeaponUIController를 찾지 못해 자동 연결은 하지 못했습니다.\n" +
                "인스펙터에서 Description Box에 'WeaponDescriptionBox'를, Description Text에 " +
                "'WeaponDescriptionBox > DescriptionText'를 직접 연결해주세요.";
        }

        Selection.activeGameObject = boxGO;
        EditorUtility.DisplayDialog("무기 설명창 UI 생성 완료", message, "확인");
    }
}

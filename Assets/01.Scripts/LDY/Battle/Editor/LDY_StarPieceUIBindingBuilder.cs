using UnityEditor;
using UnityEngine;

// 지금 열려있는 씬에 있는 LDY_StarPieceManager가 들고 있는 UI 참조(Drop UI Parent/Count Text/Fly Target/
// World Camera)를 그대로 읽어다가, LDY_StarPieceUIBinding 오브젝트를 만들고(이미 있으면 재사용) 똑같이
// 연결해준다. 맵/전투 각 씬을 열어둔 상태에서 한 번씩 실행하면 됨 - 씬마다 손으로 4개씩 드래그할 필요가 없다.
public static class LDY_StarPieceUIBindingBuilder
{
    [MenuItem("LDY/Battle/Star Piece UI Binding 생성")]
    public static void Build()
    {
        LDY_StarPieceManager manager = Object.FindFirstObjectByType<LDY_StarPieceManager>();
        if (manager == null)
        {
            Debug.LogWarning("[LDY_StarPieceUIBindingBuilder] 지금 열려있는 씬에 LDY_StarPieceManager가 없어서 UI 참조를 읽어올 수 없습니다.");
            return;
        }

        SerializedObject managerSO = new SerializedObject(manager);
        Object dropUIParent = managerSO.FindProperty("dropUIParent").objectReferenceValue;
        Object countText = managerSO.FindProperty("countText").objectReferenceValue;
        Object flyTarget = managerSO.FindProperty("flyTarget").objectReferenceValue;
        Object worldCamera = managerSO.FindProperty("worldCamera").objectReferenceValue;

        GameObject bindingGO = GameObject.Find("StarPieceUIBinding");
        if (bindingGO == null)
        {
            bindingGO = new GameObject("StarPieceUIBinding");
            Undo.RegisterCreatedObjectUndo(bindingGO, "Create StarPieceUIBinding");
        }

        LDY_StarPieceUIBinding binding = bindingGO.GetComponent<LDY_StarPieceUIBinding>();
        if (binding == null) binding = Undo.AddComponent<LDY_StarPieceUIBinding>(bindingGO);

        SerializedObject bindingSO = new SerializedObject(binding);
        bindingSO.FindProperty("dropUIParent").objectReferenceValue = dropUIParent;
        bindingSO.FindProperty("countText").objectReferenceValue = countText;
        bindingSO.FindProperty("flyTarget").objectReferenceValue = flyTarget;
        bindingSO.FindProperty("worldCamera").objectReferenceValue = worldCamera;
        bindingSO.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = bindingGO;

        if (dropUIParent == null || countText == null || worldCamera == null)
        {
            Debug.LogWarning("[LDY_StarPieceUIBindingBuilder] 일부 값이 비어있습니다 (dropUIParent/countText/worldCamera 중 null 있음) - " +
                "이 씬의 LDY_StarPieceManager 인스펙터 값 자체가 비어있는지 확인해주세요.");
        }
        else
        {
            Debug.Log($"[LDY_StarPieceUIBindingBuilder] 완료 - dropUIParent={dropUIParent.name}, countText={countText.name}, " +
                $"flyTarget={(flyTarget != null ? flyTarget.name : "(없음)")}, worldCamera={worldCamera.name}");
        }
    }
}

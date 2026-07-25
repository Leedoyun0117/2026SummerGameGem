using UnityEditor;
using UnityEngine;

// 지금 열려있는 씬에서 체력 UI(LDY_PlayerHealthUI)와 별 조각 UI(LDY_StarPieceUIBinding의 countText)가
// 속한 Canvas를 찾아서, 그 Canvas만 오버라이드 소팅으로 sortingOrder를 아주 높게 강제한다 - 상점/맵 등
// 다른 UI보다 항상 위에 그려지게 하기 위함(사망/승리 연출 캔버스(5000)보다는 낮게 잡아서 그쪽이 이김).
public static class LDY_TopmostUICanvasBuilder
{
    private const int TargetSortingOrder = 2000;

    [MenuItem("LDY/Battle/체력·별조각 UI 최상단으로")]
    public static void Apply()
    {
        int changed = 0;

        foreach (LDY_PlayerHealthUI healthUI in Object.FindObjectsByType<LDY_PlayerHealthUI>(FindObjectsSortMode.None))
        {
            if (ForceTopmost(healthUI.transform)) changed++;
        }

        foreach (LDY_StarPieceUIBinding binding in Object.FindObjectsByType<LDY_StarPieceUIBinding>(FindObjectsSortMode.None))
        {
            if (ForceTopmost(binding.transform)) changed++;
        }

        Debug.Log($"[LDY_TopmostUICanvasBuilder] Canvas {changed}개를 sortingOrder {TargetSortingOrder}(으)로 최상단 고정했습니다.");
    }

    private static bool ForceTopmost(Transform t)
    {
        Canvas canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Undo.RecordObject(canvas, "Force Topmost Canvas");
        canvas.overrideSorting = true;
        canvas.sortingOrder = TargetSortingOrder;
        EditorUtility.SetDirty(canvas);
        return true;
    }
}

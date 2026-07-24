using UnityEditor;
using UnityEngine;

// 씬에 있는 모든 Canvas의 렌더 모드와 그 하위에 뭐가 들어있는지 콘솔에 찍어주는 진단 툴.
// "이펙트가 UI에 가려서 안 보인다" 같은 문제의 원인(어느 Canvas가 Screen Space Overlay인지)을
// 하이러키를 직접 뒤지지 않고 바로 확인하기 위해 만들었다.
public static class LDY_CanvasDiagnostics
{
    [MenuItem("LDY/Battle/Canvas 진단")]
    public static void DiagnoseCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            Debug.Log("[LDY_CanvasDiagnostics] 씬에 Canvas가 하나도 없습니다.");
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            string childNames = "";
            foreach (Transform child in canvas.transform)
            {
                childNames += (childNames.Length > 0 ? ", " : "") + child.name;
            }

            string cameraInfo = canvas.worldCamera != null
                ? $"{canvas.worldCamera.name} (Depth {canvas.worldCamera.depth}, NearClip {canvas.worldCamera.nearClipPlane})"
                : "null(카메라 미지정)";

            Debug.Log($"[LDY_CanvasDiagnostics] Canvas '{canvas.name}' - Render Mode: {canvas.renderMode}, " +
                $"Sort Order: {canvas.sortingOrder}, Plane Distance: {canvas.planeDistance}, " +
                $"World Camera: {cameraInfo}, 자식: [{childNames}]");
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[LDY_CanvasDiagnostics] Main Camera '{mainCam.name}' - position: {mainCam.transform.position}, " +
                $"orthographic: {mainCam.orthographic}, nearClip: {mainCam.nearClipPlane}, farClip: {mainCam.farClipPlane}, depth: {mainCam.depth}");
        }

        GameObject board = GameObject.Find("BattleBoard");
        if (board != null)
        {
            Debug.Log($"[LDY_CanvasDiagnostics] BattleBoard position: {board.transform.position}");
        }
    }
}

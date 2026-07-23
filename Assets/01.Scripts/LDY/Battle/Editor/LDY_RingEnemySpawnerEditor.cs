using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// LDY_RingEnemySpawner 전용 씬 뷰 자유 배치 툴.
// Inspector 위쪽에 뜨는 팔레트(적 프리팹 아이콘들) 중 하나를 클릭해서 "지금 배치할 적"으로 고른 뒤,
// 씬 뷰에 표시되는 타일 구슬을 클릭하면 그 자리에 바로 배치된다.
// - 빈 타일 클릭 -> 배치
// - 같은 종류가 있는 타일 클릭 -> 제거
// - 다른 종류가 있는 타일 클릭 -> 지금 고른 종류로 교체 (제거 후 다시 배치할 필요 없음)
[CustomEditor(typeof(LDY_RingEnemySpawner))]
public class LDY_RingEnemySpawnerEditor : Editor
{
    private const float ButtonSizeRatio = 0.35f;

    private LDY_RingEnemySpawner Spawner => (LDY_RingEnemySpawner)target;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("적 팔레트 (클릭해서 지금 배치할 적을 고르세요)", EditorStyles.boldLabel);
        DrawPalette();

        EditorGUILayout.Space();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "씬 뷰에 표시되는 타일 구슬을 클릭하면 위에서 고른 적이 그 타일에 자동으로 놓입니다.\n" +
            "같은 종류가 이미 있는 타일을 다시 클릭하면 제거되고, 다른 종류가 있으면 지금 고른 종류로 바로 교체됩니다.",
            MessageType.Info);

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("모든 배치 지우기"))
        {
            ClearAll();
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawPalette()
    {
        GameObject[] palette = Spawner.enemyPalette;
        if (palette == null || palette.Length == 0)
        {
            EditorGUILayout.HelpBox("아래 'Enemy Palette' 목록에 적 프리팹을 등록하면 여기에 버튼으로 나타납니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        foreach (GameObject prefab in palette)
        {
            if (prefab == null) continue;

            bool isSelected = Spawner.brushPrefab == prefab;
            GUI.backgroundColor = isSelected ? new Color(1f, 0.84f, 0.36f) : Color.white;

            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            GUIContent content = preview != null ? new GUIContent(preview, prefab.name) : new GUIContent(prefab.name);

            if (GUILayout.Button(content, GUILayout.Width(56), GUILayout.Height(56)))
            {
                Undo.RecordObject(Spawner, "Change Brush");
                Spawner.brushPrefab = prefab;
                EditorUtility.SetDirty(Spawner);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        LDY_RingController controller = Spawner.GetComponent<LDY_RingController>();
        if (controller == null) return;

        List<Vector3> positions = controller.ComputeTilePositions();
        for (int i = 0; i < positions.Count; i++)
        {
            DrawTileButton(i, positions[i]);
        }
    }

    private void DrawTileButton(int tileIndex, Vector3 worldPos)
    {
        LDY_RingEnemySpawner.Entry entry = Spawner.GetEntry(tileIndex);
        bool occupied = entry != null && entry.enemyPrefab != null;

        Handles.color = occupied ? new Color(0.85f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 0.9f, 0.7f);
        float size = HandleUtility.GetHandleSize(worldPos) * ButtonSizeRatio;

        if (Handles.Button(worldPos, Quaternion.identity, size, size, Handles.SphereHandleCap))
        {
            ToggleTile(tileIndex, worldPos);
        }

        Handles.Label(worldPos + Vector3.up * (size + 0.15f), tileIndex.ToString());
    }

    private void ToggleTile(int tileIndex, Vector3 worldPos)
    {
        if (Spawner.brushPrefab == null)
        {
            EditorUtility.DisplayDialog("적 배치", "먼저 위 팔레트에서 배치할 적을 선택하세요.", "확인");
            return;
        }

        LDY_RingEnemySpawner.Entry entry = Spawner.GetEntry(tileIndex);

        if (entry != null && entry.enemyPrefab == Spawner.brushPrefab)
        {
            // 지금 고른 것과 같은 종류가 이미 있으면 클릭으로 제거
            if (entry.placedInstance != null)
            {
                Undo.DestroyObjectImmediate(entry.placedInstance);
            }
            Undo.RecordObject(Spawner, "Remove Enemy Placement");
            Spawner.RemoveEntry(tileIndex);
        }
        else
        {
            // 비어있거나 다른 종류가 있으면(교체) 새로 배치
            if (entry != null && entry.placedInstance != null)
            {
                Undo.DestroyObjectImmediate(entry.placedInstance);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Spawner.brushPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Enemy");
            instance.transform.SetParent(Spawner.transform);
            instance.transform.position = worldPos;

            Undo.RecordObject(Spawner, "Place Enemy");
            Spawner.SetEntry(tileIndex, Spawner.brushPrefab, instance);
        }

        EditorUtility.SetDirty(Spawner);
    }

    private void ClearAll()
    {
        Undo.RecordObject(Spawner, "Clear Enemy Placements");
        foreach (LDY_RingEnemySpawner.Entry entry in Spawner.Entries)
        {
            if (entry.placedInstance != null) Undo.DestroyObjectImmediate(entry.placedInstance);
        }
        Spawner.ClearAllEntries();
        EditorUtility.SetDirty(Spawner);
    }
}

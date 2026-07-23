using System;
using System.Collections.Generic;
using UnityEngine;

// LDY_RingController의 각 타일에 처음에 어떤 적을 배치할지 들고 있는 컴포넌트.
// 씬 뷰 툴(LDY_RingEnemySpawnerEditor)로 타일을 클릭하면 이 목록이 자동으로 채워지고,
// 실제 플레이 시작 시(Start) 그 목록대로 링에 occupant를 등록한다.
[RequireComponent(typeof(LDY_RingController))]
public class LDY_RingEnemySpawner : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public int tileIndex;
        public GameObject enemyPrefab;
        [HideInInspector] public GameObject placedInstance; // 씬 뷰 툴이 미리 만들어둔 프리뷰 인스턴스
    }

    [Header("씬 뷰 툴에서 지금 클릭하면 배치될 적 프리팹 (팔레트에서 고르면 여기로 반영됨)")]
    public GameObject brushPrefab;

    [Header("씬 뷰 툴 팔레트에 보여줄 적 프리팹 목록 (자유 배치용)")]
    public GameObject[] enemyPalette;

    [Header("타일별 배치 목록 (씬 뷰에서 타일을 클릭하면 자동으로 채워짐)")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    private LDY_RingController controller;

    private void Awake()
    {
        controller = GetComponent<LDY_RingController>();
    }

    private void Start()
    {
        SpawnAll();
    }

    // entries에 미리 만들어둔 placedInstance가 있으면 그대로 링에 등록하고,
    // 없으면(코드로 entries만 채워둔 경우) 지금 새로 Instantiate한다.
    public void SpawnAll()
    {
        if (controller == null) controller = GetComponent<LDY_RingController>();
        if (controller.Ring == null) return;

        foreach (Entry entry in entries)
        {
            if (entry.enemyPrefab == null) continue;

            GameObject instance = entry.placedInstance;
            if (instance == null)
            {
                instance = Instantiate(entry.enemyPrefab, transform);
            }

            RingSlot slot = controller.Ring.GetSlot(entry.tileIndex);
            instance.transform.position = slot.worldPosition;
            controller.Ring.PlaceOccupant(entry.tileIndex, instance);
        }
    }

    public Entry GetEntry(int tileIndex)
    {
        return entries.Find(e => e.tileIndex == tileIndex);
    }

    public void SetEntry(int tileIndex, GameObject prefab, GameObject placedInstance)
    {
        Entry existing = GetEntry(tileIndex);
        if (existing == null)
        {
            existing = new Entry { tileIndex = tileIndex };
            entries.Add(existing);
        }
        existing.enemyPrefab = prefab;
        existing.placedInstance = placedInstance;
    }

    public void RemoveEntry(int tileIndex)
    {
        entries.RemoveAll(e => e.tileIndex == tileIndex);
    }

    public void ClearAllEntries()
    {
        entries.Clear();
    }
}

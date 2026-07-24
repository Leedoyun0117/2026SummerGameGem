using System.Collections.Generic;
using UnityEngine;

// 맵을 여러 개 만드는 대신, 전투에 들어간 횟수(LDY_MapManager.BattleEntryCount)에 따라 전투 시작 시
// 적 숫자를 점점 늘려서 스폰해서 난이도를 올린다.
// 1번째 입장: baseEnemyCount(기본 12), 그 다음 입장할 때마다 enemyCountIncrement(기본 3)씩 늘어나다가
// maxEnemyCount(기본 35)에서 멈춘다. 링에 미리 만들어둔 데모 배치(LDY_RingEnemySpawner)는 지우고
// 대신 보드 전체 칸 중에서 무작위로 골라 채우되, 안쪽 링(1번)부터 순서대로 채워나가면서
// layerMaxEnemyCount에 정의된 링별 최대치를 넘지 않게 한다 (안 정해둔 링은 그 링의 전체 칸 수가 한도).
public class LDY_BattleDifficultyManager : MonoBehaviour
{
    [Header("전투 입장 횟수에 따른 적 숫자 스케일링")]
    [SerializeField] private int baseEnemyCount = 12;
    [SerializeField] private int enemyCountIncrement = 3;
    [SerializeField] private int maxEnemyCount = 35;

    [Header("링(안쪽부터)별 최대 적 수 - 예: 1번 링 4개 이하, 2번 링 6개 이하, 3번 링 8개 이하")]
    [SerializeField] private int[] layerMaxEnemyCount = { 4, 6, 8 };

    [Header("무작위로 뽑을 적 프리팹 후보들")]
    [SerializeField] private GameObject[] enemyPrefabPool;

    [Header("링 목록 (비어있으면 자식에서 자동으로 찾음)")]
    [SerializeField] private List<LDY_RingController> rings = new List<LDY_RingController>();

    private void Awake()
    {
        if (rings.Count == 0)
        {
            rings.AddRange(GetComponentsInChildren<LDY_RingController>());
        }

        // LDY_RingEnemySpawner.Start()가 데모 배치를 스폰하기 전에(모든 오브젝트의 Awake는 항상
        // 모든 Start보다 먼저 끝난다는 유니티의 보장을 이용) 미리 만들어둔 배치를 지워서 안 겹치게 한다.
        foreach (LDY_RingController ring in rings)
        {
            LDY_RingEnemySpawner spawner = ring.GetComponent<LDY_RingEnemySpawner>();
            if (spawner != null) spawner.ClearAllEntries();
        }
    }

    private void Start()
    {
        // LDY_RingController.BuildRing()도 각자의 Awake에서 실행되므로, ring.Ring이 확실히 준비된
        // Start 시점에 가서야 실제 스폰을 진행한다.
        int entryCount = LDY_MapManager.Instance != null ? Mathf.Max(LDY_MapManager.Instance.BattleEntryCount, 1) : 1;
        int targetCount = Mathf.Min(baseEnemyCount + enemyCountIncrement * (entryCount - 1), maxEnemyCount);

        SpawnEnemies(targetCount);

        Debug.Log($"[LDY_BattleDifficultyManager] 전투 입장 {entryCount}번째 - 적 {targetCount}마리 배치");
    }

    private void SpawnEnemies(int count)
    {
        if (enemyPrefabPool == null || enemyPrefabPool.Length == 0)
        {
            Debug.LogWarning("[LDY_BattleDifficultyManager] Enemy Prefab Pool이 비어있어서 적을 배치하지 못했습니다.");
            return;
        }

        int remaining = count;

        // 안쪽 링(rings[0] = 1번)부터 순서대로, 그 링의 한도(layerMaxEnemyCount, 없으면 그 링의 전체 칸 수)를
        // 넘지 않는 선에서 채우고 남은 개수를 다음 링으로 넘긴다.
        for (int layer = 0; layer < rings.Count && remaining > 0; layer++)
        {
            LDY_RingController ring = rings[layer];
            if (ring == null || ring.Ring == null) continue;

            int slotCount = ring.Ring.SlotCount;
            int layerCap = (layerMaxEnemyCount != null && layer < layerMaxEnemyCount.Length)
                ? Mathf.Min(layerMaxEnemyCount[layer], slotCount)
                : slotCount;

            int spawnInThisLayer = Mathf.Min(layerCap, remaining);
            if (spawnInThisLayer <= 0) continue;

            SpawnInRing(ring, spawnInThisLayer);
            remaining -= spawnInThisLayer;
        }
    }

    // 링 하나의 칸을 무작위 순서로 섞은 뒤 앞에서부터 spawnCount개만 채운다.
    private void SpawnInRing(LDY_RingController ring, int spawnCount)
    {
        List<int> tileIndices = new List<int>(ring.Ring.SlotCount);
        for (int i = 0; i < ring.Ring.SlotCount; i++) tileIndices.Add(i);

        for (int i = tileIndices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (tileIndices[i], tileIndices[j]) = (tileIndices[j], tileIndices[i]);
        }

        int n = Mathf.Min(spawnCount, tileIndices.Count);
        for (int i = 0; i < n; i++)
        {
            int tileIndex = tileIndices[i];
            GameObject prefab = enemyPrefabPool[Random.Range(0, enemyPrefabPool.Length)];

            GameObject instance = Instantiate(prefab, ring.transform);
            RingSlot slot = ring.Ring.GetSlot(tileIndex);
            instance.transform.position = slot.worldPosition;
            ring.Ring.PlaceOccupant(tileIndex, instance);
        }
    }
}

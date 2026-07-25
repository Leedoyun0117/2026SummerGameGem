using System.Collections.Generic;
using UnityEngine;

// 맵을 여러 개 만드는 대신, 전투에 들어간 횟수(LDY_MapManager.BattleEntryCount)에 따라 전투 시작 시
// 적 숫자를 점점 늘려서 스폰해서 난이도를 올린다.
// 1번째 입장: baseEnemyCount(기본 14), 그 다음 입장할 때마다 enemyCountIncrement(기본 3)씩 늘어나다가
// maxEnemyCount(기본 37)에서 멈춘다. 링에 미리 만들어둔 데모 배치(LDY_RingEnemySpawner)는 지우고
// 대신 보드 전체 칸 중에서 무작위로 골라 채우되, 안쪽 링(1번)부터 순서대로 채워나가면서
// layerMaxEnemyCount에 정의된 링별 최대치를 넘지 않게 한다 (안 정해둔 링은 그 링의 전체 칸 수가 한도).
// 어떤 프리팹을 뽑을지는 그냥 무작위가 아니라 링 위치에 따라 편향된다 - 안쪽(1~2번)은 특수(반사형) 적이,
// 바깥쪽(3~4번)은 노말 적이 더 잘 나오게 해서(PickEnemyPrefab) 퍼즐 완급을 만든다.
public class LDY_BattleDifficultyManager : MonoBehaviour
{
    [Header("전투 입장 횟수에 따른 적 숫자 스케일링")]
    [SerializeField] private int baseEnemyCount = 14;
    [SerializeField] private int enemyCountIncrement = 3;
    [SerializeField] private int maxEnemyCount = 37;

    [Header("링(안쪽부터)별 최대 적 수 - 예: 1번 링 6개 이하, 2번 링 8개 이하, 3번 링 10개 이하")]
    [SerializeField] private int[] layerMaxEnemyCount = { 6, 8, 10 };

    [Header("무작위로 뽑을 적 프리팹 후보들 (각 프리팹의 LDY_Enemy.EnemyType으로 특수/노말을 자동 구분함)")]
    [SerializeField] private GameObject[] enemyPrefabPool;

    [Header("퍼즐 난이도 완급 - 안쪽 링(1~2번)은 특수(반사형) 적이, 바깥쪽 링(3~4번)은 노말 적이 더 잘 나오게 함")]
    [Tooltip("1~2번 링(layer 0~1)에서 특수(WeaponReflector) 적이 뽑힐 확률")]
    [SerializeField, Range(0f, 1f)] private float innerRingSpecialChance = 0.7f;
    [Tooltip("3~4번 링(layer 2 이상)에서 노말(NoAbility) 적이 뽑힐 확률")]
    [SerializeField, Range(0f, 1f)] private float outerRingNormalChance = 0.7f;

    private readonly List<GameObject> specialPrefabs = new List<GameObject>();
    private readonly List<GameObject> normalPrefabs = new List<GameObject>();

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

        ClassifyEnemyPool();
    }

    // enemyPrefabPool을 한 번만 훑어서 LDY_Enemy.EnemyType 기준으로 특수(WeaponReflector)/노말(NoAbility)로
    // 나눠둔다 - 새 종류를 추가해도 이 배열에 프리팹 하나만 넣으면 자동으로 분류되므로 규칙(적 판별 기준)은
    // 그대로 유지된다.
    private void ClassifyEnemyPool()
    {
        specialPrefabs.Clear();
        normalPrefabs.Clear();

        if (enemyPrefabPool == null) return;

        foreach (GameObject prefab in enemyPrefabPool)
        {
            if (prefab == null) continue;

            LDY_Enemy enemy = prefab.GetComponent<LDY_Enemy>();
            bool isSpecial = enemy != null && enemy.EnemyType == LDY_EnemyType.WeaponReflector;
            (isSpecial ? specialPrefabs : normalPrefabs).Add(prefab);
        }
    }

    private void Start()
    {
        // LDY_RingController.BuildRing()도 각자의 Awake에서 실행되므로, ring.Ring이 확실히 준비된
        // Start 시점에 가서야 실제 스폰을 진행한다.
        int entryCount = LDY_MapManager.Instance != null ? Mathf.Max(LDY_MapManager.Instance.BattleEntryCount, 1) : 1;

        // 입장 횟수에 따라 계속 늘어나기만 하면 고난도에서 보드가 너무 빽빽해져서(예: maxEnemyCount까지 채우면
        // 칸의 70% 이상) 퍼즐 느낌이 사라진다 - 그래서 보드 전체 칸 수의 약 1/3을 실질적인 상한으로 삼는다.
        int totalSlots = GetTotalSlotCount();
        int ratioCap = Mathf.Max(Mathf.RoundToInt(totalSlots / 3f), 1);
        int hardCap = Mathf.Min(maxEnemyCount, ratioCap);

        int targetCount = Mathf.Min(baseEnemyCount + enemyCountIncrement * (entryCount - 1), hardCap);

        SpawnEnemies(targetCount);

        Debug.Log($"[LDY_BattleDifficultyManager] 전투 입장 {entryCount}번째 - 적 {targetCount}마리 배치 (전체 {totalSlots}칸의 1/3 = {ratioCap})");
    }

    private int GetTotalSlotCount()
    {
        int total = 0;
        foreach (LDY_RingController ring in rings)
        {
            if (ring != null && ring.Ring != null) total += ring.Ring.SlotCount;
        }
        return total;
    }

    private void SpawnEnemies(int count)
    {
        if (enemyPrefabPool == null || enemyPrefabPool.Length == 0)
        {
            Debug.LogWarning("[LDY_BattleDifficultyManager] Enemy Prefab Pool이 비어있어서 적을 배치하지 못했습니다.");
            return;
        }

        // 모든 링의 모든 칸을 하나의 풀로 모은 뒤 무작위로 섞는다 - "어느 칸에 배치되는지"는 완전히 랜덤.
        List<(LDY_RingController ring, int layer, int tileIndex)> allSlots = new List<(LDY_RingController, int, int)>();
        for (int layer = 0; layer < rings.Count; layer++)
        {
            LDY_RingController ring = rings[layer];
            if (ring == null || ring.Ring == null) continue;

            for (int i = 0; i < ring.Ring.SlotCount; i++)
                allSlots.Add((ring, layer, i));
        }

        for (int i = allSlots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allSlots[i], allSlots[j]) = (allSlots[j], allSlots[i]);
        }

        // 링(레이어)마다 이번에 최대 몇 마리까지 받을 수 있는지 - 순서를 강제하는 게 아니라
        // 섞인 순서대로 채워가다가 이미 한도를 채운 링이 나오면 그 칸만 건너뛰는 "제한" 용도로만 씀.
        int[] remainingCapPerLayer = new int[rings.Count];
        for (int layer = 0; layer < rings.Count; layer++)
        {
            LDY_RingController ring = rings[layer];
            int slotCount = (ring != null && ring.Ring != null) ? ring.Ring.SlotCount : 0;
            remainingCapPerLayer[layer] = (layerMaxEnemyCount != null && layer < layerMaxEnemyCount.Length)
                ? Mathf.Min(layerMaxEnemyCount[layer], slotCount)
                : slotCount;
        }

        int spawned = 0;
        foreach ((LDY_RingController ring, int layer, int tileIndex) in allSlots)
        {
            if (spawned >= count) break;
            if (remainingCapPerLayer[layer] <= 0) continue;

            GameObject prefab = PickEnemyPrefab(layer);
            GameObject instance = Instantiate(prefab, ring.transform);
            RingSlot slot = ring.Ring.GetSlot(tileIndex);
            instance.transform.position = slot.worldPosition;
            ring.Ring.PlaceOccupant(tileIndex, instance);

            remainingCapPerLayer[layer]--;
            spawned++;
        }
    }

    // 안쪽 링(1~2번, layer 0~1)은 특수(반사형) 적이, 바깥쪽 링(3~4번, layer 2 이상)은 노말 적이 더 잘
    // 나오게 해서 "안쪽은 아무 무기나 쓰면 반사당하니 신중하게 골라야 하고, 바깥쪽은 편하게 정리하는"
    // 완급을 준다. 원하는 종류가 풀에 하나도 없으면 반대쪽 풀로, 그마저도 비었으면 전체 풀에서 뽑는다.
    private GameObject PickEnemyPrefab(int layer)
    {
        bool wantSpecial = layer < 2
            ? Random.value < innerRingSpecialChance
            : Random.value >= outerRingNormalChance;

        List<GameObject> pool = wantSpecial ? specialPrefabs : normalPrefabs;
        if (pool.Count == 0) pool = wantSpecial ? normalPrefabs : specialPrefabs;
        if (pool.Count == 0) return enemyPrefabPool[Random.Range(0, enemyPrefabPool.Length)];

        return pool[Random.Range(0, pool.Count)];
    }
}

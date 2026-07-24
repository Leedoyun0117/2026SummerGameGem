using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LDY_RingEnemySpawner))]
public class KTH_RandomEnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class RandomSpawnGroup
    {
        [Header("스폰 개수 설정")]
        [Min(1)] public int spawnCount = 1;

        [Header("필수 생성적 (무조건 1개 이상 배치됨)")]
        public GameObject mandatoryEnemyPrefab;

        [Header("랜덤 적 목록 (나머지 타일 채우기용)")]
        public GameObject[] randomEnemyPool;

        [Header("배치 가능한 전체 타일 인덱스 목록")]
        public List<int> targetTileIndices = new List<int>();

        [Header("옵션")]
        public bool allowMultipleMandatory = false;
    }

    [Header("스폰 설정 Group")]
    [SerializeField] private RandomSpawnGroup spawnGroup;

    private LDY_RingEnemySpawner enemySpawner;
    private LDY_RingController ringController;

    private List<GameObject> spawnedInstances = new List<GameObject>();

    private void Awake()
    {
        enemySpawner = GetComponent<LDY_RingEnemySpawner>();
        ringController = GetComponent<LDY_RingController>();
    }

    private void Start()
    {
        OnMyTurnSpawn();
    }

    public void OnMyTurnSpawn()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return null;

        while (ringController != null && ringController.Ring == null)
        {
            yield return null;
        }

        // 1. 기존 생성된 타일만 골라서 안전하게 청소
        ClearAndDestroyAllOccupants();

        yield return new WaitForEndOfFrame();

        // 2. 무작위 타일 생성
        SpawnRandomTiles();
    }

    private void ClearAndDestroyAllOccupants()
    {
        if (enemySpawner == null)
            enemySpawner = GetComponent<LDY_RingEnemySpawner>();

        // 1. 추적 리스트에 등록된 타일 파괴
        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            if (spawnedInstances[i] != null)
            {
                Destroy(spawnedInstances[i]);
            }
        }
        spawnedInstances.Clear();

        // 2. 링 슬롯 데이터 비우기
        if (ringController != null && ringController.Ring != null)
        {
            for (int i = 0; i < ringController.Ring.SlotCount; i++)
            {
                RingSlot slot = ringController.Ring.GetSlot(i);
                if (slot != null && slot.occupant != null)
                {
                    // WheelVisual 같은 본체 그래픽이 아닌 KTH_Tile 타일인 경우만 삭제
                    if (slot.occupant.GetComponent<KTH_Tile>() != null)
                    {
                        Destroy(slot.occupant);
                    }
                    slot.occupant = null;
                }
            }
        }

        // 3. 🔥 안전한 파괴: KTH_Tile 컴포넌트를 가진 자식만 골라서 파괴 (WheelVisual 등 링 그래픽 보호)
        KTH_Tile[] existingTiles = GetComponentsInChildren<KTH_Tile>();
        foreach (KTH_Tile tile in existingTiles)
        {
            if (tile != null && tile.gameObject != null)
            {
                Destroy(tile.gameObject);
            }
        }

        enemySpawner.ClearAllEntries();
    }

    private void SpawnRandomTiles()
    {
        if (spawnGroup.targetTileIndices == null || spawnGroup.targetTileIndices.Count == 0)
        {
            Debug.LogWarning("[RandomEnemySpawner] targetTileIndices 목록이 비어있습니다.");
            return;
        }

        List<int> availableTiles = new List<int>(spawnGroup.targetTileIndices);
        ShuffleList(availableTiles);

        int countToSpawn = Mathf.Clamp(spawnGroup.spawnCount, 1, availableTiles.Count);
        List<int> selectedTiles = availableTiles.GetRange(0, countToSpawn);

        Debug.Log($"[RandomEnemySpawner] 이번 턴 선택된 타일 인덱스 목록: {string.Join(", ", selectedTiles)}");

        // 1. 필수 적 생성
        if (spawnGroup.mandatoryEnemyPrefab != null && selectedTiles.Count > 0)
        {
            int mandatoryTileIndex = selectedTiles[0];
            CreateAndPlaceTile(mandatoryTileIndex, spawnGroup.mandatoryEnemyPrefab);
            selectedTiles.RemoveAt(0);
        }

        // 2. 나머지 선택된 타일에 무작위 생성
        if (spawnGroup.randomEnemyPool != null && spawnGroup.randomEnemyPool.Length > 0)
        {
            foreach (int tileIndex in selectedTiles)
            {
                GameObject selectedPrefab;

                if (spawnGroup.allowMultipleMandatory && UnityEngine.Random.value < 0.3f && spawnGroup.mandatoryEnemyPrefab != null)
                {
                    selectedPrefab = spawnGroup.mandatoryEnemyPrefab;
                }
                else
                {
                    int randomIndex = UnityEngine.Random.Range(0, spawnGroup.randomEnemyPool.Length);
                    selectedPrefab = spawnGroup.randomEnemyPool[randomIndex];
                }

                if (selectedPrefab != null)
                {
                    CreateAndPlaceTile(tileIndex, selectedPrefab);
                }
            }
        }
    }

    private void CreateAndPlaceTile(int tileIndex, GameObject prefab)
    {
        if (prefab == null || ringController == null || ringController.Ring == null) return;

        GameObject instance = Instantiate(prefab, transform);

        RingSlot slot = ringController.Ring.GetSlot(tileIndex);
        if (slot != null)
        {
            instance.transform.position = slot.worldPosition;
            ringController.Ring.PlaceOccupant(tileIndex, instance);
        }

        spawnedInstances.Add(instance);
        enemySpawner.SetEntry(tileIndex, prefab, instance);
    }

    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
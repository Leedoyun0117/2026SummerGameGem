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
        [Tooltip("targetTileIndices에 등록된 타일들 중 실제로 적을 생성할 타일의 개수")]
        [Min(1)] public int spawnCount = 1;

        [Header("필수 생성적 (무조건 1개 이상 배치됨)")]
        [Tooltip("이 타일들 중 최소 하나에는 이 적이 무조건 들어갑니다.")]
        public GameObject mandatoryEnemyPrefab;

        [Header("랜덤 적 목록 (나머지 타일 채우기용)")]
        [Tooltip("필수 적을 제외한 나머지 타일에 랜덤으로 들어갈 적 프리팹들")]
        public GameObject[] randomEnemyPool;

        [Header("배치 가능한 전체 타일 인덱스 목록")]
        [Tooltip("적 스폰 후보 타일 번호들")]
        public List<int> targetTileIndices = new List<int>();

        [Header("옵션")]
        [Tooltip("체크 시 필수 적을 1개만 배치하지 않고 무작위 개수로 여러 개 배치할 수도 있음")]
        public bool allowMultipleMandatory = false;
    }

    [Header("스폰 설정 Group")]
    [SerializeField] private RandomSpawnGroup spawnGroup;

    private LDY_RingEnemySpawner enemySpawner;
    private LDY_RingController ringController;

    // 🔥 직접 스폰한 화살표/적 게임오브젝트만 기억하는 리스트
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

    /// <summary>
    /// 외부(TurnManager 등)에서 플레이어 턴 시작 시 호출하는 메인 스폰 함수
    /// </summary>
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

        // 🔥 1. 이 스크립트가 생성했던 화살표/적만 깔끔하게 파괴 (원판은 안전!)
        ClearAndDestroyAllOccupants();

        // 🔥 2. 새로운 무작위 배치 생성
        SpawnRandomTiles();
    }

    /// <summary>
    /// 원판을 건드리지 않고, 이전에 생성된 적/화살표 오브젝트만 정확하게 파괴
    /// </summary>
    private void ClearAndDestroyAllOccupants()
    {
        if (enemySpawner == null)
            enemySpawner = GetComponent<LDY_RingEnemySpawner>();

        // 1. 직접 관리하던 스폰 오브젝트만 파괴
        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            if (spawnedInstances[i] != null)
            {
                Destroy(spawnedInstances[i]);
            }
        }
        spawnedInstances.Clear();

        // 2. 링 점유 정보(Occupant)의 오브젝트 파괴 및 null 처리
        if (ringController != null && ringController.Ring != null)
        {
            for (int i = 0; i < ringController.Ring.SlotCount; i++)
            {
                RingSlot slot = ringController.Ring.GetSlot(i);
                if (slot != null && slot.occupant != null)
                {
                    // 링에 배치된 occupant가 우리가 생성한 것이라면 안전하게 제거
                    Destroy(slot.occupant);
                    slot.occupant = null;
                }
            }
        }

        // 3. Spawner의 에디터 정보 비우기
        enemySpawner.ClearAllEntries();
    }

    /// <summary>
    /// 타일을 무작위 추출하여 생성 및 링 슬롯에 배치
    /// </summary>
    private void SpawnRandomTiles()
    {
        if (spawnGroup.targetTileIndices == null || spawnGroup.targetTileIndices.Count == 0)
        {
            Debug.LogWarning("[RandomEnemySpawner] 지정된 targetTileIndices 타일 목록이 없습니다.");
            return;
        }

        List<int> availableTiles = new List<int>(spawnGroup.targetTileIndices);
        ShuffleList(availableTiles);

        int countToSpawn = Mathf.Clamp(spawnGroup.spawnCount, 1, availableTiles.Count);
        List<int> selectedTiles = availableTiles.GetRange(0, countToSpawn);

        // 1. 필수 적(Mandatory Enemy) 생성
        if (spawnGroup.mandatoryEnemyPrefab != null && selectedTiles.Count > 0)
        {
            int mandatoryTileIndex = selectedTiles[0];
            CreateAndPlaceTile(mandatoryTileIndex, spawnGroup.mandatoryEnemyPrefab);
            selectedTiles.RemoveAt(0);
        }

        // 2. 나머지 선택된 타일에 랜덤 적 생성
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

        Debug.Log($"[RandomEnemySpawner] 원판 유지 + 새로운 타일 {spawnedInstances.Count}개 스폰 완료!");
    }

    /// <summary>
    /// 프리팹 생성 후 링 슬롯 안착 및 파괴 대상 리스트에 등록
    /// </summary>
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
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
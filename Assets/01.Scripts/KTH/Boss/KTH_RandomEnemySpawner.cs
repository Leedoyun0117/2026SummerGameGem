using System.Collections;
using System.Collections.Generic;
using System.Linq; // 🔥 Distinct, ToList 사용
using UnityEngine;
using DG.Tweening;

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

    [Header("중앙점 및 DOTween 연출 설정")]
    [Tooltip("패널이 모였다가 퍼질 중심점 (비워두면 이 오브젝트의 위치 사용)")]
    [SerializeField] private Transform centerTransform;
    [Tooltip("중앙에서 원래 자리로 날아가는 이동 시간")]
    [SerializeField] private float moveDuration = 0.5f;
    [Tooltip("각 패널이 순차적으로 펼쳐지는 시차 간격")]
    [SerializeField] private float intervalDelay = 0.05f;
    [Tooltip("펼쳐지는 연출 이징 (OutBack 추천)")]
    [SerializeField] private Ease spawnEase = Ease.OutBack;

    private LDY_RingEnemySpawner enemySpawner;
    private LDY_RingController ringController;

    private List<GameObject> spawnedInstances = new List<GameObject>();

    private void Awake()
    {
        enemySpawner = GetComponent<LDY_RingEnemySpawner>();
        ringController = GetComponent<LDY_RingController>();

        if (centerTransform == null)
            centerTransform = transform;
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

        // 1. 기존 타일 완전히 제거
        ClearAndDestroyAllOccupants();

        yield return new WaitForEndOfFrame();

        // 2. 무작위 타일 생성 연출 실행
        SpawnRandomTilesWithAnimation();
    }

    private void ClearAndDestroyAllOccupants()
    {
        if (enemySpawner == null)
            enemySpawner = GetComponent<LDY_RingEnemySpawner>();

        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            if (spawnedInstances[i] != null)
            {
                spawnedInstances[i].transform.DOKill();
                Destroy(spawnedInstances[i]);
            }
        }
        spawnedInstances.Clear();

        if (ringController != null && ringController.Ring != null)
        {
            for (int i = 0; i < ringController.Ring.SlotCount; i++)
            {
                RingSlot slot = ringController.Ring.GetSlot(i);
                if (slot != null && slot.occupant != null)
                {
                    if (slot.occupant.GetComponent<KTH_Tile>() != null)
                    {
                        slot.occupant.transform.DOKill();
                        Destroy(slot.occupant);
                    }
                    slot.occupant = null;
                }
            }
        }

        KTH_Tile[] existingTiles = GetComponentsInChildren<KTH_Tile>();
        foreach (KTH_Tile tile in existingTiles)
        {
            if (tile != null && tile.gameObject != null)
            {
                tile.transform.DOKill();
                Destroy(tile.gameObject);
            }
        }

        enemySpawner.ClearAllEntries();
    }

    private void SpawnRandomTilesWithAnimation()
    {
        if (spawnGroup.targetTileIndices == null || spawnGroup.targetTileIndices.Count == 0)
        {
            Debug.LogWarning("[RandomEnemySpawner] targetTileIndices 목록이 비어있습니다.");
            return;
        }

        // 🔥 1. 중복 인덱스 제거 및 고유 타일 목록 생성
        List<int> availableTiles = spawnGroup.targetTileIndices.Distinct().ToList();

        // 🔥 2. Unity 엔진 난수(UnityEngine.Random) 기반 셔플
        ShuffleList(availableTiles);

        int countToSpawn = Mathf.Clamp(spawnGroup.spawnCount, 1, availableTiles.Count);
        List<int> selectedTiles = availableTiles.GetRange(0, countToSpawn);

        List<(GameObject obj, Vector3 targetPos)> newlyCreatedTiles = new List<(GameObject, Vector3)>();

        // 3. 필수 적 생성
        if (spawnGroup.mandatoryEnemyPrefab != null && selectedTiles.Count > 0)
        {
            int mandatoryTileIndex = selectedTiles[0];
            var tileData = PrepareTile(mandatoryTileIndex, spawnGroup.mandatoryEnemyPrefab);
            if (tileData.obj != null) newlyCreatedTiles.Add(tileData);
            selectedTiles.RemoveAt(0);
        }

        // 4. 나머지 선택된 타일에 무작위 적 생성
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
                    var tileData = PrepareTile(tileIndex, selectedPrefab);
                    if (tileData.obj != null) newlyCreatedTiles.Add(tileData);
                }
            }
        }

        // 5. DOTween 펼쳐짐 연출
        PlaySpreadAnimation(newlyCreatedTiles);
    }

    private (GameObject obj, Vector3 targetPos) PrepareTile(int tileIndex, GameObject prefab)
    {
        if (prefab == null || ringController == null || ringController.Ring == null)
            return (null, Vector3.zero);

        GameObject instance = Instantiate(prefab, transform);
        RingSlot slot = ringController.Ring.GetSlot(tileIndex);

        if (slot == null) return (null, Vector3.zero);

        Vector3 targetWorldPos = slot.worldPosition;
        Vector3 startCenterPos = (centerTransform != null) ? centerTransform.position : transform.position;

        instance.transform.position = startCenterPos;
        instance.transform.localScale = Vector3.zero;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.DOFade(1f, moveDuration * 0.5f);
        }

        ringController.Ring.PlaceOccupant(tileIndex, instance);
        spawnedInstances.Add(instance);
        enemySpawner.SetEntry(tileIndex, prefab, instance);

        return (instance, targetWorldPos);
    }

    private void PlaySpreadAnimation(List<(GameObject obj, Vector3 targetPos)> tiles)
    {
        Sequence spreadSequence = DOTween.Sequence();

        for (int i = 0; i < tiles.Count; i++)
        {
            var item = tiles[i];
            if (item.obj == null) continue;

            Transform tileTf = item.obj.transform;
            Vector3 targetPos = item.targetPos;

            Tween moveTween = tileTf.DOMove(targetPos, moveDuration).SetEase(spawnEase);
            Tween scaleTween = tileTf.DOScale(Vector3.one, moveDuration).SetEase(spawnEase);

            spreadSequence.Insert(i * intervalDelay, moveTween);
            spreadSequence.Insert(i * intervalDelay, scaleTween);
        }
    }

    /// <summary>
    /// 🔥 UnityEngine.Random 기반 고성능 셔플 함수
    /// </summary>
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

    private void OnDestroy()
    {
        for (int i = 0; i < spawnedInstances.Count; i++)
        {
            if (spawnedInstances[i] != null)
                spawnedInstances[i].transform.DOKill();
        }
    }
}
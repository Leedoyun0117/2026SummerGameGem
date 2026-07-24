using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro; // 🔥 TextMeshPro 사용을 위한 네임스페이스
using UnityEngine;

public class KTH_PlayerMovement : MonoBehaviour
{
    [Header("원형 보드 설정")]
    [SerializeField] private Transform center;
    [SerializeField] private float moveAngleStep = 45f;

    [Header("직선 이동 설정")]
    [SerializeField] private float straightMoveDistance = 1f;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.Linear;

    [Header("시작 위치 체크 설정")]
    [SerializeField] private Transform checkTransform;
    [SerializeField] private float checkRadius = 0.3f;
    [SerializeField] private LayerMask pathTileLayer;
    [SerializeField] private float checkDelay = 0.1f;

    [Header("회전 설정")]
    [SerializeField] private float angleOffset = -90f;

    [Header("최종 목적지 콜라이더 태그")]
    [SerializeField] private string endZoneTag = "EndZone";

    // 🔥 [추가] 이름 기반 UI 탐색 및 메시지 설정
    [Header("안내 UI 설정")]
    [SerializeField] private string warningTextName = "WarningText"; // 씬에서 찾을 UI 오브젝트 이름
    [SerializeField] private string noTileMessage = "이동할 타일이 없습니다!"; // 출력할 메시지
    [SerializeField] private float textDuration = 2f; // 텍스트 유지 시간

    private TMP_Text warningText;
    private Coroutine textCoroutine;

    private Tween currentTween;
    private bool isMoving;
    private bool isOnRing = false;

    private Vector2 straightDir;
    private float radius;
    private float currentAngle;
    private float rotationSign = 1f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector2 initialStraightDir;

    private HashSet<GameObject> visitedTiles = new HashSet<GameObject>();

    private void Start()
    {
        if (center == null && transform.parent != null)
            center = transform.parent;

        straightDir = transform.up;

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialStraightDir = transform.up;

        // 🔥 이름으로 UI 텍스트 탐색 후 초기화
        FindWarningTextByName();
    }

    /// <summary>
    /// 🔥 씬 내에서 지정한 이름의 TMP_Text 컴포넌트를 탐색 (비활성화 상태도 검색 가능)
    /// </summary>
    private void FindWarningTextByName()
    {
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        foreach (TMP_Text txt in allTexts)
        {
            // 실제 씬에 존재하는 오브젝트인지 체크 및 이름 비교
            if (txt.gameObject.scene.isLoaded && txt.gameObject.name == warningTextName)
            {
                warningText = txt;
                break;
            }
        }

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[PlayerMovement] '{warningTextName}' 이름을 가진 TMP_Text 오브젝트를 찾지 못했습니다.");
        }
    }

    public void ResetToInitialPosition()
    {
        currentTween?.Kill();
        StopAllCoroutines();

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        isMoving = false;
        isOnRing = false;
        straightDir = initialStraightDir;

        visitedTiles.Clear();

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }

        Debug.Log("[PlayerMovement] 플레이어가 초기 시작 위치 및 방향으로 복귀했습니다.");
    }

    public void StartMovement()
    {
        if (isMoving) return;

        visitedTiles.Clear();
        StartCoroutine(StartMovementRoutine());
    }

    private IEnumerator StartMovementRoutine()
    {
        isMoving = true;

        if (checkDelay > 0f)
            yield return new WaitForSeconds(checkDelay);
        else
            yield return new WaitForEndOfFrame();

        bool hasTile = HasTileAtCheckTransform();

        if (checkTransform != null && !hasTile)
        {
            Debug.Log("[PlayerMovement] 패널 없음 -> 체크 위치 이동 후 보스 턴");

            // 🔥 타일 없음 안내 텍스트 출력
            ShowWarningMessage(noTileMessage);

            MoveToCheckTransformAndEndTurn();
            yield break;
        }

        if (KTH_CameraMoving.Instance != null)
        {
            KTH_CameraMoving.Instance.ZoomIn();
        }

        isMoving = false;
        MoveStep();
    }

    /// <summary>
    /// 🔥 UI 텍스트 출력 및 타이머 처리
    /// </summary>
    private void ShowWarningMessage(string message)
    {
        if (warningText == null) return;

        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
        }

        textCoroutine = StartCoroutine(WarningTextRoutine(message));
    }

    private IEnumerator WarningTextRoutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSeconds(textDuration);

        warningText.gameObject.SetActive(false);
        textCoroutine = null;
    }

    private bool HasTileAtCheckTransform()
    {
        if (checkTransform == null) return true;

        Collider2D hit = (pathTileLayer == 0)
            ? Physics2D.OverlapCircle(checkTransform.position, checkRadius)
            : Physics2D.OverlapCircle(checkTransform.position, checkRadius, pathTileLayer);

        if (hit != null)
        {
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform))
                return false;

            return true;
        }

        return false;
    }

    private void MoveToCheckTransformAndEndTurn()
    {
        RotateTo((checkTransform.position - transform.position).normalized);

        currentTween?.Kill();
        currentTween = transform.DOMove(checkTransform.position, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                EndMovementAndNextTurn();
            });
    }

    private void MoveStep()
    {
        if (isMoving) return;
        isMoving = true;

        if (isOnRing)
            MoveStepOnRing();
        else
            MoveStepStraight();
    }

    private void MoveStepStraight()
    {
        RotateTo(straightDir);

        Vector3 targetPos = transform.position + (Vector3)(straightDir.normalized * straightMoveDistance);

        currentTween?.Kill();
        currentTween = transform.DOMove(targetPos, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isMoving = false;
                MoveStep();
            });
    }

    private void MoveStepOnRing()
    {
        float targetAngle = currentAngle + (moveAngleStep * rotationSign);

        currentTween?.Kill();
        currentTween = DOTween.To(
                () => currentAngle,
                angle =>
                {
                    currentAngle = angle;
                    ApplyRingPositionAndRotation();
                },
                targetAngle,
                moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isMoving = false;
                MoveStep();
            });
    }

    private void ApplyRingPositionAndRotation()
    {
        if (center == null) return;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        transform.position = center.position + (Vector3)offset;

        float tangentAngle = currentAngle + (90f * rotationSign);
        transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle + angleOffset);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(endZoneTag))
        {
            EndMovementAndNextTurn();
            return;
        }

        KTH_Tile tile = other.GetComponentInParent<KTH_Tile>();
        if (tile == null || tile.IsUsed) return;

        GameObject tileObj = tile.gameObject;

        if (visitedTiles.Contains(tileObj))
        {
            Debug.Log($"[PlayerMovement] 이미 밟았던 패널({tileObj.name}) 재방문! -> 이동 종료");
            EndMovementAndNextTurn();
            return;
        }

        visitedTiles.Add(tileObj);

        switch (tile.CurrentTileType)
        {
            case TileType.Arrow:
                ProcessArrowTile(tile);
                break;

            case TileType.Attack:
                ProcessAttackTile(tile);
                break;

            case TileType.Treasure:
                ProcessTreasureTile(tile);
                break;
        }

        if (tile.IsConsumable)
        {
            tile.UseTile();
        }
    }

    private void ProcessArrowTile(KTH_Tile tile)
    {
        transform.position = tile.transform.position;

        Vector2 arrowDir = tile.GetArrowDirection();

        Vector2 offset = transform.position - center.position;
        radius = offset.magnitude;
        currentAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

        Vector2 radialDir = offset.normalized;
        float dot = Vector2.Dot(radialDir, arrowDir);

        if (Mathf.Abs(dot) > 0.5f)
        {
            isOnRing = false;
            straightDir = arrowDir;
        }
        else
        {
            isOnRing = true;
            float cross = radialDir.x * arrowDir.y - radialDir.y * arrowDir.x;
            rotationSign = cross >= 0f ? 1f : -1f;
            ApplyRingPositionAndRotation();
        }

        currentTween?.Kill();
        isMoving = false;
        MoveStep();
    }

    private void ProcessAttackTile(KTH_Tile tile)
    {
        currentTween?.Kill();
        isMoving = false;

        transform.position = tile.transform.position;

        Debug.Log($"[PlayerMovement] 공격 타일 밟음! 보스에게 데미지 전달");

        EndMovementAndNextTurn();
    }

    private void ProcessTreasureTile(KTH_Tile tile)
    {
        currentTween?.Kill();
        isMoving = true;

        transform.position = tile.transform.position;

        Debug.Log("[PlayerMovement] 보물상자 밟음! 아이템 소환 후 계속 이동");

        if (tile.ItemPrefabs != null && tile.ItemPrefabs.Count > 0)
        {
            LDY_RingController ringController = tile.GetComponentInParent<LDY_RingController>();

            List<RingSlot> emptySlots = GetEmptySlots(ringController);
            int spawnCount = Mathf.Min(tile.ItemSpawnCount, emptySlots.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                RingSlot targetSlot = emptySlots[i];
                GameObject randomItemPrefab = tile.ItemPrefabs[Random.Range(0, tile.ItemPrefabs.Count)];

                Transform parentTransform = (ringController != null) ? ringController.transform : tile.transform.parent;
                GameObject itemInstance = Instantiate(randomItemPrefab, tile.transform.position, Quaternion.identity, parentTransform);

                targetSlot.occupant = itemInstance;

                Collider2D itemCollider = itemInstance.GetComponent<Collider2D>();
                if (itemCollider != null)
                {
                    itemCollider.enabled = false;
                }

                Vector3 targetPos = targetSlot.worldPosition;
                itemInstance.transform.localScale = Vector3.zero;

                Sequence itemSeq = DOTween.Sequence();
                itemSeq.Join(itemInstance.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad));
                itemSeq.Join(itemInstance.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));

                itemSeq.OnComplete(() =>
                {
                    if (itemCollider != null)
                    {
                        itemCollider.enabled = true;
                    }
                });
            }
        }

        DOVirtual.DelayedCall(0.5f, () =>
        {
            isMoving = false;
            MoveStep();
        });
    }

    private List<RingSlot> GetEmptySlots(LDY_RingController ringController)
    {
        List<RingSlot> emptySlots = new List<RingSlot>();

        if (ringController != null && ringController.Ring != null)
        {
            for (int i = 0; i < ringController.Ring.SlotCount; i++)
            {
                RingSlot slot = ringController.Ring.GetSlot(i);

                if (slot != null && slot.occupant == null)
                {
                    Collider2D hit = Physics2D.OverlapCircle(slot.worldPosition, 0.3f);

                    if (hit == null || (hit.gameObject != gameObject && hit.GetComponentInParent<KTH_Tile>() == null))
                    {
                        emptySlots.Add(slot);
                    }
                }
            }
        }

        for (int i = emptySlots.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            RingSlot temp = emptySlots[i];
            emptySlots[i] = emptySlots[rnd];
            emptySlots[rnd] = temp;
        }

        return emptySlots;
    }

    private void EndMovementAndNextTurn()
    {
        currentTween?.Kill();
        isMoving = false;

        if (KTH_CameraMoving.Instance != null)
        {
            KTH_CameraMoving.Instance.ZoomIn();
        }

        if (KTH_TurnManager.Instance != null)
        {
            KTH_TurnManager.Instance.NextTurn();
        }
    }

    private void RotateTo(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }

    private void OnDrawGizmosSelected()
    {
        if (checkTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(checkTransform.position, checkRadius);
        }
    }
}
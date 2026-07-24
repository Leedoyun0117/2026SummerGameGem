using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        // 🔥 새 KTH_Tile 컴포넌트 감지
        KTH_Tile tile = other.GetComponentInParent<KTH_Tile>();
        if (tile == null) return;

        GameObject tileObj = tile.gameObject;

        // 🔥 이미 밟았던 패널 재방문 처리
        if (visitedTiles.Contains(tileObj))
        {
            Debug.Log($"[PlayerMovement] 이미 밟았던 패널({tileObj.name}) 재방문! -> 이동 종료");
            EndMovementAndNextTurn();
            return;
        }

        visitedTiles.Add(tileObj);

        // 🔥 Enum 타입에 따른 분기 처리
        switch (tile.CurrentTileType)
        {
            case TileType.Arrow:
                // 1. 화살표인 경우: 기존대로 경로 변경 후 이동 지속
                ProcessArrowTile(tile);
                break;

            case TileType.Attack:
                // 2. 공격 타일인 경우: 보스 공격 후 이동 멈춤 & 턴 전환
                ProcessAttackTile(tile);
                break;
        }
    }

    /// <summary>
    /// 화살표 타일 발판 로직
    /// </summary>
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

    /// <summary>
    /// 공격 타일 발판 로직
    /// </summary>
    private void ProcessAttackTile(KTH_Tile tile)
    {
        currentTween?.Kill();
        isMoving = false;

        // 플레이어 위치를 공격 타일 중앙으로 보정
        transform.position = tile.transform.position;

        Debug.Log($"[PlayerMovement] 공격 타일({tile.gameObject.name}) 도달 -> 이동 중단 및 보스 턴 전환");

        // 바로 카메라 줌아웃 및 보스 턴으로 전환
        EndMovementAndNextTurn();
    }

    private void EndMovementAndNextTurn()
    {
        currentTween?.Kill();
        isMoving = false;

        if (KTH_CameraMoving.Instance != null)
        {
            KTH_CameraMoving.Instance.ZoomOut();
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
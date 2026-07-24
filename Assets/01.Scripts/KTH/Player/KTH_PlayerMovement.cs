using System.Collections; // 🔥 코루틴 사용을 위한 네임스페이스
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
    [Tooltip("이동 시작 시 패널 유무를 체크할 트랜스폼")]
    [SerializeField] private Transform checkTransform;
    [Tooltip("패널/발판/화살표 감지 범위를 원형으로 체크할 반지름")]
    [SerializeField] private float checkRadius = 0.3f;
    [Tooltip("패널이 속한 레이어 Mask (0일 경우 모든 콜라이더 감지)")]
    [SerializeField] private LayerMask pathTileLayer;
    [Tooltip("체크 전 대기할 시간(초)")]
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

    private void Start()
    {
        if (center == null && transform.parent != null)
            center = transform.parent;

        straightDir = transform.up;
    }

    /// <summary>
    /// TurnManager에서 턴이 소진되었을 때 외부에서 호출
    /// </summary>
    public void StartMovement()
    {
        if (isMoving) return;

        // 🔥 코루틴을 실행하여 딜레이 후 감지 및 이동 시작
        StartCoroutine(StartMovementRoutine());
    }

    /// <summary>
    /// 딜레이를 준 후 물리 연산이 완료되었을 때 패널을 체크하는 코루틴
    /// </summary>
    private IEnumerator StartMovementRoutine()
    {
        isMoving = true;

        // 🔥 물리 갱신 및 프레임 안정화를 위해 지정한 시간(0.1초) 대기
        if (checkDelay > 0f)
        {
            yield return new WaitForSeconds(checkDelay);
        }
        else
        {
            yield return new WaitForEndOfFrame();
        }

        // 1. 딜레이 후 CheckTransform 위치의 패널 유무 검사
        bool hasTile = HasTileAtCheckTransform();

        // 2. 패널이 없는 경우: 체크 위치로 이동 후 보스 턴 전환
        if (checkTransform != null && !hasTile)
        {
            Debug.Log("[PlayerMovement] 체크 위치에 패널이 없습니다! 체크 위치로 이동 후 보스 턴 전환");
            MoveToCheckTransformAndEndTurn();
            yield break;
        }

        // 3. 패널이 정상적으로 존재하는 경우: 카메라 줌인 후 이동
        if (KTH_CameraMoving.Instance != null)
        {
            KTH_CameraMoving.Instance.ZoomIn();
        }

        isMoving = false; // MoveStep 내부에서 true로 다시 변경함
        MoveStep();
    }

    private bool HasTileAtCheckTransform()
    {
        if (checkTransform == null)
        {
            Debug.LogWarning("[PlayerMovement] CheckTransform이 연결되지 않았습니다.");
            return true;
        }

        Collider2D hit;
        if (pathTileLayer == 0)
        {
            hit = Physics2D.OverlapCircle(checkTransform.position, checkRadius);
        }
        else
        {
            hit = Physics2D.OverlapCircle(checkTransform.position, checkRadius, pathTileLayer);
        }

        if (hit != null)
        {
            // 감지된 오브젝트가 플레이어 자신 또는 자식이면 패널이 없는 것으로 간주
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform))
            {
                Debug.Log($"[PlayerMovement] 감지된 콜라이더가 플레이어 자신({hit.name})이므로 패널 없음 처리");
                return false;
            }

            Debug.Log($"[PlayerMovement] 패널 감지 성공: {hit.name}");
            return true;
        }

        Debug.Log("[PlayerMovement] 체크 위치에 패널 없음");
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
                isMoving = false;

                if (KTH_CameraMoving.Instance != null)
                {
                    KTH_CameraMoving.Instance.ZoomOut();
                }

                if (KTH_TurnManager.Instance != null)
                {
                    KTH_TurnManager.Instance.NextTurn();
                }
            });
    }

    private void MoveStep()
    {
        if (isMoving) return;
        isMoving = true;

        if (isOnRing)
        {
            MoveStepOnRing();
        }
        else
        {
            MoveStepStraight();
        }
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
            Debug.Log($"[PlayerMovement] 맨 끝 콜라이더({other.name}) 충돌! -> 보스 턴 진행");
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
            return;
        }

        KTH_Arrow arrow = other.GetComponentInParent<KTH_Arrow>();
        if (arrow == null) return;

        Vector2 arrowDir = arrow.GetArrowDirection();

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
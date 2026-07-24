using System.Collections;
using DG.Tweening; // DOTween 연출을 원치 않으시면 삭제 가능합니다.
using UnityEngine;

public class KTH_AttackObject : MonoBehaviour
{
    [Header("수명 설정")]
    [SerializeField] private int maxTurnLifetime = 2; // 2턴 유지

    [Header("소멸 연출 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string destroyTriggerName = "isDestroy"; // 소멸 애니메이션 트리거 이름
    [SerializeField] private float destroyDelay = 0.5f; // 애니메이션 대기 시간 (또는 DOTween 연출 시간)

    private int elapsedTurns = 0;
    private bool wasPlayerTurn = false;
    private bool isDestroying = false; // 중복 파괴 방지 플래그

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (isDestroying || KTH_TurnManager.Instance == null) return;

        bool isPlayerInput = (KTH_TurnManager.Instance.CurrentState == KTH_TurnManager.TurnState.PlayerInput);

        // 플레이어 턴으로 '전환되는 순간' 감지 (1턴 경과)
        if (isPlayerInput && !wasPlayerTurn)
        {
            elapsedTurns++;
            Debug.Log($"⏳ [공격 오브젝트] {elapsedTurns}턴 경과 (최대 {maxTurnLifetime}턴)");

            if (elapsedTurns > maxTurnLifetime)
            {
                StartCoroutine(DestroyRoutine());
            }
        }

        wasPlayerTurn = isPlayerInput;
    }

    /// <summary>
    /// 🔥 소멸 애니메이션을 재생한 후 오브젝트를 파괴하는 코루틴
    /// </summary>
    private IEnumerator DestroyRoutine()
    {
        isDestroying = true;
        Debug.Log("🧹 [공격 오브젝트] 소멸 애니메이션 실행 후 삭제합니다.");

        // 1. Animator 트리거가 있는 경우 실행
        if (animator != null && !string.IsNullOrEmpty(destroyTriggerName))
        {
            animator.SetTrigger(destroyTriggerName);
        }
        else
        {
            // 2. 별도의 애니메이션 클립이 없을 경우 DOTween으로 스케일이 줄어들며 사라지는 기본 연출
            transform.DOScale(Vector3.zero, destroyDelay).SetEase(Ease.InBack);
        }

        // 콜라이더를 꺼서 사라지는 동안 플레이어와 중복 충돌하지 않도록 처리
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        // 소멸 연출 대기
        yield return new WaitForSeconds(destroyDelay);

        // 실제 오브젝트 파괴
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroying) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("💥 [공격 오브젝트] 플레이어와 충돌함!");
            KTH_PlayerHealth.Instance.TakeDamage(20);
            // TODO: 플레이어 데미지 처리 (TakeDamage) 들어갈 자리
        }
    }
}
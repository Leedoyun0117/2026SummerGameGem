using System;
using System.Collections;
using UnityEngine;

public class KTH_NormalAttack : KTH_BossAttack
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackParamName = "isAttack";

    private void Awake()
    {
        // Animator가 할당되지 않았다면 오브젝트에서 자동으로 가져옵니다.
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected override IEnumerator AttackRoutine(Action onAttackFinished)
    {
        Debug.Log("⚡ [보스 공격] 일반 공격 시작!");

        // 1. 애니메이션 파라미터 isAttack = true 설정 (애니메이션 재생)
        if (animator != null)
        {
            animator.SetBool(attackParamName, true);
        }

        // 2. 애니메이션이 재생 완료될 때까지 대기
        // (attackDuration 동안 대기하거나, 애니메이션 길이에 맞게 연출 대기)
        yield return new WaitForSeconds(attackDuration);

        // 3. [TODO] 플레이어 데미지 처리 (TakeDamage) 들어갈 자리
        // TakeDamageToPlayer(); 
        Debug.Log("💥 [보스 공격] 타격 판정 (플레이어 데미지 처리 지점)");
        KTH_PlayerHealth.Instance.TakeDamage(30);

        // 4. 애니메이션 파라미터 초기화 (다음 공격을 위해 false)
        if (animator != null)
        {
            animator.SetBool(attackParamName, false);
        }

        // 5. 공격 완료 알림
        onAttackFinished?.Invoke();
    }
}
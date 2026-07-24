using System;
using System.Collections;
using UnityEngine;


public class KTH_BossAttack2 : KTH_BossAttack
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackParamName = "isAttack";

    [Header("Object Spawn Settings")]
    [SerializeField] private GameObject attackPrefab; // 소환할 공격 오브젝트 프리팹
    [SerializeField] private Transform spawnPoint;    // 오브젝트가 소환될 위치 (없으면 보스 위치)

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected override IEnumerator AttackRoutine(Action onAttackFinished)
    {
        Debug.Log("⚡ [보스 공격] 공격 애니메이션 및 오브젝트 소환 시작!");

        // 1. 애니메이션 재생
        if (animator != null)
        {
            animator.SetBool(attackParamName, true);
        }

        // 2. 공격 오브젝트 소환 (spawnPoint가 없으면 보스 본인 위치)
        Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

        if (attackPrefab != null)
        {
            Instantiate(attackPrefab, spawnPosition, spawnRotation);
            Debug.Log("🔮 [보스 공격] 공격 오브젝트 소환 완료!");
        }

        // 3. 애니메이션 및 공격 연출 대기
        yield return new WaitForSeconds(attackDuration);

        // 4. 애니메이션 파라미터 초기화
        if (animator != null)
        {
            animator.SetBool(attackParamName, false);
        }

        // 5. 공격 완료 알림
        onAttackFinished?.Invoke();
    }
}

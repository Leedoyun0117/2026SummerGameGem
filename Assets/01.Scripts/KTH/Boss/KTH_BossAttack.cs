using System;
using System.Collections;
using UnityEngine;

public abstract class KTH_BossAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] protected float attackDuration = 1.5f; // 공격 수행 시간

    /// <summary>
    /// 보스 턴에 호출될 공격 실행 메서드
    /// </summary>
    /// <param name="onAttackFinished">공격이 완료되었을 때 호출할 콜백 함수</param>
    public virtual void ExecuteAttack(Action onAttackFinished)
    {
        StartCoroutine(AttackRoutine(onAttackFinished));
    }

    protected abstract IEnumerator AttackRoutine(Action onAttackFinished);
}

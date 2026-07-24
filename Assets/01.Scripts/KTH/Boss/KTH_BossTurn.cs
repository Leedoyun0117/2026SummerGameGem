using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KTH_BossTurn : MonoBehaviour
{
    [Header("보스 공격 패턴 목록")]
    [SerializeField] private List<KTH_BossAttack> attackPatterns = new List<KTH_BossAttack>();

    [Header("기존 스포너 참조")]
    [SerializeField] private KTH_RandomEnemySpawner randomSpawner;

    private void Start()
    {
        // 인스펙터에 수동으로 안 넣었을 경우, 보스에 붙은 모든 공격 패턴 자동 수집
        if (attackPatterns == null || attackPatterns.Count == 0)
        {
            attackPatterns.AddRange(GetComponents<KTH_BossAttack>());
        }

        if (randomSpawner == null)
        {
            randomSpawner = GetComponent<KTH_RandomEnemySpawner>();
        }
    }

    public void TakeTurn()
    {
        Debug.Log("[BossTurn] 보스 턴 시작");

        // 1. 기존 적/화살표 스폰 기능 실행
        if (randomSpawner != null)
        {
            randomSpawner.OnMyTurnSpawn();
        }

        // 2. 랜덤 공격 패턴 실행
        ExecuteRandomAttack();
    }

    private void ExecuteRandomAttack()
    {
        // 등록된 공격 패턴이 없을 경우 기본 대기 후 턴 종료
        if (attackPatterns == null || attackPatterns.Count == 0)
        {
            Debug.LogWarning("[BossTurn] 등록된 공격 패턴이 없습니다. 바로 턴을 종료합니다.");
            Invoke(nameof(FinishTurn), 1f);
            return;
        }

        // 등록된 패턴 중 하나를 랜덤으로 선택
        int randomIndex = Random.Range(0, attackPatterns.Count);
        KTH_BossAttack selectedAttack = attackPatterns[randomIndex];

        Debug.Log($"[BossTurn] 랜덤 공격 선택됨: {selectedAttack.GetType().Name}");

        // 선택된 공격 실행 (완료 시 FinishTurn 콜백 호출)
        selectedAttack.ExecuteAttack(FinishTurn);
    }

    private void FinishTurn()
    {
        Debug.Log("[BossTurn] 보스 턴 및 공격 종료");

        // 플레이어 턴으로 넘어가기
        if (KTH_TurnManager.Instance != null)
        {
            KTH_TurnManager.Instance.NextTurn();
        }
    }
}
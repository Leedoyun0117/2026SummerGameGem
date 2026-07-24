using UnityEngine;

public class KTH_BossTurn : MonoBehaviour
{
    [Header("보스 행동 대기 시간")]
    [SerializeField] private float actionDelay = 1f;

    [Header("랜덤 스포너 참조")]
    [SerializeField] private KTH_RandomEnemySpawner randomSpawner;

    public void TakeTurn()
    {
        Debug.Log("[BossTurn] 보스 턴 시작 -> 적 랜덤 스폰 진행");

        // 🔥 보스 턴 시작 시 스폰 실행!
        if (randomSpawner != null)
        {
            randomSpawner.OnMyTurnSpawn();
        }
        else
        {
            // 만약 동일한 오브젝트에 붙어있다면 자동으로 찾아오기
            randomSpawner = GetComponent<KTH_RandomEnemySpawner>();
            if (randomSpawner != null)
            {
                randomSpawner.OnMyTurnSpawn();
            }
        }
        Debug.Log("[BossTurn] 보스 턴 시작");

        // 보스 패턴 연출 후 FinishTurn 호출
        Invoke(nameof(FinishTurn), actionDelay);
    }

    private void FinishTurn()
    {
        Debug.Log("[BossTurn] 보스 턴 종료");

        // 🔥 보스 공격이 끝났으므로 NextTurn 호출 (플레이어 조작 턴으로 복귀 및 턴 충전)
        KTH_TurnManager.Instance.NextTurn();
    }
}

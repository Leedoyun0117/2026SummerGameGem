using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 보스 전투의 턴 흐름: 일정 주기로 보스가 플레이어를 공격하고, 보스가 쓰러지면 승리 처리(맵으로 복귀)한다.
// 무기 선택/공격 자체는 KTH_BossWeaponUIController가 담당하고, 여기는 "시간이 지나면 보스가 반격한다"는
// 턴 흐름과 승패 판정만 맡는다(LDY_BattleTurnManager의 타임아웃 패턴과 비슷하되 보스 전용으로 완전히 별개).
public class BossTurn : MonoBehaviour
{
    [Header("보스 공격 주기 (초마다 플레이어를 공격)")]
    [SerializeField] private float attackInterval = 8f;
    [SerializeField] private int attackDamage = 8;

    [Header("승리 후 돌아갈 맵 씬 이름 (LDY_MapManager가 있는 씬)")]
    [SerializeField] private string mapSceneName = "LDY_TestScene";

    [Header("보스를 쓰러뜨린 뒤 맵으로 돌아가기 전 대기 시간(연출용)")]
    [SerializeField] private float victoryDelay = 1.5f;

    private float attackTimer;
    private bool battleEnded;

    private void Start()
    {
        attackTimer = attackInterval;

        if (KTH_BossController.Instance != null)
        {
            KTH_BossController.Instance.OnDefeated += HandleBossDefeated;
        }
    }

    private void OnDestroy()
    {
        if (KTH_BossController.Instance != null)
        {
            KTH_BossController.Instance.OnDefeated -= HandleBossDefeated;
        }
    }

    private void Update()
    {
        if (battleEnded) return;
        if (KTH_BossController.Instance == null || KTH_BossController.Instance.IsDead) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = attackInterval;
            if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.TakeDamage(attackDamage);
        }
    }

    private void HandleBossDefeated()
    {
        if (battleEnded) return;
        battleEnded = true;

        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        if (victoryDelay > 0f) yield return new WaitForSeconds(victoryDelay);

        if (LDY_MapManager.Instance != null) LDY_MapManager.Instance.CompleteActiveNode();

        if (!string.IsNullOrEmpty(mapSceneName)) SceneManager.LoadScene(mapSceneName);
    }
}

using UnityEngine;

// 한 턴에 turnDuration(기본 20초)을 주고, 그 시간이 다 되면(플레이어가 그동안 몇 번을 공격했든 상관없이)
// 시간 초과 처리를 한다 - 씬에 있는 LDY_Enemy 중 NoAbility 타입(1번 - 특별한 반격 없는 대신 턴 종료 시
// 원거리 공격하는 적)이 전부 플레이어에게 자기 데미지만큼 공격한 뒤 타이머를 리셋한다.
public class LDY_BattleTurnManager : MonoBehaviour
{
    public static LDY_BattleTurnManager Instance { get; private set; }

    [SerializeField] private float turnDuration = 20f;

    public float TimeRemaining { get; private set; }
    public float TurnDuration => turnDuration;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        // 공격 이펙트(빔/관통 연출)가 재생되는 동안은 시간이 멈춘다.
        if (LDY_AttackTargetController.Instance != null && LDY_AttackTargetController.Instance.IsResolvingEffect) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f) HandleTimeout();
    }

    // 시간 초과 - NoAbility 타입 적들이 전부 플레이어를 공격한다.
    private void HandleTimeout()
    {
        LDY_Enemy[] enemies = Object.FindObjectsByType<LDY_Enemy>(FindObjectsSortMode.None);
        foreach (LDY_Enemy enemy in enemies)
        {
            if (enemy.EnemyType != LDY_EnemyType.NoAbility) continue;
            if (KTH_PlayerHealth.Instance == null) continue;

            KTH_PlayerHealth.Instance.TakeDamage(enemy.TimeoutAttackDamage);
        }

        ResetTimer();
    }

    private void ResetTimer()
    {
        TimeRemaining = turnDuration;
    }
}

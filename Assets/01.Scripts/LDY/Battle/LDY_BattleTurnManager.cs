using System.Collections;
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
        UpdateFragmentCharge();
        if (TimeRemaining <= 0f) HandleTimeout();
    }

    // 턴이 흐르는 동안(0~1) NoAbility 적들 몸 옆에 파편이 점점 모이는 것처럼 보이게 진행도를 넘겨준다.
    private void UpdateFragmentCharge()
    {
        float progress = 1f - Mathf.Clamp01(TimeRemaining / turnDuration);

        LDY_Enemy[] enemies = Object.FindObjectsByType<LDY_Enemy>(FindObjectsSortMode.None);
        foreach (LDY_Enemy enemy in enemies)
        {
            if (enemy.EnemyType != LDY_EnemyType.NoAbility) continue;
            enemy.SetFragmentChargeProgress(progress);
        }
    }

    // 시간 초과 - NoAbility 타입 적들이 전부 플레이어를 공격한다. 각자 이펙트가 실제로 도착하는
    // 시점(TravelDuration)에 맞춰서 그때 가서야 데미지를 준다(파편이 맞는 순간 = 데미지 타이밍).
    private void HandleTimeout()
    {
        LDY_Enemy[] enemies = Object.FindObjectsByType<LDY_Enemy>(FindObjectsSortMode.None);
        foreach (LDY_Enemy enemy in enemies)
        {
            if (enemy.EnemyType != LDY_EnemyType.NoAbility) continue;
            if (KTH_PlayerHealth.Instance == null) continue;

            enemy.ReleaseFragmentCharge();
            StartCoroutine(TimeoutAttackRoutine(enemy));
        }

        ResetTimer();
    }

    private IEnumerator TimeoutAttackRoutine(LDY_Enemy enemy)
    {
        // KTH_PlayerHealth.transform.position은 배틀 보드와 다른 좌표계(오버월드 등)일 수 있어서
        // 조준용으로 쓰지 않는다 - 배틀 보드 안에서의 "플레이어 위치"는 LDY_AttackTargetController의
        // PlayerBattlePosition(활 레이저 발사 위치와 동일 기준)을 대신 쓴다.
        Vector3 targetPos = LDY_AttackTargetController.Instance != null
            ? LDY_AttackTargetController.Instance.PlayerBattlePosition
            : KTH_PlayerHealth.Instance.transform.position;

        float travelWait = enemy.PlayTimeoutAttackEffect(targetPos);

        if (travelWait > 0f) yield return new WaitForSeconds(travelWait);

        if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.TakeDamage(enemy.TimeoutAttackDamage);
    }

    private void ResetTimer()
    {
        TimeRemaining = turnDuration;
    }
}

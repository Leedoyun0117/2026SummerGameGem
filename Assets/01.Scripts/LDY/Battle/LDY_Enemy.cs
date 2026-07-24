using UnityEngine;

// 적 종류:
// - NoAbility (1번): 특별한 반격 능력 없음. 어떤 무기로 맞아도 그냥 죽는다.
//   대신 턴 제한 시간(LDY_BattleTurnManager) 안에 죽이지 못하면 원거리 공격으로 플레이어에게 데미지를 준다.
// - WeaponReflector (2~4번): 정해둔 모양(reflectAgainstShape)의 무기로 맞으면 죽지 않고 오히려
//   플레이어에게 데미지를 반사한다. 그 외의 무기로 맞으면 그냥 죽는다.
//   (예: 2번=활/세로1x4, 3번=망치/사각형2x2, 4번=검/가로4x1)
public enum LDY_EnemyType
{
    NoAbility,
    WeaponReflector,
}

public class LDY_Enemy : MonoBehaviour
{
    [SerializeField] private LDY_EnemyType enemyType = LDY_EnemyType.NoAbility;

    [Header("NoAbility 전용 - 턴 시간 초과 시 플레이어에게 입히는 원거리 데미지")]
    [SerializeField] private int timeoutAttackDamage = 5;

    [Header("WeaponReflector 전용 - 이 모양의 무기로 맞으면 반사(안 죽고 플레이어가 데미지를 입음)")]
    [SerializeField] private LDY_WeaponAttackShape reflectAgainstShape;
    [SerializeField] private int reflectDamage = 10;

    public LDY_EnemyType EnemyType => enemyType;
    public int TimeoutAttackDamage => timeoutAttackDamage;

    // 이 무기로 맞았을 때 반사되는지 확인하고, 반사되면 플레이어에게 데미지를 주고 true를 반환한다.
    // true가 반환되면 호출한 쪽(LDY_AttackTargetController)은 이 적을 죽이면 안 된다.
    public bool TryReflect(LDY_WeaponAttackShape incomingShape)
    {
        if (enemyType != LDY_EnemyType.WeaponReflector) return false;
        if (incomingShape != reflectAgainstShape) return false;

        if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.TakeDamage(reflectDamage);
        return true;
    }
}

using System.Collections;
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

    // 이 무기로 맞으면 반사되는지만 확인한다(데미지 부작용 없음) - 반사 연출(레이저가 먼저 적에게
    // 맞고, 그 다음 반사되어 나에게 돌아오는)을 다 보여준 뒤에 ApplyReflectDamage를 따로 불러야 할 때 쓴다.
    public bool WillReflect(LDY_WeaponAttackShape incomingShape)
    {
        return enemyType == LDY_EnemyType.WeaponReflector && incomingShape == reflectAgainstShape;
    }

    // 실제로 플레이어에게 반사 데미지를 준다 - 반사 이펙트가 나에게 도달하는 시점 등, 원하는 타이밍에 호출.
    public void ApplyReflectDamage()
    {
        if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.TakeDamage(reflectDamage);
    }

    // 이 무기로 맞았을 때 반사되는지 확인하고, 반사되면 즉시 플레이어에게 데미지를 주고 true를 반환한다.
    // true가 반환되면 호출한 쪽(LDY_AttackTargetController)은 이 적을 죽이면 안 된다.
    // (연출 타이밍을 맞춰야 하면 WillReflect + ApplyReflectDamage를 따로 써야 함 - PierceRoutine이 그 경우)
    public bool TryReflect(LDY_WeaponAttackShape incomingShape)
    {
        if (!WillReflect(incomingShape)) return false;
        ApplyReflectDamage();
        return true;
    }

    // 공격을 맞았을 때(죽기 직전) 호출 - 무기의 흔들림 강도/피격 색으로 잠깐 빨개지며 흔들리고,
    // 무기의 hitCrackleEffect가 켜져 있으면(예: 검) 몸 위에 전기 지지직 효과도 같이 낸다.
    public void PlayHitReaction(LDY_Weapon weapon)
    {
        if (weapon == null) return;

        StartCoroutine(HitReactionRoutine(weapon.hitShakeIntensity, weapon.hitShakeDuration, weapon.hitFlashColor));

        if (weapon.hitCrackleEffect)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            float bodySize = sr != null ? Mathf.Max(sr.bounds.size.x, sr.bounds.size.y) : 1f;

            GameObject go = new GameObject("CrackleEffect");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            LDY_CrackleEffect crackle = go.AddComponent<LDY_CrackleEffect>();
            crackle.Init(bodySize, weapon.crackleColorA, weapon.crackleColorB, weapon.hitShakeDuration);
        }
    }

    private IEnumerator HitReactionRoutine(float intensity, float duration, Color flashColor)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        Vector3 originalLocalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (sr != null) sr.color = Color.Lerp(flashColor, originalColor, t);

            Vector2 offset = Random.insideUnitCircle * intensity * (1f - t);
            transform.localPosition = originalLocalPos + (Vector3)offset;

            yield return null;
        }

        if (sr != null) sr.color = originalColor;
        transform.localPosition = originalLocalPos;
    }
}

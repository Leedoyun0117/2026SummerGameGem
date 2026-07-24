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
    [Tooltip("턴 시간 초과 시 이 적 위치에서 플레이어 쪽으로 나가는 원거리 공격 이펙트(화염 파편 등). 비워두면 이펙트 없이 데미지만 들어감")]
    [SerializeField] private GameObject timeoutAttackEffectPrefab;
    [Tooltip("공격하는 동안 이 적 몸의 LDY_BurningEffect 불꽃이 이 배율만큼 커졌다가 공격이 끝나면 다시 줄어든다 (LDY_BurningEffect가 없으면 무시됨)")]
    [SerializeField] private float attackFlareUpMultiplier = 1.6f;
    [SerializeField] private float attackFlareRampDuration = 0.15f;

    [Header("WeaponReflector 전용 - 이 모양의 무기로 맞으면 반사(안 죽고 플레이어가 데미지를 입음)")]
    [SerializeField] private LDY_WeaponAttackShape reflectAgainstShape;
    [SerializeField] private int reflectDamage = 10;

    public LDY_EnemyType EnemyType => enemyType;
    public int TimeoutAttackDamage => timeoutAttackDamage;

    private LDY_FragmentChargeEffect fragmentCharge;

    private void Awake()
    {
        fragmentCharge = GetComponentInChildren<LDY_FragmentChargeEffect>();
    }

    // LDY_BattleTurnManager가 매 프레임 턴 진행도(0~1)를 넘겨준다 - 몸 옆에 LDY_FragmentChargeEffect가
    // 있으면 그 진행도만큼 파편이 점점 모이는 것처럼 보인다(없으면 그냥 무시됨).
    public void SetFragmentChargeProgress(float progress)
    {
        if (fragmentCharge != null) fragmentCharge.SetProgress(progress);
    }

    // 시간 초과로 실제 공격이 나갈 때 호출 - 모여있던 파편이 확 사라지며 정리된다.
    public void ReleaseFragmentCharge()
    {
        if (fragmentCharge != null) fragmentCharge.Release();
    }

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

    // 턴 시간 초과 원거리 공격 이펙트를 이 적 위치에서 targetPosition(플레이어) 쪽으로 재생하고,
    // 그 이펙트가 실제로 도착하는 데 걸리는 시간을 돌려준다(ILDY_TravelEffect 구현 시) - 호출한 쪽에서
    // 이 시간만큼 기다렸다가 데미지를 주면 "맞는 순간"과 데미지 타이밍이 맞아떨어진다.
    public float PlayTimeoutAttackEffect(Vector3 targetPosition)
    {
        if (timeoutAttackEffectPrefab == null)
        {
            Debug.LogWarning($"[LDY_Enemy] {name}: Timeout Attack Effect Prefab이 비어있어서 파편 이펙트를 못 냈습니다(데미지는 그대로 들어감).");
            return 0f;
        }

        Debug.Log($"[LDY_Enemy] {name}: 턴 시간 초과 - 파편 이펙트 발사 ({timeoutAttackEffectPrefab.name})");
        GameObject effect = Instantiate(timeoutAttackEffectPrefab, transform.position, Quaternion.identity);
        if (effect.TryGetComponent(out ILDY_EffectTarget effectTarget)) effectTarget.TargetPosition = targetPosition;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.FragmentFire);
        }

        // Screen Space Camera Canvas의 UI는 실제 거리와 상관없이 렌더 큐 순서로 겹침이 정해지는 경우가
        // 있어서, 이 이펙트가 화면 아래쪽 등 UI와 겹칠 때도 항상 보이도록 렌더 큐를 확실히 뒤(위)로 민다.
        foreach (Renderer renderer in effect.GetComponentsInChildren<Renderer>())
        {
            foreach (Material material in renderer.materials)
            {
                if (material != null) material.renderQueue = 4000;
            }
        }

        float travelDuration = effect.TryGetComponent(out ILDY_TravelEffect travelEffect) ? travelEffect.TravelDuration : 0f;

        // 공격하는 동안 불타는 이펙트가 있으면 커졌다가, 파편이 도착할 즈음 다시 원래 크기로 줄어든다.
        LDY_BurningEffect burning = GetComponentInChildren<LDY_BurningEffect>();
        if (burning != null)
        {
            float holdDuration = Mathf.Max(travelDuration - attackFlareRampDuration * 2f, 0f);
            burning.FlareUp(attackFlareUpMultiplier, attackFlareRampDuration, holdDuration);
        }

        return travelDuration;
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

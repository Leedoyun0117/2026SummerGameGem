using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 전용 무기 선택 UI. 일반 전투의 링보드 조준(LDY_AttackTargetController)과 달리 보스는
// 대상이 하나뿐이라 조준이 필요 없다 - 무기 3개(활/폭발/검) 중 하나를 고르고 "공격" 버튼을 누르면
// 그 무기의 damage만큼 곧바로 KTH_BossController의 체력을 깎는다.
public class KTH_BossWeaponUIController : MonoBehaviour
{
    [SerializeField] private Button[] weaponButtons;
    [SerializeField] private LDY_Weapon[] weapons;
    [SerializeField] private Button attackButton;

    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject[] weaponHighlights;

    [Header("공격 이펙트가 재생될 위치 (비우면 보스 위치)")]
    [SerializeField] private Transform effectOrigin;

    private int selectedIndex = -1;

    private void Awake()
    {
        ApplyDamageBonuses();

        if (attackButton != null) attackButton.gameObject.SetActive(false);
        HideAllHighlights();
        if (descriptionText != null) descriptionText.text = string.Empty;

        if (weaponButtons != null)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int index = i; // 클로저 캡처(반복 변수를 그대로 쓰면 마지막 값으로 고정되어버림)
                if (weaponButtons[i] != null) weaponButtons[i].onClick.AddListener(() => SelectWeapon(index));
            }
        }

        if (attackButton != null) attackButton.onClick.AddListener(ExecuteAttack);
    }

    // 사수자리의 활/태양빛 촛불/가시물고기의 뼛조각 아이템으로 쌓인 무기별 데미지 보너스를 반영한다.
    private void ApplyDamageBonuses()
    {
        if (weapons == null || JCY_RunProgress.Instance == null) return;

        JCY_RunProgress progress = JCY_RunProgress.Instance;
        foreach (LDY_Weapon weapon in weapons)
        {
            if (weapon == null) continue;

            int bonus = weapon.shape switch
            {
                LDY_WeaponAttackShape.Vertical1x4 => progress.bowDamageBonus,
                LDY_WeaponAttackShape.Square2x2 => progress.explosionDamageBonus,
                LDY_WeaponAttackShape.Horizontal4x1 => progress.swordDamageBonus,
                _ => 0,
            };

            if (bonus <= 0) continue;

            weapon.damage += bonus;
            weapon.isForged = true;
        }
    }

    private void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;

        selectedIndex = index;
        LDY_Weapon weapon = weapons[index];

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.WeaponEquip);
        }

        if (attackButton != null) attackButton.gameObject.SetActive(true);

        HideAllHighlights();
        if (weaponHighlights != null && index < weaponHighlights.Length && weaponHighlights[index] != null)
        {
            weaponHighlights[index].SetActive(true);
        }

        if (descriptionText != null)
        {
            descriptionText.text = (weapon.isForged && !string.IsNullOrEmpty(weapon.forgedDescription))
                ? weapon.forgedDescription
                : weapon.description;
        }
    }

    private void HideAllHighlights()
    {
        if (weaponHighlights == null) return;
        foreach (GameObject highlight in weaponHighlights)
        {
            if (highlight != null) highlight.SetActive(false);
        }
    }

    private void ExecuteAttack()
    {
        if (selectedIndex < 0 || weapons == null || selectedIndex >= weapons.Length) return;
        if (KTH_BossController.Instance == null || KTH_BossController.Instance.IsDead) return;

        LDY_Weapon weapon = weapons[selectedIndex];

        if (weapon.hitEffectPrefab != null)
        {
            Vector3 targetPos = KTH_BossController.Instance.transform.position;
            GameObject effect = Instantiate(weapon.hitEffectPrefab, targetPos, Quaternion.identity);
            Destroy(effect, weapon.hitEffectDuration);
        }

        KTH_BossController.Instance.TakeDamage(weapon.damage);
    }
}

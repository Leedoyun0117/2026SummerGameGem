using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 왼쪽 상단 "무기" 버튼을 누르면 무기 3칸이 펼쳐지고, 그중 하나를 고르면 LDY_AttackTargetController에
// 그 무기를 활성화해서 공격 범위 미리보기(빨간 하이라이트)가 뜨도록 한다.
// 이 상태에서 방향키로 대상 위치를 옮기고(LDY_RingSelectionManager가 라우팅), "공격" 버튼 또는
// 스페이스바(LDY_RingSelectionManager 쪽에서 처리)로 확정한다.
public class LDY_WeaponUIController : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject weaponPanel;
    [SerializeField] private Button[] weaponButtons;
    [SerializeField] private LDY_Weapon[] weapons;
    [SerializeField] private Button attackButton;

    [Header("공격 가능(무기 선택됨) 상태일 때만 화면 아래에 뜨는 힌트 UI")]
    [SerializeField] private GameObject attackHintUI;

    private void Awake()
    {
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (attackHintUI != null) attackHintUI.SetActive(false);

        if (toggleButton != null) toggleButton.onClick.AddListener(TogglePanel);

        if (weaponButtons != null)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int index = i; // 클로저 캡처(반복 변수를 그대로 쓰면 마지막 값으로 고정되어버림)
                if (weaponButtons[i] != null)
                {
                    weaponButtons[i].onClick.AddListener(() => SelectWeapon(index));
                }
            }
        }

        if (attackButton != null) attackButton.onClick.AddListener(ExecuteAttackFromButton);
    }

    private void Start()
    {
        // LDY_AttackTargetController.Instance는 그쪽의 Awake에서 설정되는데, Awake 호출 순서는
        // 보장되지 않으므로 (모든 Awake가 끝난 뒤 호출되는) Start에서 구독한다.
        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.OnAttackExecuted += HandleAttackExecuted;
        }
    }

    private void OnDestroy()
    {
        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.OnAttackExecuted -= HandleAttackExecuted;
        }
    }

    // 공격이 실행되면(공격 버튼 클릭이든 스페이스바든) 무기 패널/공격 버튼을 원래대로 감춘다.
    private void HandleAttackExecuted(List<RingSlot> targeted)
    {
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (attackHintUI != null) attackHintUI.SetActive(false);
    }

    private void TogglePanel()
    {
        if (weaponPanel == null) return;
        weaponPanel.SetActive(!weaponPanel.activeSelf);
    }

    private void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;

        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.SetWeapon(weapons[index]);
        }

        if (attackButton != null) attackButton.gameObject.SetActive(true);
        if (attackHintUI != null) attackHintUI.SetActive(true);
    }

    private void ExecuteAttackFromButton()
    {
        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.ExecuteAttack();
            LDY_AttackTargetController.Instance.ClearWeapon();
        }
        // UI 정리(패널/버튼 숨기기)는 HandleAttackExecuted(OnAttackExecuted 이벤트)에서 처리된다.
    }
}

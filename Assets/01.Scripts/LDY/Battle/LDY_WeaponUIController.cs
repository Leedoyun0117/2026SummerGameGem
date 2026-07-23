using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("무기 선택 시 표시할 겉 하이라이트 (weaponButtons/weapons와 같은 순서·같은 길이)")]
    [SerializeField] private GameObject[] weaponHighlights;

    [Header("무기 선택 시 해당 무기의 설명을 보여줄 텍스트")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("설명 텍스트가 한 글자씩 타이핑되는 효과 (글자당 대기 시간, 초)")]
    [SerializeField] private float typingInterval = 0.02f;

    [Header("설명창 슬라이드 효과 (descriptionText의 배경 박스 - 없으면 슬라이드 없이 즉시 표시)")]
    [SerializeField] private RectTransform descriptionBox;
    [SerializeField] private Vector2 descriptionHiddenOffset = new Vector2(0f, -60f); // 숨겨질 때 이 만큼 밀려나서 대기 (기본: 아래로 슬라이드 아웃)
    [SerializeField] private float descriptionSlideDuration = 0.2f;

    private Coroutine descriptionRoutine;
    private Vector2 descriptionShownPos;
    private bool descriptionBoxInitialized;
    private bool descriptionVisible;
    private int selectedWeaponIndex = -1;

    private void Awake()
    {
        if (weaponPanel != null) weaponPanel.SetActive(false);
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (attackHintUI != null) attackHintUI.SetActive(false);
        HideAllHighlights();
        if (descriptionText != null) descriptionText.text = string.Empty;
        InitDescriptionBox();

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

    // 공격이 실행되면(공격 버튼 클릭이든 스페이스바든) 공격 버튼/하이라이트/설명창은 원래대로 감추지만,
    // 무기 패널(무기 목록)은 계속 열어둔다 - 바로 다음 무기를 고르거나 같은 무기를 다시 쓸 수 있게.
    private void HandleAttackExecuted(List<RingSlot> targeted)
    {
        CloseAttackUI();
    }

    // 공격 버튼/힌트/무기 하이라이트/설명창을 전부 닫힌 상태로 되돌리고, 조준 중이던 무기 선택도 해제해서
    // 보드 위의 빨간 범위 하이라이트도 같이 꺼지게 한다.
    // (무기 패널을 닫을 때, 그리고 공격이 실제로 실행됐을 때 둘 다 여기서 처리한다)
    private void CloseAttackUI()
    {
        selectedWeaponIndex = -1;
        if (attackButton != null) attackButton.gameObject.SetActive(false);
        if (attackHintUI != null) attackHintUI.SetActive(false);
        HideAllHighlights();

        if (descriptionRoutine != null) StopCoroutine(descriptionRoutine);
        if (descriptionText != null) descriptionText.text = string.Empty;
        descriptionRoutine = StartCoroutine(HideDescriptionRoutine());

        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.ClearWeapon();
        }
    }

    // descriptionBox가 처음에 놓인 위치(에디터에서 배치한 위치)를 "펼쳐진 상태" 기준점으로 기억해두고,
    // 시작할 땐 그보다 descriptionHiddenOffset만큼 밀려난 위치(숨김 상태)에서 대기시킨다.
    private void InitDescriptionBox()
    {
        if (descriptionBox == null || descriptionBoxInitialized) return;

        descriptionShownPos = descriptionBox.anchoredPosition;
        descriptionBox.anchoredPosition = descriptionShownPos + descriptionHiddenOffset;
        descriptionBox.gameObject.SetActive(false); // 오프셋만으로는 화면 안에 계속 보일 수 있어서 아예 꺼둔다
        descriptionBoxInitialized = true;
        descriptionVisible = false;
    }

    // 이미 다른 무기의 설명이 펼쳐져 있으면 먼저 반대로 슬라이드해서 닫은 뒤(나가는 효과),
    // 그 다음 새 설명으로 다시 슬라이드해서 열고(들어오는 효과) 타이핑한다.
    // 처음 여는 경우(펼쳐진 게 없을 때)는 닫는 과정 없이 바로 연다.
    private IEnumerator SwitchDescriptionRoutine(string description)
    {
        if (descriptionBox != null && descriptionVisible)
        {
            yield return SlideRoutine(descriptionShownPos + descriptionHiddenOffset);
            descriptionBox.gameObject.SetActive(false);
            descriptionVisible = false;
        }

        if (descriptionText != null) descriptionText.text = string.Empty;

        if (descriptionBox != null)
        {
            descriptionBox.gameObject.SetActive(true);
            yield return SlideRoutine(descriptionShownPos);
            descriptionVisible = true;
        }

        if (descriptionText != null && !string.IsNullOrEmpty(description))
        {
            for (int i = 0; i < description.Length; i++)
            {
                descriptionText.text += description[i];
                yield return new WaitForSeconds(typingInterval);
            }
        }
    }

    // 공격 실행 등으로 완전히 닫을 때 - 열려있던 위치에서 숨김 위치로 슬라이드만 한다(타이핑 없음).
    private IEnumerator HideDescriptionRoutine()
    {
        if (descriptionBox == null) yield break;

        yield return SlideRoutine(descriptionShownPos + descriptionHiddenOffset);
        descriptionBox.gameObject.SetActive(false);
        descriptionVisible = false;
    }

    private IEnumerator SlideRoutine(Vector2 target)
    {
        Vector2 start = descriptionBox.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < descriptionSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / descriptionSlideDuration);
            descriptionBox.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        descriptionBox.anchoredPosition = target;
    }

    private void HideAllHighlights()
    {
        if (weaponHighlights == null) return;
        foreach (GameObject highlight in weaponHighlights)
        {
            if (highlight != null) highlight.SetActive(false);
        }
    }

    private void TogglePanel()
    {
        if (weaponPanel == null) return;

        bool willBeActive = !weaponPanel.activeSelf;
        weaponPanel.SetActive(willBeActive);

        if (!willBeActive) CloseAttackUI();
    }

    private void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;

        // 이미 선택되어 있는 무기를 다시 누르면 선택을 취소한다.
        if (selectedWeaponIndex == index)
        {
            CloseAttackUI();
            return;
        }

        selectedWeaponIndex = index;

        if (LDY_AttackTargetController.Instance != null)
        {
            LDY_AttackTargetController.Instance.SetWeapon(weapons[index]);
        }

        if (attackButton != null) attackButton.gameObject.SetActive(true);
        if (attackHintUI != null) attackHintUI.SetActive(true);

        HideAllHighlights();
        if (weaponHighlights != null && index < weaponHighlights.Length && weaponHighlights[index] != null)
        {
            weaponHighlights[index].SetActive(true);
        }

        if (descriptionRoutine != null) StopCoroutine(descriptionRoutine);
        descriptionRoutine = StartCoroutine(SwitchDescriptionRoutine(weapons[index].description));
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

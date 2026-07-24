using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 체력 관리. 링보드가 있는 일반 전투(LDY_Enemy)와 달리 보스는 여러 대 맞아야 죽으므로
// 체력을 따로 들고 있다가 KTH_BossWeaponUIController가 부르는 TakeDamage로 깎인다.
// 실제 승리 처리(맵으로 복귀 등)는 OnDefeated 이벤트를 구독하는 BossTurn이 담당한다.
public class KTH_BossController : MonoBehaviour
{
    public static KTH_BossController Instance { get; private set; }

    [SerializeField] private int maxHP = 60;
    private int currentHP;

    [Header("체력바 UI (없어도 동작함)")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead { get; private set; }

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action OnDefeated;

    private void Awake()
    {
        Instance = this;
        currentHP = maxHP;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        OnHealthChanged?.Invoke(currentHP, maxHP);
        UpdateUI();

        if (currentHP <= 0) HandleDefeated();
    }

    private void HandleDefeated()
    {
        IsDead = true;
        OnDefeated?.Invoke();
    }

    private void UpdateUI()
    {
        if (healthBarFill != null) healthBarFill.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        if (healthText != null) healthText.text = $"{currentHP} / {maxHP}";
    }
}

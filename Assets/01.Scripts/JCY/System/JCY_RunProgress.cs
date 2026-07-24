using TMPro;
using UnityEngine;

// 런(한 판) 전체에서 유지되는 상점 아이템들의 지속 효과/카운터를 모아두는 곳.
// 맵 <-> 상점 <-> 전투 <-> 보스 씬을 오가도 DontDestroyOnLoad로 값이 유지된다.
// 아이템 하나짜리 즉발 효과(체력/골드 등)는 JCY_ItemManager가 KTH_PlayerHealth/StarPieceManager를
// 직접 불러서 처리하고, 여기는 "이후로도 계속 영향을 주는" 값/플래그만 들고 있는다.
public class JCY_RunProgress : MonoBehaviour
{
    public static JCY_RunProgress Instance { get; private set; }

    [Header("무기 데미지 보너스 (사수자리의 활 / 태양빛 촛불 / 가시물고기의 뼛조각) - 보스 전투에서 사용")]
    public int bowDamageBonus;
    public int explosionDamageBonus;
    public int swordDamageBonus;

    [Header("턴 시간 보너스 (블랙홀 조각 + 백조자리의 깃털)")]
    public float turnDurationBonus;

    [Header("아이템/포션 구매 한도 보너스 (히키하이커 가방 / 히치하이커 두루마리)")]
    public int itemCapacityBonus;
    public int potionPurchaseLimitBonus;

    private const int BaseItemCapacity = 10;
    private const int BasePotionPurchaseLimit = 2;

    public int ItemCapacity => BaseItemCapacity + itemCapacityBonus;
    public int PotionPurchaseLimit => BasePotionPurchaseLimit + potionPurchaseLimitBonus;

    public int OwnedItemCount { get; private set; }
    public int PotionPurchaseCountThisShop { get; private set; }

    [Header("패시브 소지 플래그")]
    public bool hasFeather;
    public bool hasMagnetite;
    public bool hasMeteoriteFragments;
    public bool hasHitchhikerGuide;
    public bool hasStarlightLantern;
    public int tinyAsteroidCount;

    private int killCountSinceLastHeal;
    private bool grantingMeteoriteBonus; // NotifyGoldGained -> NotifyPieceCollected -> NotifyGoldGained 재귀 방지

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // LDY_MapManager도 DontDestroyOnLoad라 Awake 순서와 무관하게 Start에서 구독하면 항상 안전하다.
        if (LDY_MapManager.Instance != null)
        {
            LDY_MapManager.Instance.onShopNodeSelected.AddListener(HandleNonCombatNodeEntered);
            LDY_MapManager.Instance.onEventNodeSelected.AddListener(HandleNonCombatNodeEntered);
        }
    }

    // 등불 속 별빛 - 비전투구역(상점/이벤트) 진입마다 최대체력 5 증가
    private void HandleNonCombatNodeEntered(LDY_MapNode node)
    {
        if (!hasStarlightLantern) return;
        if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.PlusMaxHp(5);
    }

    // 상점 아이템(20종) 하나가 실제로 구매 처리될 때마다 JCY_ItemManager가 호출한다.
    // 백조자리의 깃털 효과(구매마다 최대시간 0.75 증가)가 여기서 걸린다.
    public void NotifyItemPurchased()
    {
        OwnedItemCount++;
        if (hasFeather) turnDurationBonus += 0.75f;
    }

    public bool HasItemCapacity() => OwnedItemCount < ItemCapacity;

    public bool TryConsumePotionPurchase()
    {
        if (PotionPurchaseCountThisShop >= PotionPurchaseLimit) return false;
        PotionPurchaseCountThisShop++;
        return true;
    }

    // 상점 씬에 새로 진입할 때(JCY_ShopManager.Awake)마다 포션 구매 횟수를 초기화한다.
    public void ResetPotionPurchaseCount()
    {
        PotionPurchaseCountThisShop = 0;
    }

    // 별빛없는 자철석 - 적 3마리 처치마다 현재체력 1 회복
    public void NotifyEnemyKilled()
    {
        if (!hasMagnetite) return;

        killCountSinceLastHeal++;
        if (killCountSinceLastHeal >= 3)
        {
            killCountSinceLastHeal = 0;
            if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.Heal(1);
        }
    }

    // 운석의 파편 - 골드를 획득할 때마다 30% 확률로 1을 추가로 더 얻는다.
    // amount만큼 이미 지급이 끝난 뒤(LDY_StarPieceManager.NotifyPieceCollected 내부)에 호출된다.
    public void NotifyGoldGained()
    {
        if (!hasMeteoriteFragments || grantingMeteoriteBonus) return;
        if (Random.value >= 0.3f) return;

        grantingMeteoriteBonus = true;
        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.NotifyPieceCollected(1);
        grantingMeteoriteBonus = false;
    }
}

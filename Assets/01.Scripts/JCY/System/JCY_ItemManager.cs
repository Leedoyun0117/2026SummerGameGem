using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

// 상점 아이템(20종) 구매 처리 + 효과 적용을 담당한다.
// 각 아이템의 고유 효과는 ApplyItemEffect에서 JCY_ItemSO.effectType으로 분기해서 처리하고,
// maxHealthUP/curHealthUP/backStarPiece처럼 여러 아이템이 공통으로 쓰는 수치는 그 앞에서
// 값이 0이 아닐 때만 자동으로 적용된다(안 쓰는 아이템은 그냥 0으로 둬서 무시됨).
public class JCY_ItemManager : MonoBehaviour
{
    public static JCY_ItemManager instance;
    [SerializeField] private TextMeshProUGUI countTxt;


    private void Awake()
    {
        instance = this;
    }

    public void UseItem(JCY_Item item)
    {
        JCY_ItemSO itemSO = item.ItemSO;

        int starPiece = StarPieceManager.instance._starPiece;

        if (starPiece < itemSO.cost)
        {
            Debug.Log("돈 없어요");
            return;
        }

        if (JCY_RunProgress.Instance != null && !JCY_RunProgress.Instance.HasItemCapacity())
        {
            Debug.Log("아이템 인벤토리가 가득 찼어요");
            return;
        }

        StarPieceManager.instance.StarPieceDown(itemSO.cost);
        ApplyItemEffect(itemSO);
        if (JCY_RunProgress.Instance != null) JCY_RunProgress.Instance.NotifyItemPurchased();

        int index = item.DisplayIndex;
        JCY_ShopManager.instance._displayCost[index].text = "";
        JCY_ShopManager.instance._displayName[index].text = "품절";

        // 버튼 비활성화
        item.GetComponent<UnityEngine.UI.Button>().interactable = false;
        // 원본 프리팹도 목록에서 지움
        JCY_ShopManager.instance.RemoveItem(item.OriginPrefab);
    }

    // 아이템 하나의 실제 효과를 적용한다. 비용 차감/상점 UI 갱신과는 분리되어 있어서, 우주의 비밀처럼
    // "다른 아이템의 효과만 비용 없이 재발동"하는 경우에도 그대로 재사용할 수 있다.
    public void ApplyItemEffect(JCY_ItemSO itemSO)
    {
        if (itemSO.maxHealthUP != 0 && KTH_PlayerHealth.Instance != null)
        {
            KTH_PlayerHealth.Instance.PlusMaxHp(itemSO.maxHealthUP);
        }

        if (KTH_PlayerHealth.Instance != null)
        {
            if (itemSO.curHealthUP > 0) KTH_PlayerHealth.Instance.Heal(itemSO.curHealthUP);
            else if (itemSO.curHealthUP < 0) KTH_PlayerHealth.Instance.TakeDamage(-itemSO.curHealthUP);
        }

        if (itemSO.backStarPiece != 0)
        {
            StarPieceManager.instance.StarPieceUP(itemSO.backStarPiece);
        }

        JCY_RunProgress progress = JCY_RunProgress.Instance;

        switch (itemSO.effectType)
        {
            case JCY_ItemEffectType.AlienCactus:
                // 외계 선인장 - 최대체력을 깎은 뒤(위 공통 처리) 현재체력을 그 최대체력으로 꽉 채운다.
                if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.SetToFull();
                break;

            case JCY_ItemEffectType.BlackHoleShards:
                // 블랙홀 조각 - 턴 제한시간 보너스 누적(LDY_BattleTurnManager가 시작 시 읽어감)
                if (progress != null) progress.turnDurationBonus += itemSO.maxTimeUP;
                break;

            case JCY_ItemEffectType.BowSagittarius:
                if (progress != null) progress.bowDamageBonus += itemSO.effectAmount;
                break;

            case JCY_ItemEffectType.CandleOfTheSun:
                if (progress != null) progress.explosionDamageBonus += itemSO.effectAmount;
                break;

            case JCY_ItemEffectType.Feather:
                // 백조자리의 깃털 - 구매 즉시 효과는 없고, 이후 모든 구매마다 턴 시간이 늘어난다
                // (JCY_RunProgress.NotifyItemPurchased에서 처리)
                if (progress != null) progress.hasFeather = true;
                break;

            case JCY_ItemEffectType.GravitationalFieldApple:
                if (KTH_PlayerHealth.Instance != null)
                {
                    int overflow = KTH_PlayerHealth.Instance.SwapCurrentAndMax();
                    if (overflow > 0) StarPieceManager.instance.StarPieceUP(overflow / 3);
                }
                break;

            case JCY_ItemEffectType.HickeyHicerScroll:
                if (progress != null) progress.potionPurchaseLimitBonus += itemSO.effectAmount;
                int count = JCY_RunProgress.Instance.PotionPurchaseCountThisShop;
                int Limit = JCY_RunProgress.Instance.PotionPurchaseLimit;
                int point = Limit - count;
                countTxt.text = $"현재 포션 구매 한도:{point}";
                break;

            case JCY_ItemEffectType.HickeyHickeyBag:
                if (progress != null) progress.itemCapacityBonus += itemSO.effectAmount;
                break;

            case JCY_ItemEffectType.HitchhikerGuide:
                if (progress != null) progress.hasHitchhikerGuide = true;
                break;

            case JCY_ItemEffectType.Magnetite:
                if (progress != null) progress.hasMagnetite = true;
                break;

            case JCY_ItemEffectType.MeteoriteFragments:
                if (progress != null) progress.hasMeteoriteFragments = true;
                break;

            case JCY_ItemEffectType.RingOfSaturn:
                if (KTH_PlayerHealth.Instance != null) KTH_PlayerHealth.Instance.reviveCharges++;
                break;

            case JCY_ItemEffectType.SpinefishBoneShard:
                if (progress != null) progress.swordDamageBonus += itemSO.effectAmount;
                break;

            case JCY_ItemEffectType.StarlightLantern:
                if (progress != null) progress.hasStarlightLantern = true;
                break;

            case JCY_ItemEffectType.TinyAsteroid:
                if (progress != null) progress.tinyAsteroidCount++;
                break;

            case JCY_ItemEffectType.UniverseSecrets:
                ApplyRandomOtherItemEffect(itemSO);
                break;

            // Diamond/OrionArmor/RingOfSaturn 이전에 처리 안 된 나머지(SpringWaterAquarius/Watermelon 등)는
            // 위 공통 처리(maxHealthUP/curHealthUP/backStarPiece)만으로 충분해서 별도 분기가 필요 없다.
        }
    }

    // 우주의 비밀 - 상점 전체 아이템(자기 자신 제외) 중 하나를 무작위로 뽑아 그 효과를 비용 차감 없이 재발동한다.
    private void ApplyRandomOtherItemEffect(JCY_ItemSO self)
    {
        if (JCY_ShopManager.instance == null) return;

        GameObject[] all = JCY_ShopManager.instance.AllItemPrefabs;
        if (all == null || all.Length == 0) return;

        List<JCY_ItemSO> candidates = new List<JCY_ItemSO>();
        foreach (GameObject prefab in all)
        {
            if (prefab == null) continue;
            JCY_Item comp = prefab.GetComponent<JCY_Item>();
            if (comp == null || comp.ItemSO == null || comp.ItemSO == self) continue;
            candidates.Add(comp.ItemSO);
        }

        if (candidates.Count == 0) return;

        JCY_ItemSO picked = candidates[Random.Range(0, candidates.Count)];
        Debug.Log($"[JCY_ItemManager] 우주의 비밀 - '{picked.itemName}' 효과 발동");
        ApplyItemEffect(picked);
    }
}

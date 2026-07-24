using UnityEngine;

public class JCY_Potion : MonoBehaviour
{
    public JCY_PotionSO itemSO;

    public void UsePotion()
    {
        if (StarPieceManager.instance._starPiece < itemSO.cost)
        {
            Debug.Log("돈 업쓰요");
            return;
        }

        // 히치하이커 두루마리 - 상점당 포션 구매 한도(기본 2회 + 보너스)를 넘으면 더 못 산다.
        if (JCY_RunProgress.Instance != null && !JCY_RunProgress.Instance.TryConsumePotionPurchase())
        {
            Debug.Log("이번 상점에서 포션 구매 한도를 다 썼어요");
            return;
        }

        Debug.Log("포션 사용");
        switch (itemSO.itemName)
        {
            case "랜덤 박스":
                {
                    int backCost = Random.Range(0, itemSO.cost * 2);
                    StarPieceManager.instance.StarPieceUP(backCost);
                    Debug.Log($"{backCost}획득");
                }
                break;

            case "체력 포션":
                {
                    KTH_PlayerHealth PH = FindFirstObjectByType<KTH_PlayerHealth>();

                    if (PH != null)
                    {
                        StarPieceManager.instance.StarPieceDown(itemSO.cost);
                        PH.Heal(itemSO.health);
                        Debug.Log($"포션으로 {itemSO.health}만큼 체력회복");
                    }
                    else
                        Debug.Log("체력 시스템없음");
                }
                break;

            case "리롤 포션":
                {
                    JCY_ShopManager.instance.DisplayItems();
                    Debug.Log("리롤");
                }
                break;
        }

    }
}

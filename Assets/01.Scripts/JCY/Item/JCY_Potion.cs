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

        Debug.Log("포션 사용");
        switch (itemSO.itemName)
        {
            case "GoldPotion":
                {
                    int backCost = Random.Range(0, itemSO.cost * 2);
                    StarPieceManager.instance.StarPieceUP(backCost);
                    Debug.Log($"{backCost}획득");
                }
                break;

            case "HealthPotion":
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

            case "RerollPoton":
                {
                    JCY_ShopManager.instance.DisplayItems();
                    Debug.Log("리롤");
                }
                break;
        }

    }
}

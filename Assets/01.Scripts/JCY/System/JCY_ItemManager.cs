using System;
using UnityEngine;
using UnityEngine.Apple.ReplayKit;

public class JCY_ItemManager : MonoBehaviour
{
    public static JCY_ItemManager instance;

    private void Awake()
    {
        instance = this;
    }

    public void UseItem(JCY_Item item)
    {
        JCY_ItemSO itemSO = item.ItemSO;

        int starPiece = StarPieceManager.instance._starPiece;

        if(starPiece < itemSO.cost)
        {
            Debug.Log("돈이 업네요");
            return;
        }

        KTH_PlayerHealth.Instance.PlusMaxHp(itemSO.maxHealthUP);

        StarPieceManager.instance.StarPieceUP(itemSO.backStarPiece);

        int index = item.DisplayIndex;
        JCY_ShopManager.instance._displayCost[index].text = "";
        JCY_ShopManager.instance._displayName[index].text = "품절";

        // 버튼 비활성화
        item.GetComponent<UnityEngine.UI.Button>().interactable = false;
        // 다음 상점에서 안 나오게
        JCY_ShopManager.instance.RemoveItem(item.OriginPrefab);

        // 현재 상점에서 제거
    }
}

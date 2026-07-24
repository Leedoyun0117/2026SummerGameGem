using UnityEngine;
using UnityEngine.Apple.ReplayKit;

public class JCY_ItemManager : MonoBehaviour
{
    public static JCY_ItemManager instance;

    private void Awake()
    {
        instance = this;
    }

    public void UseItem(JCY_ItemSO itemSO)
    {
        int starPiece = StarPieceManager.instance._starPiece;
        if(starPiece < itemSO.cost)
        {
            Debug.Log("µ·ÀÌ ¾÷³×¿ä");
            return;
        }
        KTH_PlayerHealth.Instance.PlusMaxHp(itemSO.maxHealthUP);
        StarPieceManager.instance.StarPieceUP(itemSO.backStarPiece);
    }
}

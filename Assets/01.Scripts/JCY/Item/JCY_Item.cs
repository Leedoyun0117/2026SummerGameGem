using UnityEngine;

public class JCY_Item : MonoBehaviour
{
    [SerializeField] private JCY_ItemSO itemSO;

    public JCY_ItemSO ItemSO => itemSO;

    public void OnClick()
    {
        JCY_ItemManager.instance.UseItem(itemSO);
    }
}

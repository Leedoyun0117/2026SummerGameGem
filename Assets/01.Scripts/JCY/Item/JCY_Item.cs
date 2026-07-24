using UnityEngine;

public class JCY_Item : MonoBehaviour
{
    [SerializeField] private JCY_ItemSO itemSO;

    public JCY_ItemSO ItemSO => itemSO;

    public GameObject OriginPrefab { get; set; }
    public int DisplayIndex { get; set; }
    public void OnClick()
    {
        JCY_ItemManager.instance.UseItem(this);
    }
}

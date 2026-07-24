using UnityEngine;
using UnityEngine.EventSystems;

public class JCY_Item : MonoBehaviour , IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private JCY_ItemSO itemSO;

    public JCY_ItemSO ItemSO => itemSO;

    public GameObject OriginPrefab { get; set; }
    public int DisplayIndex { get; set; }
    public void OnClick()
    {
        JCY_ItemManager.instance.UseItem(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        JCY_ToolTipManager.instance.Show(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        JCY_ToolTipManager.instance.Hide();
    }
}

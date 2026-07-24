using TMPro;
using UnityEngine;

public class JCY_ToolTipManager : MonoBehaviour
{
    public static JCY_ToolTipManager instance;

    [SerializeField] private GameObject tooltip;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private int rectX;
    [SerializeField] private int rectY;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        tooltip.SetActive(false);
    }

    public void Show(JCY_Item item)
    {
        tooltip.SetActive(true);

        itemName.text = item.ItemSO.itemName;
        itemDescription.text = item.ItemSO.Description;

        tooltipRect.position =
            item.GetComponent<RectTransform>().position + new Vector3(rectX, rectY, 0f);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}

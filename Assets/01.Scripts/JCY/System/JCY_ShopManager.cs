using TMPro;
using UnityEngine;

public class JCY_ShopManager : MonoBehaviour
{
    public static JCY_ShopManager instance;
    public GameObject[] _items;
    public GameObject[] _displayPoint;
    public TextMeshProUGUI[] _displayCost;
    private JCY_ItemSO _itemSO;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable()
    {
        DisplayItems();
    }

    public void DisplayItems()
    {
        for( int i = 0; i < _displayPoint.Length; i++ )
        {
            int index = Random.Range(0 , _items.Length);
            GameObject obj = Instantiate(_items[index], _displayPoint[i].transform, false);
            JCY_Item item = obj.GetComponent<JCY_Item>();
            _itemSO = item.ItemSO;
            _displayCost[i].text = _itemSO.cost.ToString();
        }
    }
}

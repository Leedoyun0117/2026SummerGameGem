using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JCY_ShopManager : MonoBehaviour
{
    public static JCY_ShopManager instance;

    public GameObject[] _items;
    private List<GameObject> itemList;

    public GameObject[] _displayPoint;
    public TextMeshProUGUI[] _displayCost;
    public TextMeshProUGUI[] _displayName;
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

        itemList = new List<GameObject>(_items);
    }
    private void OnEnable()
    {
        DisplayItems();
    }

    public void DisplayItems()
    {

        for (int i = 0; i < _displayPoint.Length; i++)
        {
            foreach (Transform child in _displayPoint[i].transform)
            {
                Destroy(child.gameObject);
            }

            _displayCost[i].text = "";
            _displayName[i].text = "";
        }


        List<GameObject> displayList = new List<GameObject>(itemList);

        int displayCount = Mathf.Min(_displayPoint.Length, displayList.Count);


        for (int i = 0; i < displayCount; i++)
        {

            int index = Random.Range(0, displayList.Count);

            GameObject prefab = displayList[index];
            GameObject obj = Instantiate(prefab, _displayPoint[i].transform, false);

            JCY_Item item = obj.GetComponent<JCY_Item>();
            item.OriginPrefab = prefab;
            item.DisplayIndex = i;

            _itemSO = item.ItemSO;

            _displayCost[i].text = _itemSO.cost.ToString()+"별조각";
            _displayName[i].text = _itemSO.itemName;

            displayList.RemoveAt(index); // 이번 상점에서만 중복 방지
        }
    }

    public void RemoveItem(GameObject item)
    {
        itemList.Remove(item);
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class JCY_ShopManager : MonoBehaviour
{
    private JCY_ItemSO _itemSO;
    public static JCY_ShopManager instance;
    [Header("아이템 진열")]
    public GameObject[] _items;
    private List<GameObject> itemList;

    public GameObject[] _displayPoint;
    public TextMeshProUGUI[] _displayCost;
    public TextMeshProUGUI[] _displayName;

    [Header("상점 애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject shop;



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
    private void Start()
    {
        shop.SetActive(false);
    }

    private void Update()
    {
        if(Keyboard.current.oKey.wasPressedThisFrame)
            OpenShop();

        if (Keyboard.current.cKey.wasPressedThisFrame)
            CloseShop();
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

    public void OpenShop()
    {
        Debug.Log("상점 오픈");
        shop.SetActive(true);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        animator.SetTrigger("Open");

        DisplayItems();
    }

    public void CloseShop()
    {
        Debug.Log("상점 닫힘");
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        animator.SetTrigger("Close");
    }
    public void CloseEnd()
    {
        shop.SetActive(false);
    }
}

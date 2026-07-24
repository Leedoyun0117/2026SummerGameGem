using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JCY_ShopManager : MonoBehaviour
{
    private JCY_ItemSO _itemSO;
    public static JCY_ShopManager instance;
    [Header("아이템 진열")]
    public GameObject[] _items;
    private List<GameObject> itemList;

    // 우주의 비밀이 무작위 효과를 뽑을 때 쓰는 전체 아이템 풀(이번 런에서 이미 팔린 것도 포함한 원본 목록)
    public GameObject[] AllItemPrefabs => _items;

    public GameObject[] _displayPoint;
    public TextMeshProUGUI[] _displayCost;
    public TextMeshProUGUI[] _displayName;

    [Header("상점 애니메이션")]
    [SerializeField] private Animator animator;
    [SerializeField] private CanvasGroup canvasGroup;



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
        // 상점에 새로 진입할 때마다(리롤 포션으로 다시 여는 것과는 별개) 포션 구매 한도를 초기화한다.
        if (JCY_RunProgress.Instance != null) JCY_RunProgress.Instance.ResetPotionPurchaseCount();

        DisplayItems();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            OpenShop();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            CloseShop();
        }
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
        gameObject.SetActive(true);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        animator.SetTrigger("Open");

        DisplayItems();
    }

    public void CloseShop()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        animator.SetTrigger("Close");
    }
    public void CloseEnd()
    {
        gameObject.SetActive(false);
    }
}

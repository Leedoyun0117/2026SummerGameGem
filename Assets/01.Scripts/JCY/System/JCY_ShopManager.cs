using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class JCY_ShopManager : MonoBehaviour
{
    private JCY_ItemSO _itemSO;
    public static JCY_ShopManager instance;
    [SerializeField] private TextMeshProUGUI countTxt;
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
    [SerializeField]private GameObject shop_UI;



    // shop_UI/canvasGroup/animator/_displayPoint 등은 전부 이 씬(맵) 안에 있는 오브젝트를 가리키는
    // 참조라서 DontDestroyOnLoad로 살려두면 안 된다 - 살려두면 이 매니저는 씬이 넘어가도 안 죽지만
    // 참조하던 UI들은 씬과 함께 파괴되어서, 다음에 씬이 다시 로드된 뒤에는 "MissingReferenceException:
    // shop_UI가 더 이상 존재하지 않습니다" 같은 에러가 난다. instance는 씬이 로드될 때마다 자연스럽게
    // 새로 생기는 이 인스턴스로 계속 갱신되므로 그걸로 충분하다.
    private void Awake()
    {
        instance = this;
        itemList = new List<GameObject>(_items);
    }

    // 맵에서 초록별(Shop 노드)을 클릭하면 LDY_MapManager가 아이리스 연출이 끝난 뒤 이 이벤트를 쏴준다 -
    // O키로 직접 여는 대신 이걸 구독해서 상점을 연다. LDY_MapManager는 DontDestroyOnLoad라 Awake 순서와
    // 무관하게 Start에서 구독하면 항상 안전하다(JCY_RunProgress.Start()와 동일한 패턴).
    private void Start()
    {
        shop_UI.SetActive(false);

        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.AddListener(HandleShopNodeSelected);
    }

    // 이 매니저는 (위 이유로) 씬과 함께 파괴되는데, LDY_MapManager.onShopNodeSelected는 DontDestroyOnLoad라
    // 구독을 안 풀면 죽은 인스턴스의 핸들러가 계속 이벤트 목록에 남아있게 된다 - 다음에 이벤트가 울릴 때
    // 그 죽은 핸들러가 shop_UI 등 이미 파괴된 참조를 건드리면서 예외를 던지고, 그 예외 때문에 뒤이어
    // 등록된(진짜 살아있는) 핸들러까지 호출이 안 되는 문제로 이어진다 - 그래서 반드시 여기서 구독 해제해야 한다.
    private void OnDestroy()
    {
        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.RemoveListener(HandleShopNodeSelected);
    }

    private void HandleShopNodeSelected(LDY_MapNode node)
    {
        OpenShop();
    }

    private void OnEnable()
    {
        // 상점에 새로 진입할 때마다(리롤 포션으로 다시 여는 것과는 별개) 포션 구매 한도를 초기화한다.
        if (JCY_RunProgress.Instance != null) JCY_RunProgress.Instance.ResetPotionPurchaseCount();

        DisplayItems();
    }
    private void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            CloseShop();
        }

        if(Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log("돈저");
            StarPieceManager.instance.StarPieceUP(100);
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
        shop_UI.SetActive(true);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        JCY_RunProgress.Instance.ResetPotionPurchaseCount();
        countTxt.text = $"현재 포션 구매 한도:{JCY_RunProgress.Instance.PotionPurchaseLimit}";

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
        shop_UI.SetActive(false);
    }
}

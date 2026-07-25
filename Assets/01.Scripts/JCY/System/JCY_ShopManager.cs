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

    [Header("배경음악")]
    [SerializeField]private AudioClip _shopBGM;
    [SerializeField] private AudioClip _normalBGM;



    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // 이 매니저는 DontDestroyOnLoad라 맵 씬을 다시 불러와도 안 죽지만, shop_UI/animator/
            // canvasGroup/countTxt/_displayPoint 등은 원래 그 씬에 있던 오브젝트라서 씬이 바뀌면
            // 같이 파괴된다 - 그대로 두면 상점을 열자마자(또는 닫힐 때) MissingReferenceException이 난다.
            // 그래서 중복 인스턴스(=이번에 새로 로드된 씬의 진짜 참조들)를 버리기 전에 그 참조들만
            // 영속 인스턴스로 옮겨온다.
            instance.RebindSceneRefs(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        itemList = new List<GameObject>(_items);
    }

    private void RebindSceneRefs(JCY_ShopManager fresh)
    {
        countTxt = fresh.countTxt;
        _displayPoint = fresh._displayPoint;
        _displayCost = fresh._displayCost;
        _displayName = fresh._displayName;
        animator = fresh.animator;
        canvasGroup = fresh.canvasGroup;
        shop_UI = fresh.shop_UI;

        shop_UI.SetActive(false);
    }
    private void Start()
    {
        // 상점은 "씬(맵)에 들어가면" 자동으로 열리는 게 아니라 초록별(Shop 노드)을 클릭했을 때만 열려야
        // 한다 - 여기서 OpenShop()을 부르면 맵에 들어가자마자 상점이 떠버린다. 기본은 숨겨만 두고,
        // 실제로 여는 건 LDY_ShopUIOpener(맵 노드 클릭 이벤트를 구독)가 OpenShop()을 호출해서 처리한다.
        if (shop_UI != null) shop_UI.SetActive(false);
        else Debug.LogWarning("[JCY_ShopManager] shop_UI가 비어있습니다 - 인스펙터에서 연결해주세요.");
    }
    private void OnEnable()
    {
        // 상점에 새로 진입할 때마다(리롤 포션으로 다시 여는 것과는 별개) 포션 구매 한도를 초기화한다.
        if (JCY_RunProgress.Instance != null) JCY_RunProgress.Instance.ResetPotionPurchaseCount();

        DisplayItems();
    }

    private void Update()
    {
        if(Keyboard.current.oKey.wasPressedThisFrame)
            OpenShop();

        if (Keyboard.current.cKey.wasPressedThisFrame)
            CloseShop();

        if (Keyboard.current.mKey.wasPressedThisFrame)
            StarPieceManager.instance.StarPieceUP(100);
    }


    public void DisplayItems()
    {
        if (_displayPoint == null || _displayCost == null || _displayName == null)
        {
            Debug.LogWarning("[JCY_ShopManager] _displayPoint/_displayCost/_displayName 중 비어있는 게 있어서 아이템을 진열하지 못했습니다.");
            return;
        }

        for (int i = 0; i < _displayPoint.Length; i++)
        {
            if (_displayPoint[i] == null) continue;

            foreach (Transform child in _displayPoint[i].transform)
            {
                Destroy(child.gameObject);
            }

            if (_displayCost[i] != null) _displayCost[i].text = "";
            if (_displayName[i] != null) _displayName[i].text = "";
        }


        List<GameObject> displayList = new List<GameObject>(itemList);

        int displayCount = Mathf.Min(_displayPoint.Length, displayList.Count);


        for (int i = 0; i < displayCount; i++)
        {
            if (_displayPoint[i] == null) continue;

            int index = Random.Range(0, displayList.Count);

            GameObject prefab = displayList[index];
            GameObject obj = Instantiate(prefab, _displayPoint[i].transform, false);

            JCY_Item item = obj.GetComponent<JCY_Item>();
            item.OriginPrefab = prefab;
            item.DisplayIndex = i;

            _itemSO = item.ItemSO;

            if (_displayCost[i] != null) _displayCost[i].text = _itemSO.cost.ToString() + "별조각";
            if (_displayName[i] != null) _displayName[i].text = _itemSO.itemName;

            displayList.RemoveAt(index); // 이번 상점에서만 중복 방지
        }
    }

    public void RemoveItem(GameObject item)
    {
        itemList.Remove(item);
    }

    // canvasGroup/countTxt/animator/JCY_RunProgress.Instance 중 하나라도 비어있으면(씬 설정 누락, 참조가
    // 아직 안 붙었을 때 등) 그 줄에서 예외가 나서 그 아래의 shop_UI 활성화/DisplayItems()까지 통째로
    // 실행이 안 되는 게 "상점 자체가 안 뜬다"의 흔한 원인이었다 - shop_UI를 켜는 것만큼은 무조건 먼저,
    // 확실하게 실행되도록 나머지는 전부 null 체크로 감싼다.
    public void OpenShop()
    {
        if (shop_UI == null)
        {
            Debug.LogWarning("[JCY_ShopManager] shop_UI가 비어있어서 상점을 열 수 없습니다.");
            return;
        }

        shop_UI.SetActive(true);

        if (canvasGroup != null)
        {
            // Animator에 AnimatorController가 안 물려있으면(콘솔에 "Animator is not playing an
            // AnimatorController" 경고) SetTrigger("Open")가 아무 효과도 없어서 CanvasGroup의 alpha가
            // 0으로 남아있는 채로 방치된다 - shop_UI는 활성화됐는데 화면에는 안 보이는 상태. 애니메이터가
            // 있든 없든 상관없이 여기서 직접 alpha를 1로 만들어서 확실히 보이게 한다.
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (JCY_RunProgress.Instance != null)
        {
            JCY_RunProgress.Instance.ResetPotionPurchaseCount();
            if (countTxt != null) countTxt.text = $"현재 포션 구매 한도:{JCY_RunProgress.Instance.PotionPurchaseLimit}";
        }

        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Open");

        if (_shopBGM != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(_shopBGM);
        }

        DisplayItems();
    }

    public void CloseShop()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (_normalBGM != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(_normalBGM);
        }

        // Close 애니메이션이 끝나면 CloseEnd()가 shop_UI를 꺼주는 구조인데, AnimatorController가 없으면
        // 그 이벤트 자체가 안 일어나서 영영 안 꺼진다 - 그럴 땐 여기서 바로 꺼준다.
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Close");
        else CloseEnd();
    }
    public void CloseEnd()
    {
        shop_UI.SetActive(false);
    }
}

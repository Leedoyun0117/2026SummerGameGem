using TMPro;
using UnityEngine;

// 상점 코드(JCY_ItemManager/JCY_Potion/JCY_ShopManager)가 쓰는 화폐 API를 그대로 유지하되,
// 실제 값은 전투 중 코인 드롭을 담당하는 LDY_StarPieceManager로 위임한다 - 예전에는 상점용
// 화폐와 전투용 화폐가 서로 다른 값으로 분리되어 있었는데, 이제 하나로 통합됐다.
public class StarPieceManager : MonoBehaviour
{
    public static StarPieceManager instance;

    public int _starPiece => LDY_StarPieceManager.Instance != null ? LDY_StarPieceManager.Instance.Count : 0;

    [SerializeField] private TextMeshProUGUI _starUI;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        instance = this;
        // DontDestroyOnLoad는 "최상위(루트)" 오브젝트에만 걸 수 있다 - 이 스크립트가 Canvas 밑
        // 자식에 붙어있으면 gameObject 그대로는 예외가 나서(그 뒤 초기화가 전부 씹힘) transform.root를 쓴다.
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void Start()
    {
        // LDY_StarPieceManager도 DontDestroyOnLoad 싱글턴이라 Awake 호출 순서가 보장되지 않는다 -
        // 모든 Awake가 끝난 뒤 호출되는 Start에서 구독해야 항상 안전하게 연결된다.
        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.OnCountChanged += UpdateUI;
        UpdateUI(_starPiece);
    }

    private void UpdateUI(int count)
    {
        if (_starUI != null) _starUI.text = count.ToString();
    }

    public void StarPieceUP(int value)
    {
        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.NotifyPieceCollected(value);
    }

    public void StarPieceDown(int value)
    {
        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.Spend(value);
    }
}

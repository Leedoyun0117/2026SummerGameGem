using UnityEngine;

// 맵에서 초록별(Shop 노드)에 들어가면 다른 로직 없이 지정한 UI 오브젝트를 그냥 SetActive(true)로 켠다.
// JCY_ShopManager의 애니메이션/CanvasGroup 파이프라인과 무관하게 동작해서, "노드 진입 -> UI 켜짐" 연결
// 자체가 되는지부터 단순하게 확인/보장하기 위한 용도.
public class LDY_ShopUIOpener : MonoBehaviour
{
    [SerializeField] private GameObject shopUI;

    private void Start()
    {
        if (shopUI != null) shopUI.SetActive(false);

        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.AddListener(HandleShopNodeSelected);
    }

    private void OnDestroy()
    {
        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.RemoveListener(HandleShopNodeSelected);
    }

    private void HandleShopNodeSelected(LDY_MapNode node)
    {
        if (shopUI != null) shopUI.SetActive(true);
    }

    // 닫기(나가기) 버튼의 OnClick()에 이 메서드를 직접 연결해서 쓰면 됨.
    public void Close()
    {
        if (shopUI != null) shopUI.SetActive(false);
    }
}

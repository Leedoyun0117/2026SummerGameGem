using System.Collections;
using UnityEngine;

// 초록별(Shop 노드) 클릭 -> openDelay(기본 0.1초) 뒤에 shopUI.SetActive(true), 나가기 버튼 ->
// shopUI.SetActive(false). Animator/CanvasGroup/BGM 같은 다른 시스템 상태가 뭐가 됐든 이 on/off 하나는
// 항상 확실하게 동작하게 하는 게 목적. 아이템 진열(DisplayItems)은 JCY_ShopManager가 있으면 곁다리로
// 같이 시도하되, 그게 실패해도(경고만 찍힘) 켜고 끄는 것 자체는 절대 막히지 않는다.
public class LDY_ShopUIOpener : MonoBehaviour
{
    [SerializeField] private GameObject shopUI;
    [SerializeField] private float openDelay = 0.1f;

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
        StartCoroutine(OpenAfterDelay());
    }

    private IEnumerator OpenAfterDelay()
    {
        if (openDelay > 0f) yield return new WaitForSeconds(openDelay);

        if (shopUI != null) shopUI.SetActive(true);
        if (JCY_ShopManager.instance != null) JCY_ShopManager.instance.DisplayItems();
    }

    // 나가기 버튼의 OnClick()에 이 메서드를 직접 연결해서 쓰면 됨.
    public void Close()
    {
        if (shopUI != null) shopUI.SetActive(false);
    }
}

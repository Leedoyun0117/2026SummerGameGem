using TMPro;
using UnityEngine;

// LDY_StarPieceManager는 DontDestroyOnLoad라 씬이 바뀌어도 안 죽지만, 그 매니저가 참조하는 UI
// (드롭이 생성될 위치, 카운트 텍스트, 조각이 날아갈 목적지 등)는 각 씬(맵/전투)마다 따로 있는 오브젝트다.
// 씬이 바뀔 때마다 이 컴포넌트가 "지금 이 씬의 UI"를 매니저에게 새로 등록해줘야, 씬을 오갈 때 참조가
// 끊기지 않는다(끊기면 적을 죽여도 조각이 하나도 안 나오고 개수도 안 늘어남).
// 별 UI가 있는 모든 씬(맵/전투 등)에 하나씩 붙이고, 그 씬 안의 UI 오브젝트들을 연결해두면 됨.
public class LDY_StarPieceUIBinding : MonoBehaviour
{
    [SerializeField] private RectTransform dropUIParent;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private RectTransform flyTarget;
    [SerializeField] private Camera worldCamera;

    private void Start()
    {
        // 씬을 Map부터 안 거치고 이 씬(예: 전투)을 바로 Play 했거나, LDY_StarPieceManager 오브젝트 자체가
        // 이 씬에 없으면 Instance가 null이라 그동안 아무 일도 안 하고 조용히 끝났다 - 그래서 조각이 계속
        // 하나도 안 나왔던 것. 여기서 없으면 만들어서라도 반드시 바인딩한다.
        if (LDY_StarPieceManager.Instance == null)
        {
            GameObject go = new GameObject("LDY_StarPieceManager");
            go.AddComponent<LDY_StarPieceManager>();
        }

        LDY_StarPieceManager.Instance.BindUI(dropUIParent, countText, flyTarget, worldCamera);
    }
}

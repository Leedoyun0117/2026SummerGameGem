using TMPro;
using UnityEngine;

public class JCY_ToolTipManager : MonoBehaviour
{
    public static JCY_ToolTipManager instance;

    [SerializeField] private GameObject tooltip;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private int rectX;
    [SerializeField] private int rectY;

    // tooltip(과 tooltipRect/itemName/itemDescription)은 이 씬(맵) 안에 있는 오브젝트를 가리키는 참조라서
    // DontDestroyOnLoad로 살려두면 안 된다 - 살려두면 이 매니저는 씬이 넘어가도 안 죽지만, 전투 씬에
    // 갔다가 돌아오면 씬이 통째로 다시 로드되면서 새 tooltip 오브젝트가 생기는데, 이미 살아있는(구인스턴스)
    // 매니저의 Awake()는 중복 판정으로 곧장 리턴돼서 새로 생긴 tooltip에 SetActive(false)를 걸어줄 기회가
    // 없다 - 그 결과 씬 파일에 저장된 기본 활성 상태 그대로 툴팁이 켜진 채로 보이게 된다.
    private void Awake()
    {
        instance = this;
        tooltip.SetActive(false);
    }

    public void Show(JCY_Item item)
    {
        tooltip.SetActive(true);

        itemName.text = item.ItemSO.itemName;
        itemDescription.text = item.ItemSO.Description;

        tooltipRect.position =
            item.GetComponent<RectTransform>().position + new Vector3(rectX, rectY, 0f);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}

using TMPro;
using UnityEngine;

// StarPiece 재화 매니저 - 적이 죽은 자리에 조각을 흩뿌리는 연출(SpawnDrops)을 내고, 조각이 목적지
// (dropUIParent 기준 anchoredPosition (0,0))에 도착하면 Count를 올리고 UI 텍스트를 갱신한다.
// LDY_Enemy가 죽을 때(OnDestroy) 이 매니저의 SpawnDrops를 자동으로 부른다.
public class LDY_StarPieceManager : MonoBehaviour
{
    public static LDY_StarPieceManager Instance { get; private set; }

    [Header("드롭 오브젝트가 생성될 UI 부모 (Screen Space Overlay Canvas 하위) - 조각은 이 부모 기준 (0,0)으로 날아간다")]
    [SerializeField] private RectTransform dropUIParent;

    [Header("LDY_StarPieceDrop이 붙은 UI 프리팹(Image 등)")]
    [SerializeField] private GameObject starPieceDropPrefab;

    [SerializeField] private Camera worldCamera;

    [Header("적 한 마리 죽였을 때 떨어지는 조각 개수 범위")]
    [SerializeField] private int minDropCount = 1;
    [SerializeField] private int maxDropCount = 3; // 실제로는 이 값 포함해서 랜덤 (Random.Range(min, max+1))

    [Header("조각이 흩어지는 반경(화면 픽셀 기준)")]
    [SerializeField] private float scatterRadius = 30f;

    [Header("재화 개수를 표시할 텍스트 (없어도 동작함)")]
    [SerializeField] private TextMeshProUGUI countText;

    public int Count { get; private set; }
    public event System.Action<int> OnCountChanged;

    private void Awake()
    {
        Instance = this;
        if (worldCamera == null) worldCamera = Camera.main;
        UpdateCountText();
    }

    // 조각 하나가 목적지에 도착할 때마다 호출된다(LDY_StarPieceDrop에서).
    public void NotifyPieceCollected(int amount)
    {
        Count += amount;
        OnCountChanged?.Invoke(Count);
        UpdateCountText();
    }

    private void UpdateCountText()
    {
        if (countText != null) countText.text = Count.ToString();
    }

    // 적이 죽은 월드 좌표에서 1~3개의 StarPiece 조각을 스폰한다.
    public void SpawnDrops(Vector3 worldPosition)
    {
        if (starPieceDropPrefab == null || dropUIParent == null)
        {
            Debug.LogWarning("[LDY_StarPieceManager] Star Piece Drop Prefab 또는 Drop UI Parent가 비어있어서 조각을 스폰하지 못했습니다.");
            return;
        }

        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null)
        {
            Debug.LogWarning("[LDY_StarPieceManager] 카메라를 찾지 못해 조각을 스폰하지 못했습니다.");
            return;
        }

        int count = Random.Range(minDropCount, maxDropCount + 1);
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(starPieceDropPrefab, dropUIParent);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 scatterOffset = Random.insideUnitCircle * scatterRadius;
            rt.position = screenPos + (Vector3)scatterOffset;

            LDY_StarPieceDrop drop = go.GetComponent<LDY_StarPieceDrop>();
            if (drop != null) drop.Init(1);
        }
    }
}

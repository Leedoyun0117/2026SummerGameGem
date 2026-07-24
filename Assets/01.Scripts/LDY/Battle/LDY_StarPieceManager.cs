using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// StarPiece 재화 매니저 - 적이 죽은 자리에 조각을 흩뿌리는 연출(SpawnDrops)을 내고, 조각이 목적지
// (dropUIParent 기준 anchoredPosition (0,0))에 도착하면 Count를 올리고 UI 텍스트를 갱신한다.
// LDY_Enemy가 죽을 때(OnDestroy) 이 매니저의 SpawnDrops를 자동으로 부른다.
public class LDY_StarPieceManager : MonoBehaviour
{
    public static LDY_StarPieceManager Instance { get; private set; }

    [Header("드롭 오브젝트가 생성될 UI 부모 (Screen Space Overlay Canvas 하위)")]
    [SerializeField] private RectTransform dropUIParent;

    [Header("조각이 날아가는 목적지(플레이어 위치/재화 카운터 아이콘 등) - 비워두면 dropUIParent 기준 (0,0)으로 날아감")]
    [SerializeField] private RectTransform flyTarget;

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
        // 맵 <-> 전투 씬을 오가도 재화 UI가 계속 떠 있도록, 이미 하나 있으면(이전 씬에서 넘어온 것)
        // 이번에 새로 생긴 쪽을 파괴하고, 처음이면 파괴되지 않게 표시해둔다.
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad는 "최상위(루트)" 오브젝트에만 걸 수 있다 - 이 스크립트가 Canvas 밑
        // 자식에 붙어있으면 gameObject 그대로는 예외가 나서(그 뒤 초기화가 전부 씹힘) transform.root를 쓴다.
        DontDestroyOnLoad(transform.root.gameObject);

        if (worldCamera == null) worldCamera = Camera.main;
        UpdateCountText();

        // 씬이 바뀌면(맵 <-> 전투) Camera.main도 그 씬의 카메라로 바뀌므로, 캐시해둔 worldCamera를
        // 새로 잡아준다 - 안 그러면 화면 좌표 계산이 이전 씬 카메라 기준으로 어긋난다.
        SceneManager.sceneLoaded += (scene, mode) => worldCamera = Camera.main;
    }

    // 이 매니저는 DontDestroyOnLoad라 씬이 바뀌어도 안 죽지만, dropUIParent/countText/flyTarget/worldCamera는
    // 원래 그 씬에 있던 오브젝트라서 씬이 바뀌면 같이 파괴된다 - 그대로 두면 다음 씬에서는 죽은 참조를 들고
    // 있게 되어(적을 죽여도 조각이 안 나오는 원인) worldCamera만 sceneLoaded로 갱신하던 걸로는 부족했다.
    // 별 UI가 있는 씬마다 LDY_StarPieceUIBinding을 하나 붙여두면, 그 씬이 로드될 때 여기로 자기 씬의
    // UI를 다시 등록해준다.
    public void BindUI(RectTransform newDropUIParent, TextMeshProUGUI newCountText, RectTransform newFlyTarget, Camera newWorldCamera)
    {
        dropUIParent = newDropUIParent;
        countText = newCountText;
        flyTarget = newFlyTarget;
        if (newWorldCamera != null) worldCamera = newWorldCamera;

        UpdateCountText();
    }

    // 조각 하나가 목적지에 도착할 때마다 호출된다(LDY_StarPieceDrop에서). 상점에서 아이템 보상으로
    // 골드를 지급할 때도(JCY_ItemManager 등) 이 메서드를 그대로 쓴다 - 화폐가 하나로 통합되어 있다.
    public void NotifyPieceCollected(int amount)
    {
        Count += amount;
        OnCountChanged?.Invoke(Count);
        UpdateCountText();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.CoinGain);
        }

        // 운석의 파편 - 골드를 얻을 때마다 30% 확률로 1을 추가로 더 준다.
        if (JCY_RunProgress.Instance != null) JCY_RunProgress.Instance.NotifyGoldGained();
    }

    // 골드를 소비한다(상점 구매 등). 음수로 내려가지 않게 0에서 클램프한다.
    public void Spend(int amount)
    {
        Count = Mathf.Max(0, Count - amount);
        OnCountChanged?.Invoke(Count);
        UpdateCountText();

        // 히치하이커 안내서 - 소비 결과 돈이 정확히 0이 되면 50을 되돌려준다.
        if (Count == 0 && JCY_RunProgress.Instance != null && JCY_RunProgress.Instance.hasHitchhikerGuide)
        {
            NotifyPieceCollected(50);
        }
    }

    private void UpdateCountText()
    {
        if (countText != null) countText.text = Count.ToString();
    }

    // dropUIParent는 원래 있던 씬의 Canvas를 가리키는데, 그 씬이 언로드되면 Unity가 파괴해서 "Missing"
    // 참조가 된다(LDY_StarPieceUIBinding으로 씬마다 다시 연결해줄 수도 있지만, 깜빡하거나 씬 설정이
    // 안 맞으면 또 끊긴다) - 그래서 아예 스폰 시점에 매번 확인해서, 없으면 지금 활성 씬에 전체화면
    // Canvas를 즉석에서 만들어 쓴다. 코드만으로 항상 보장되므로 씬마다 수동으로 연결 안 해도 된다.
    private void EnsureDropUIParent()
    {
        if (dropUIParent != null) return;

        GameObject canvasGO = new GameObject("StarPieceDropCanvas(Auto)", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        dropUIParent = (RectTransform)canvasGO.transform;
        Debug.Log("[LDY_StarPieceManager] dropUIParent가 없어서(Missing/None) 자동으로 새 Canvas를 만들어 대신 씁니다.");
    }

    // 적이 죽은 월드 좌표에서 1~3개의 StarPiece 조각을 스폰한다.
    public void SpawnDrops(Vector3 worldPosition)
    {
        EnsureDropUIParent();

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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.CoinDrop);
        }

        int count = Random.Range(minDropCount, maxDropCount + 1);
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);
        Debug.Log($"[LDY_StarPieceManager] SpawnDrops: worldPos={worldPosition}, camera={worldCamera.name}, screenPos={screenPos}, count={count}, dropUIParent={dropUIParent.name}(active={dropUIParent.gameObject.activeInHierarchy}, lossyScale={dropUIParent.lossyScale})");

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(starPieceDropPrefab, dropUIParent);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogWarning("[LDY_StarPieceManager] 스폰된 오브젝트에 RectTransform이 없습니다.");
                continue;
            }

            // dropUIParent가 속한 Canvas가 다른 UI(예: 화면 아래 SPACE 힌트가 있는 BattleUICanvas)와
            // Sort Order가 같으면 어느 게 위로 그려질지 안 정해진다 - 이 조각만 오버라이드 캔버스를
            // 붙여서 확실히 다른 UI보다 위(가장 높은 값)로 그려지게 한다.
            Canvas overrideCanvas = go.GetComponent<Canvas>();
            if (overrideCanvas == null) overrideCanvas = go.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 1000;

            Vector2 scatterOffset = Random.insideUnitCircle * scatterRadius;
            PositionInParent(rt, (Vector2)screenPos + scatterOffset);

            Debug.Log($"[LDY_StarPieceManager] 조각 생성됨: name={go.name}, active={go.activeInHierarchy}, " +
                $"rt.position={rt.position}, rt.anchoredPosition={rt.anchoredPosition}, rt.localScale={rt.localScale}, " +
                $"parentChain={GetParentChainNames(go.transform)}");

            LDY_StarPieceDrop drop = go.GetComponent<LDY_StarPieceDrop>();
            if (drop != null) drop.Init(flyTarget, 1);
        }
    }

    // CanvasScaler 등으로 dropUIParent의 lossyScale이 1이 아니면(예: 0.03), rt.position(월드 좌표)을
    // 직접 설정할 경우 anchoredPosition이 극단적인 값(수만 단위)으로 튀어서 렌더링이 깨진다.
    // RectTransformUtility로 스크린 좌표 -> 로컬(anchoredPosition) 변환을 제대로 거쳐야 한다.
    private void PositionInParent(RectTransform rt, Vector2 screenPosition)
    {
        Canvas canvas = dropUIParent.GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dropUIParent, screenPosition, uiCamera, out Vector2 localPoint))
        {
            rt.anchoredPosition = localPoint;
        }
    }

    private static string GetParentChainNames(Transform t)
    {
        var names = new System.Collections.Generic.List<string>();
        for (Transform current = t; current != null; current = current.parent)
        {
            names.Add($"{current.name}(scale={current.localScale})");
        }
        names.Reverse();
        return string.Join(" > ", names);
    }
}

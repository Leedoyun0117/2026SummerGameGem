using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Arrow,     // 이동 경로 변경용 화살표
    Attack,    // 보스 공격용 타일 (이동 멈춤 및 보스 턴 전환)
    Treasure,  // 보물상자 (아이템이 주변 빈 타일로 튀어나옴)
}

public class KTH_Tile : MonoBehaviour
{
    [Header("타일 유형 설정")]
    [SerializeField] private TileType tileType = TileType.Arrow;
    public TileType CurrentTileType => tileType;

    [Header("타일 소모 설정")]
    [Tooltip("한 번 밟으면 소모되어 사라질 타일인지 여부 (예: 스폰된 아이템 타일)")]
    [SerializeField] private bool isConsumable = false;
    public bool IsConsumable => isConsumable;

    private bool isUsed = false;
    public bool IsUsed => isUsed;

    [Header("화살표 설정 (TileType == Arrow일 때 사용)")]
    [SerializeField] private string centerName = "Center";
    [SerializeField] private bool pointOutward = true;
    [SerializeField] private float angleOffset = 0f;

    private Transform center;

    [Header("공격 타일 설정")]
    [SerializeField] private GameObject selectAttack;

    [Header("보물상자 설정 (TileType == Treasure일 때 사용)")]
    [Tooltip("보물상자 애니메이터 (비워두면 오브젝트에서 자동 가져옴)")]
    [SerializeField] private Animator treasureAnimator;
    [SerializeField] private string isOpenParamName = "isOpen";
    [SerializeField] private List<GameObject> itemPrefabs = new List<GameObject>();
    [SerializeField] private int itemSpawnCount = 3;

    public List<GameObject> ItemPrefabs => itemPrefabs;
    public int ItemSpawnCount => itemSpawnCount;

    private void Awake()
    {
        if (treasureAnimator == null)
            treasureAnimator = GetComponent<Animator>();
    }

    /// <summary>
    /// 🎁 보물상자 열림 애니메이션 재생
    /// </summary>
    public void OpenTreasureChest()
    {
        if (treasureAnimator != null)
        {
            treasureAnimator.SetBool(isOpenParamName, true);
        }
    }

    private void Start()
    {
        FindCenter();
    }

    private void Update()
    {
        if (tileType == TileType.Arrow)
        {
            UpdateRotation();
        }
    }

    private void FindCenter()
    {
        if (center != null) return;

        if (!string.IsNullOrEmpty(centerName))
        {
            GameObject centerObj = GameObject.Find(centerName);
            if (centerObj != null) center = centerObj.transform;
        }

        if (center == null && transform.parent != null)
        {
            center = transform.parent;
        }
    }

    private void UpdateRotation()
    {
        if (center == null)
        {
            FindCenter();
            if (center == null) return;
        }

        Vector2 dir = (Vector2)(transform.position - center.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        if (!pointOutward) dir = -dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    public Vector2 GetArrowDirection()
    {
        return -transform.right;
    }

    /// <summary>
    /// 아이템/타일 사용 처리 (소모성 타일일 경우 호출)
    /// </summary>
    public void UseTile()
    {
        if (isUsed) return;
        isUsed = true;

        if (isConsumable)
        {
            // 사용 후 파괴 (필드에서 제거)
            Destroy(gameObject, 0.1f);
        }
    }
}
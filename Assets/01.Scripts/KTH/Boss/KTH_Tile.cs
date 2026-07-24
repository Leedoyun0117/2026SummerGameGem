using UnityEngine;

public enum TileType
{
    Arrow,  // 이동 경로 변경용 화살표
    Attack,  // 보스 공격용 타일 (이동 멈춤 및 보스 턴 전환)
    Treasure
}

public class KTH_Tile : MonoBehaviour
{
    [Header("타일 유형 설정")]
    [SerializeField] private TileType tileType = TileType.Arrow;
    public TileType CurrentTileType => tileType;

    [Header("화살표 설정 (TileType == Arrow일 때 사용)")]
    [Tooltip("기준이 되는 중심점(원의 중앙). 비워두면 부모 오브젝트를 사용")]
    [SerializeField] private Transform center;
    [Tooltip("체크하면 중심에서 바깥쪽을 향함 / 체크 해제하면 중심(안쪽)을 향함")]
    [SerializeField] private bool pointOutward = true;
    [Tooltip("스프라이트 기본 방향 보정")]
    [SerializeField] private float angleOffset = 0f;

    [SerializeField] private GameObject selectAttack;
   
    private void Start()
    {
        if (center == null && transform.parent != null)
            center = transform.parent;
    }

    private void Update()
    {
        // 화살표 타입일 때만 링 중심 기반 회전 업데이트
        if (tileType == TileType.Arrow)
        {
            UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        if (center == null) return;

        Vector2 dir = (Vector2)(transform.position - center.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        if (!pointOutward) dir = -dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    /// <summary>
    /// 화살표가 가리키는 전방 방향 벡터 전달
    /// </summary>
    public Vector2 GetArrowDirection()
    {
        return -transform.right;
    }
}
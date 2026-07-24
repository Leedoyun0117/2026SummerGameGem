using UnityEngine;

public class KTH_Arrow : MonoBehaviour
{
    [Tooltip("기준이 되는 중심점(원의 중앙). 비워두면 부모 오브젝트를 사용")]
    [SerializeField] private Transform center;

    [Tooltip("체크하면 중심에서 바깥쪽을 향함 / 체크 해제하면 중심(안쪽)을 향함")]
    [SerializeField] private bool pointOutward = true;

    [Tooltip("스프라이트 기본 방향 보정. 기본 스프라이트가 '왼쪽(X-)'을 바라본다면 0으로 두시면 됩니다.")]
    [SerializeField] private float angleOffset = 0f;

    private void Start()
    {
        if (center == null && transform.parent != null)
            center = transform.parent;
    }

    private void Update()
    {
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        if (center == null) return;

        Vector2 dir = (Vector2)(transform.position - center.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        if (!pointOutward) dir = -dir;

        // 스프라이트가 왼쪽(-X)을 볼 때, dir 방향으로 맞추는 각도 계산
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    /// <summary>
    /// 화살표가 실제 가리키고 있는 전방 세계 방향 벡터 (스프라이트가 왼쪽일 때 -transform.right)
    /// </summary>
    public Vector2 GetArrowDirection()
    {
        return -transform.right;
    }
}

using UnityEngine;

public class KTH_Boss : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform playerTransform;

    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 플레이어 Transform을 인스펙터에서 안 넣었을 경우 자동으로 찾기
        if (playerTransform == null)
        {
            KTH_PlayerMovement player = FindFirstObjectByType<KTH_PlayerMovement>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    private void Update()
    {
        FlipToPlayer();
    }

    /// <summary>
    /// 플레이어 위치에 따라 보스 플립 처리
    /// </summary>
    private void FlipToPlayer()
    {
        if (playerTransform == null || spriteRenderer == null) return;

        // 플레이어가 보스보다 오른쪽에 있으면 flipX = false (기본 바라보는 방향이 오른쪽일 때)
        if (playerTransform.position.x > transform.position.x)
        {
            spriteRenderer.flipX = false;
        }
        // 플레이어가 보스보다 왼쪽에 있으면 flipX = true
        else if (playerTransform.position.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
    }
}
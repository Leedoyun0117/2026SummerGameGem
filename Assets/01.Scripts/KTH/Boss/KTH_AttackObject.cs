using UnityEngine;

public class KTH_AttackObject : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1.0f; // 일정 시간 뒤 자동 삭제

    private void Start()
    {
        // 일정 시간이 지나면 소환된 오브젝트를 자동으로 제거합니다.
        Destroy(gameObject, destroyDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 충돌 대상이 플레이어인지 확인 (Tag 기준)
        if (other.CompareTag("Player"))
        {
            Debug.Log("💥 [공격 오브젝트] 플레이어와 충돌함!");

            // TODO: 플레이어 데미지 처리 (TakeDamage) 들어갈 자리
            /*
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
            }
            */
        }
    }
}

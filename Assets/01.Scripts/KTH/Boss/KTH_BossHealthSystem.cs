using System;
using UnityEngine;
using UnityEngine.Events;

public class KTH_BossHealthSystem : MonoBehaviour
{
    [Header("Boss Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private string dieBoolName = "isDeath";
    [SerializeField] private float destroyDelay = 2f;

    [Header("Events (UI 및 애니메이션 연동용)")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onBossDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; } = false;

    private void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 보스에게 데미지를 가할 때 호출
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"💥 [보스 피격] 데미지 : {damage} | 남은 체력 : {currentHealth}/{maxHealth}");

        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    private void Die()
    {
        if (IsDead) return;

        IsDead = true;

        Debug.Log("☠️ [보스 사망]");

        // 죽는 애니메이션 실행
        if (animator != null)
        {
            animator.SetBool(dieBoolName, true);
        }

        // 모든 Collider 비활성화
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // Animator와 이 스크립트를 제외한 모든 스크립트 정지
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script == this)
                continue;

            if (script is Animator)
                continue;

            script.enabled = false;
        }

        // 이벤트 호출
        onBossDied?.Invoke();

        // 애니메이션 종료 후 삭제
        Destroy(gameObject, destroyDelay);
    }
}
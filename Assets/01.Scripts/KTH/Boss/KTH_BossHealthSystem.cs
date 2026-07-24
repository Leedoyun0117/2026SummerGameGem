using System;
using UnityEngine;
using UnityEngine.Events;

public class KTH_BossHealthSystem : MonoBehaviour
{

    [Header("Boss Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Events (UI 및 애니메이션 연동용)")]
    // (현재 체력, 최대 체력)을 전달하는 이벤트
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onBossDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; } = false;

    private void Start()
    {
        currentHealth = maxHealth;

        // 시작 시 UI 초기화를 위해 이벤트 호출
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 보스에게 데미지를 가할 때 호출하는 함수
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"💥 [보스 피격] 데미지: {damage} | 남은 체력: {currentHealth}/{maxHealth}");

        // 체력 변경 이벤트 호출 (UI 업데이트용)
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력 회복 함수 (필요 시 사용)
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
        Debug.Log("☠️ [보스 사망] 보스가 처치되었습니다!");

        // 사망 이벤트 호출 (애니메이션, 턴 매니저 처리, 게임 승리 연출 등)
        onBossDied?.Invoke();

        // TODO: 필요 시 보스 오브젝트 비활성화 또는 파괴 로직 추가
        // Destroy(gameObject, 2.0f);
    }
}


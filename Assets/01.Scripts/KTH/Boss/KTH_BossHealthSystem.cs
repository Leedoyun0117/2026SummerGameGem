using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;
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

    [Header("HP Bar")]
    [SerializeField] private Image hpBar;
    [SerializeField] private float hpBarTweenTime = 0.3f;

    private Tween hpTween;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; } = false;

    private void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (hpBar != null)
            hpBar.fillAmount = 1f;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 보스에게 데미지를 가할 때 호출
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        float ratio = currentHealth / maxHealth;

        if (hpBar != null)
        {
            hpTween?.Kill();
            hpTween = hpBar
                .DOFillAmount(ratio, hpBarTweenTime)
                .SetEase(Ease.OutQuad);
        }

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

        float ratio = currentHealth / maxHealth;

        if (hpBar != null)
        {
            hpTween?.Kill();
            hpTween = hpBar
                .DOFillAmount(ratio, hpBarTweenTime)
                .SetEase(Ease.OutQuad);
        }

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    private void Die()
    {
        if (IsDead) return;

        IsDead = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.Death);
        }

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

        SceneManager.LoadScene("End Scene");

        // 애니메이션 종료 후 삭제
        Destroy(gameObject, destroyDelay);
    }
}
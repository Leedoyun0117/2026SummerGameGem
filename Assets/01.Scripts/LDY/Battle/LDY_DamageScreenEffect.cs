using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어가 데미지를 입을 때(체력 감소) 화면 연출: 카메라가 짧게 흔들리고, 플레이어 이미지 자체가 잠깐
// 빨갛게 물들었다가 원래 색으로 돌아온다. KTH_PlayerHealth.OnHealthChanged를 구독해서 체력이
// "줄어들 때만" 재생한다(회복/초기 동기화 시에는 재생 안 함 - LDY_PlayerHealthUI의 하트 흔들림과 같은 판단 방식).
public class LDY_DamageScreenEffect : MonoBehaviour
{
    [SerializeField] private KTH_PlayerHealth playerHealth;

    [Header("카메라 흔들림")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.3f;

    [Header("빨갛게 물드는 대상 - UI 이미지면 playerImageUI, 월드 스프라이트면 playerSpriteRenderer에 연결. " +
        "둘 다 비워두면 씬의 LDY_PlayerHealthUI 하트 이미지를 대신 씀")]
    [SerializeField] private Image playerImageUI;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashInDuration = 0.05f;
    [SerializeField] private float flashOutDuration = 0.3f;

    private int lastKnownHp = -1;
    private Color originalColor;
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;

    private void Start()
    {
        if (playerHealth == null) playerHealth = KTH_PlayerHealth.Instance;
        if (targetCamera == null) targetCamera = Camera.main;

        if (playerImageUI == null && playerSpriteRenderer == null)
        {
            LDY_PlayerHealthUI healthUI = FindFirstObjectByType<LDY_PlayerHealthUI>();
            if (healthUI != null) playerImageUI = healthUI.HeartImage;
        }

        if (playerImageUI != null) originalColor = playerImageUI.color;
        else if (playerSpriteRenderer != null) originalColor = playerSpriteRenderer.color;

        if (playerHealth != null)
        {
            lastKnownHp = playerHealth.CurrentHP;
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (lastKnownHp >= 0 && current < lastKnownHp)
        {
            PlayShake();
            PlayFlash();
        }
        lastKnownHp = current;
    }

    private void PlayShake()
    {
        if (targetCamera == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        // 다른 곳에서 카메라를 옮길 수도 있으니, 흔들기 시작하는 그 순간의 위치를 기준으로 삼는다
        // (Start에서 한 번 캐싱해두면 흔들림이 끝난 뒤 옛날 위치로 스냅되는 문제가 생길 수 있음).
        Vector3 basePos = targetCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeStrength;
            targetCamera.transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        targetCamera.transform.localPosition = basePos;
    }

    private void PlayFlash()
    {
        if (playerImageUI == null && playerSpriteRenderer == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return StartCoroutine(LerpTint(originalColor, flashColor, flashInDuration));
        yield return StartCoroutine(LerpTint(flashColor, originalColor, flashOutDuration));
    }

    private IEnumerator LerpTint(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetTint(Color.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetTint(to);
    }

    private void SetTint(Color c)
    {
        if (playerImageUI != null) playerImageUI.color = c;
        else if (playerSpriteRenderer != null) playerSpriteRenderer.color = c;
    }
}

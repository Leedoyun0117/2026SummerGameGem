using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// KTH_PlayerHealth의 체력 값을 "현재/최대"(예: 50/100) 형식으로 텍스트에 표시하고,
// 하트 이미지 하나를 체력 비율에 맞는 정해진 스프라이트로 바꿔준다. 데미지를 입으면(체력이 줄어들면)
// 하트가 잠깐 흔들린다.
// KTH_PlayerHealth.OnHealthChanged를 구독해서 값이 바뀔 때만 갱신하고, 매 프레임 폴링하지 않는다.
public class LDY_PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private KTH_PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("하트 이미지 - 체력이 1/n(heartSprites 개수분의 1)씩 닳을 때마다 다음 모양으로 바뀜")]
    [SerializeField] private Image heartImage;
    [SerializeField] private Sprite[] heartSprites; // 0번 = 가득 찬 상태, 마지막 = 빈 상태 순서로 채워 넣기

    [Header("데미지를 입을 때(체력 감소) 하트 흔들림 효과")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 8f;

    private int lastKnownHp = -1;
    private Vector2 heartOriginalPos;
    private Coroutine shakeRoutine;

    private void Start()
    {
        if (heartImage != null) heartOriginalPos = heartImage.rectTransform.anchoredPosition;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(playerHealth.CurrentHP, playerHealth.MaxHP);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (hpText != null) hpText.text = $"{current}/{max}";
        UpdateHeart(current, max);

        // 체력이 줄어든 경우(데미지)에만 흔들고, 초기 동기화나 회복 시에는 흔들지 않는다.
        if (lastKnownHp >= 0 && current < lastKnownHp) ShakeHeart();
        lastKnownHp = current;
    }

    // 체력 비율을 heartSprites 개수만큼의 단계로 나눠서, 몇 칸이 닳았는지에 맞는 스프라이트를 고른다.
    private void UpdateHeart(int current, int max)
    {
        if (heartImage == null || heartSprites == null || heartSprites.Length == 0) return;

        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        int stageCount = heartSprites.Length;
        int stageIndex = Mathf.FloorToInt((1f - ratio) * stageCount);
        stageIndex = Mathf.Clamp(stageIndex, 0, stageCount - 1);

        heartImage.sprite = heartSprites[stageIndex];
    }

    private void ShakeHeart()
    {
        if (heartImage == null) return;

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        RectTransform rt = heartImage.rectTransform;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * shakeStrength;
            rt.anchoredPosition = heartOriginalPos + offset;
            yield return null;
        }

        rt.anchoredPosition = heartOriginalPos;
    }
}

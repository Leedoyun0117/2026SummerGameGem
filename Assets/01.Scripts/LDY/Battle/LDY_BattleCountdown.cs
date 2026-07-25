using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// "3, 2, 1, START!" 카운트다운 재생만 담당하는 컴포넌트.
// 맵에서 만들어져 DontDestroyOnLoad로 넘어가는 LDY_SceneTransition이 "씬이 이미 보이는 상태에서" Play()를
// 호출해서 씀 - 그래서 전투/보스 씬마다 따로 만들 필요 없이 맵에서 한 번만 소환되면 됨.
// dimBackground는 이 컴포넌트 자체가 들고 있는 반투명 배경(+입력 차단)으로, 아이리스와 별개로 동작함
public class LDY_BattleCountdown : MonoBehaviour
{
    [SerializeField] private GameObject dimBackground; // 반투명 배경(Raycast Target으로 입력도 막음), 평소엔 꺼둠
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float secondsPerNumber = 1f;
    [SerializeField] private float startHoldDuration = 0.6f;
    [SerializeField] private AudioClip _matchBGM;

    public void Play(Action onComplete)
    {
        if (dimBackground != null) dimBackground.SetActive(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.CountdownStart);
        }

        if (_matchBGM != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(_matchBGM);
            Debug.Log("BGM 바뀜");
        }


        StartCoroutine(CountdownRoutine(onComplete));
    }

    private IEnumerator CountdownRoutine(Action onComplete)
    {
        yield return ShowNumber("3", secondsPerNumber);
        yield return ShowNumber("2", secondsPerNumber);
        yield return ShowNumber("1", secondsPerNumber);
        yield return ShowNumber("START!", startHoldDuration);

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator ShowNumber(string text, float duration)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = text;
            yield return Punch(countdownText.rectTransform);
        }

        yield return new WaitForSecondsRealtime(duration);
    }

    // 숫자가 크게 튀어나왔다가 제자리로 줄어드는 펀치 효과
    private IEnumerator Punch(RectTransform rt)
    {
        const float punchDuration = 0.18f;
        float t = 0f;

        while (t < punchDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchDuration);
            rt.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k);
            yield return null;
        }

        rt.localScale = Vector3.one;
    }
}

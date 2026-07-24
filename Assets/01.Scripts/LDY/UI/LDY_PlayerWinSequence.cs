using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 보드의 모든 적을 처치하면(LDY_AttackTargetController.OnAllEnemiesDefeated) "Win" 텍스트를 페이드인
// 시키고, 잠깐 뒤 맵 진행을 완료 처리한 다음 지정한 씬(보통 맵)으로 돌아간다.
public class LDY_PlayerWinSequence : MonoBehaviour
{
    [Tooltip("Win 캔버스를 씬에 기본 비활성화 상태로 놔두고 싶으면 그 캔버스(또는 최상위) GameObject를 연결 - " +
        "승리 시 자동으로 켜준다. 비워두면 winGroup이 붙은 오브젝트 자신을 켬")]
    [SerializeField] private GameObject winCanvasRoot;
    [SerializeField] private CanvasGroup winGroup;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private float winFadeDuration = 0.5f;
    [SerializeField] private float winHoldDuration = 1.5f;
    [SerializeField] private string destinationSceneName = "LDY_MapScene";

    private bool triggered;

    private void Start()
    {
        if (LDY_AttackTargetController.Instance != null)
            LDY_AttackTargetController.Instance.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;

        if (winGroup != null) winGroup.alpha = 0f;

        if (winCanvasRoot != null) winCanvasRoot.SetActive(false);
        else if (winGroup != null) winGroup.gameObject.SetActive(false);

        if (winText != null) winText.text = "Win";
    }

    private void OnDestroy()
    {
        if (LDY_AttackTargetController.Instance != null)
            LDY_AttackTargetController.Instance.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    private void HandleAllEnemiesDefeated()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        if (winCanvasRoot != null) winCanvasRoot.SetActive(true);
        else if (winGroup != null) winGroup.gameObject.SetActive(true);

        if (winGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(winGroup, 0f, 1f, winFadeDuration));

        yield return new WaitForSecondsRealtime(winHoldDuration);

        // 맵으로 돌아왔을 때 이번에 들어왔던 노드가 클리어 처리되고 다음 노드가 열리도록 여기서 완료 처리한다.
        if (LDY_MapManager.Instance != null) LDY_MapManager.Instance.CompleteActiveNode();

        SceneManager.LoadScene(destinationSceneName);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }
}

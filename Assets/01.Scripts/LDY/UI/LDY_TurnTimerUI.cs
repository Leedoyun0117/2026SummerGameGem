using TMPro;
using UnityEngine;

// LDY_BattleTurnManager의 남은 시간을 "20.00" 형식으로 표시하고, 일정 시간 이하로 남으면 텍스트 색이
// 노란색 -> 빨간색으로 바뀐다. 옆의 회전하는 시계 아이콘은 별도로 만들어둔 애니메이션인데, 시간이
// 0이 되면(턴 종료) Animator.speed를 0으로 만들어서 그 자리에서 멈추고, 다음 턴이 시작되면 다시 돌아간다.
public class LDY_TurnTimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("이 값(초) 이하로 남으면 색이 바뀐다")]
    [SerializeField] private float yellowThreshold = 10f;
    [SerializeField] private float redThreshold = 3f;

    [Header("단계별 텍스트 색")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    [Header("1초에 한 바퀴 도는 시계 아이콘 애니메이터 - 시간이 0이 되면 멈춘다")]
    [SerializeField] private Animator clockAnimator;

    private void Update()
    {
        if (LDY_BattleTurnManager.Instance == null) return;

        float remaining = Mathf.Max(LDY_BattleTurnManager.Instance.TimeRemaining, 0f);

        if (timerText != null)
        {
            timerText.text = remaining.ToString("F2");

            if (remaining <= redThreshold) timerText.color = redColor;
            else if (remaining <= yellowThreshold) timerText.color = yellowColor;
            else timerText.color = normalColor;
        }

        if (clockAnimator != null) clockAnimator.speed = remaining > 0f ? 1f : 0f;
    }
}

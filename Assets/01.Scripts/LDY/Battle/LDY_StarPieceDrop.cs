using System.Collections;
using UnityEngine;

// StarPiece 조각 하나의 연출: 떨어진 자리에서 살짝 튀어올랐다가(bounce) dropDuration 동안 가만히
// 있다가, 목적지(flyTarget - 없으면 부모 기준 anchoredPosition (0,0))로 flyDuration 동안 날아가면서
// 점점 작아지다 사라진다("골드가 UI로 빨려들어가는" 느낌). 도착하면 LDY_StarPieceManager로 재화가
// 더해지고 UI 텍스트도 갱신된다.
// Screen Space Overlay UI 기준 - LDY_StarPieceManager.SpawnDrops가 시작 위치를 스크린 좌표로 잡아준다.
[RequireComponent(typeof(RectTransform))]
public class LDY_StarPieceDrop : MonoBehaviour
{
    [SerializeField] private float dropDuration = 1.5f; // 떨어진 채로 대기하는 시간(살짝 튀어오르는 시간 포함)
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float bounceHeight = 20f;
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private AnimationCurve flyEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rt;
    private RectTransform flyTarget;
    private int amount;

    // flyTarget이 있으면(예: 화면의 플레이어 위치/재화 카운터 아이콘) 그 위치로, 없으면 부모 기준
    // (0,0)으로 날아간다 - 씬마다(맵/전투) 목적지가 다를 수 있어서 매번 넘겨받는다.
    public void Init(RectTransform target, int starPieceAmount)
    {
        flyTarget = target;
        amount = starPieceAmount;
        rt = GetComponent<RectTransform>();
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1) 살짝 위로 튀어올랐다가 떨어짐
        Vector2 restPos = rt.anchoredPosition;
        Vector2 peak = restPos + Vector2.up * bounceHeight;

        float t = 0f;
        while (t < bounceDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Sin(Mathf.Clamp01(t / bounceDuration) * Mathf.PI);
            rt.anchoredPosition = Vector2.Lerp(restPos, peak, p);
            yield return null;
        }
        rt.anchoredPosition = restPos;

        // 2) 목적지로 날아갈 때까지 그 자리에서 대기
        float remainingWait = dropDuration - bounceDuration;
        if (remainingWait > 0f) yield return new WaitForSeconds(remainingWait);

        // 3) 목적지로 날아가며 점점 작아짐 (flyTarget이 있으면 그 위치, 없으면 부모 기준 (0,0))
        Vector2 flyStart = rt.anchoredPosition;
        Vector2 flyEnd = flyTarget != null ? flyTarget.anchoredPosition : Vector2.zero;

        t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float p = flyEase.Evaluate(Mathf.Clamp01(t / flyDuration));
            rt.anchoredPosition = Vector2.Lerp(flyStart, flyEnd, p);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, p);
            yield return null;
        }

        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.NotifyPieceCollected(amount);
        Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;

// 흡수 이펙트 - 바깥쪽에 흩어진 짧은 선들이 이 오브젝트의 중심으로 빨려들어가듯 모여들면서 점점
// 작아지다 사라진다("에너지를 빨아들이는" 느낌). 면역(반사) 적이 공격을 흡수하는 순간, 그 적 위치에
// 재생하면 된다 - 그 다음에 폭발/반사 이펙트를 다른 곳(예: 플레이어 위치)에서 "방출"시키면
// "흡수하고 방출한다"는 한 세트의 연출이 완성된다.
// 별도 프리팹 설정 없이 빈 GameObject에 이 스크립트 하나만 붙여서 저장하면 바로 동작한다.
public class LDY_AbsorbEffect : MonoBehaviour
{
    [SerializeField] private int lineCount = 12;
    [SerializeField] private float startRadius = 0.8f;
    [SerializeField] private float lineLength = 0.18f;
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Color color = new Color(0.6f, 0.2f, 0.85f, 1f); // 보라색 - 흡수하는 느낌

    private void Awake()
    {
        for (int i = 0; i < lineCount; i++)
        {
            float angleDeg = (360f / lineCount) * i + Random.Range(-10f, 10f);
            StartCoroutine(AnimateLine(angleDeg));
        }

        Destroy(gameObject, duration + 0.15f);
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(다른 이펙트들과 동일한 방식).
    private IEnumerator AnimateLine(float angleDeg)
    {
        GameObject go = new GameObject("AbsorbLine");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.widthMultiplier = lineWidth;
        line.positionCount = 2;
        line.numCapVertices = 2;
        line.sortingOrder = 14;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        line.material = new Material(shader);
        line.startColor = color;
        line.endColor = color;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        // 시작 시점을 살짝씩 어긋나게 해서 한꺼번에 빨려들어가지 않고 순차적으로 빨려드는 느낌을 준다.
        float startDelay = Random.Range(0f, duration * 0.3f);
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        float travelDuration = duration - startDelay;
        float t = 0f;
        while (t < travelDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / travelDuration);
            float r = Mathf.Lerp(startRadius, 0f, p);

            line.SetPosition(0, dir * (r + lineLength));
            line.SetPosition(1, dir * r);

            Color c = color;
            c.a = color.a * (1f - p * 0.4f);
            line.startColor = c;
            line.endColor = c;

            yield return null;
        }

        Destroy(go);
    }
}

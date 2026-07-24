using System.Collections;
using UnityEngine;

// 전기 지지직 효과. 정해진 반경 안에서 짧고 삐죽삐죽한 선들이 매 프레임(flickerInterval마다) 랜덤한
// 위치/색으로 다시 그려져서 깜빡이는 것처럼 보인다. 색은 colorA~colorB 사이를 오간다(기본 주황~노랑).
// LDY_Enemy가 적 몸 위에 자식으로 만들어서 붙이고 Init()으로 크기/색/지속시간을 넘겨준다.
public class LDY_CrackleEffect : MonoBehaviour
{
    [SerializeField] private int lineCount = 6;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float flickerInterval = 0.04f;

    private Color colorA = new Color(1f, 0.5f, 0f, 1f);
    private Color colorB = new Color(1f, 0.95f, 0.2f, 1f);
    private float radius = 0.5f;
    private float duration = 0.3f;
    private LineRenderer[] lines;

    public void Init(float bodySize, Color a, Color b, float effectDuration)
    {
        radius = Mathf.Max(bodySize * 0.5f, 0.1f);
        colorA = a;
        colorB = b;
        duration = effectDuration;

        lines = new LineRenderer[lineCount];
        for (int i = 0; i < lineCount; i++) lines[i] = BuildLine();

        StartCoroutine(FlickerRoutine());
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(다른 이펙트들과 동일한 방식).
    private LineRenderer BuildLine()
    {
        GameObject go = new GameObject("CrackleLine");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.widthMultiplier = lineWidth;
        line.numCapVertices = 2;
        line.sortingOrder = 15;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        line.material = new Material(shader);

        return line;
    }

    private IEnumerator FlickerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (LineRenderer line in lines) RedrawLine(line);
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        Destroy(gameObject);
    }

    private void RedrawLine(LineRenderer line)
    {
        Vector3 center = (Vector3)(Random.insideUnitCircle * radius);
        Vector3 dir = (Vector3)(Random.insideUnitCircle.normalized * (radius * 0.6f));

        const int segments = 4;
        Vector3 start = center - dir;
        Vector3 end = center + dir;

        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            if (i != 0 && i != segments - 1)
            {
                point += (Vector3)(Random.insideUnitCircle * lineWidth * 3f);
            }
            line.SetPosition(i, point);
        }

        Color c = Color.Lerp(colorA, colorB, Random.value);
        line.startColor = c;
        line.endColor = c;
    }

    // Material은 인스턴스별로 새로 만들어졌으니 오브젝트가 파괴될 때 같이 정리한다.
    private void OnDestroy()
    {
        if (lines == null) return;
        foreach (LineRenderer line in lines)
        {
            if (line != null && line.material != null) Destroy(line.material);
        }
    }
}

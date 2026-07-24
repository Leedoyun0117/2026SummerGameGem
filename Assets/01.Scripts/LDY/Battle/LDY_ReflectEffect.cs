using System.Collections;
using TMPro;
using UnityEngine;

// 반사(WeaponReflector) 이펙트. LDY_AttackTargetController가 반사가 일어난 적의 위치에 이 프리팹을
// Instantiate하고 TargetPosition에 발사 위치(플레이어 쪽)를 넣어준다.
// 연출: 1) 적 위치에서 별빛(작은 반짝임)이 위로 짤랑 튀어오르며 사라짐
//      2) 동시에 레이저가 적 -> 플레이어 방향으로 짧은 시간(travelDuration) 안에 빠르게 뻗어나간다.
// 별도 프리팹 설정 없이 빈 GameObject에 이 스크립트 하나만 붙여서 저장하면 바로 동작한다.
public class LDY_ReflectEffect : MonoBehaviour, ILDY_EffectTarget
{
    public Vector3 TargetPosition { get; set; }

    [Header("별빛(스파클)")]
    [SerializeField] private int starCount = 8;
    [SerializeField] private float starSpread = 0.35f;
    [SerializeField] private float starRiseHeight = 0.5f;
    [SerializeField] private float starLifetime = 0.3f;
    [SerializeField] private float starSize = 0.18f;
    [SerializeField] private Color starColor = new Color(1f, 1f, 0.6f, 1f);

    [Header("적 -> 플레이어로 빠르게 날아오는 레이저 (붉은빛 - 반사되어 나를 위협, 우리 레이저와 같은 코어+글로우 스타일)")]
    [SerializeField] private Color coreColor = new Color(1f, 0.55f, 0.4f, 1f);
    [SerializeField] private Color glowColor = new Color(1f, 0.25f, 0.15f, 0.45f);
    [SerializeField] private float coreWidth = 0.08f;
    [SerializeField] private float glowWidth = 0.3f;
    [SerializeField] private float travelDuration = 0.15f; // 이 시간 안에 적 위치 -> 플레이어까지 다 뻗어나간다(빠르게)

    [Header("전기처럼 일렁이는 효과 (우리 레이저와 동일)")]
    [SerializeField] private int segmentCount = 14;
    [SerializeField] private float jitterAmount = 0.12f;
    [SerializeField] private float jitterSpeed = 30f;

    [Header("\"반사!\" 텍스트 팝업 - 레이저/별빛이랑 겹쳐서 안 보이지 않게 옆으로 띄운다")]
    [SerializeField] private string reflectText = "반사!";
    [SerializeField] private Color reflectTextColor = new Color(0.95f, 0.1f, 0.1f, 1f);
    [SerializeField] private float reflectTextSize = 4f;
    [SerializeField] private Vector3 reflectTextOffset = new Vector3(0.9f, 0.3f, 0f);
    [SerializeField] private float reflectTextRiseHeight = 0.6f;
    [SerializeField] private float reflectTextLifetime = 0.6f;

    private LineRenderer coreLine;
    private LineRenderer glowLine;
    private float elapsed;

    private void Awake()
    {
        for (int i = 0; i < starCount; i++) SpawnStar();
        glowLine = BuildLine("ReflectLaserGlow", glowColor, glowWidth, 9);
        coreLine = BuildLine("ReflectLaserCore", coreColor, coreWidth, 10);
        SpawnReflectText();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = travelDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / travelDuration);
        Vector3 tip = Vector3.Lerp(transform.position, TargetPosition, t);

        DrawJitteredLine(glowLine, tip);
        DrawJitteredLine(coreLine, tip);
    }

    // 우리 레이저(LDY_HitEffect)와 같은 방식 - 시작점(적 위치)/끝점(현재 뻗어나간 지점)은 고정하고
    // 중간 지점들만 Perlin 노이즈로 흔들어서 전기가 일렁이는 것처럼 보이게 한다.
    private void DrawJitteredLine(LineRenderer line, Vector3 tip)
    {
        Vector3 start = transform.position;
        Vector3 dir = tip - start;
        Vector3 normal = new Vector3(-dir.y, dir.x, 0f).normalized;

        line.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = segmentCount <= 1 ? 0f : (float)i / (segmentCount - 1);
            Vector3 point = Vector3.Lerp(start, tip, t);

            if (i != 0 && i != segmentCount - 1)
            {
                float noise = (Mathf.PerlinNoise(t * 5f, Time.time * jitterSpeed) - 0.5f) * 2f * jitterAmount;
                point += normal * noise;
            }

            line.SetPosition(i, point);
        }
    }

    private void SpawnStar()
    {
        LineRenderer line = BuildLine("Star", starColor, 0.03f, 12);
        line.loop = true;

        Vector2 offset = Random.insideUnitCircle * starSpread;
        Vector3 startPos = transform.position + new Vector3(offset.x, Mathf.Abs(offset.y), 0f);

        StartCoroutine(AnimateStar(line, startPos));
    }

    private IEnumerator AnimateStar(LineRenderer line, Vector3 startPos)
    {
        Vector3 endPos = startPos + Vector3.up * starRiseHeight;
        float t = 0f;

        while (t < starLifetime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / starLifetime);

            Vector3 center = Vector3.Lerp(startPos, endPos, p);
            float size = starSize * (1f - p * 0.6f);
            DrawDiamond(line, center, size);

            Color c = starColor;
            c.a = starColor.a * (1f - p);
            line.startColor = c;
            line.endColor = c;

            yield return null;
        }

        if (line != null) Destroy(line.gameObject);
    }

    private void DrawDiamond(LineRenderer line, Vector3 center, float size)
    {
        line.positionCount = 4;
        line.SetPosition(0, center + new Vector3(0f, size, 0f));
        line.SetPosition(1, center + new Vector3(size, 0f, 0f));
        line.SetPosition(2, center + new Vector3(0f, -size, 0f));
        line.SetPosition(3, center + new Vector3(-size, 0f, 0f));
    }

    // 적 위치 위에 "반사!" 텍스트가 팝(살짝 튀어오르며 커짐) 하고, 위로 떠오르면서 옅어지다 사라진다.
    // Canvas UI가 아니라 월드 스페이스 TextMeshPro라서 별도 UI 연결 없이 이 이펙트 안에서 바로 뜬다.
    private void SpawnReflectText()
    {
        GameObject go = new GameObject("ReflectText");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = reflectTextOffset;
        go.transform.localScale = Vector3.zero;

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = reflectText;
        text.color = reflectTextColor;
        text.fontSize = reflectTextSize;
        text.alignment = TextAlignmentOptions.Center;

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.sortingOrder = 20;

        // 월드스페이스 TextMeshPro는 정면이 카메라를 보고 있지 않으면(뒷면 컬링) 아예 안 보일 수 있어서
        // 카메라 방향을 그대로 따라가게 고정한다(이 프로젝트는 탑뷰 고정 카메라라 매 프레임 갱신할 필요는 없음).
        if (Camera.main != null) go.transform.rotation = Camera.main.transform.rotation;

        StartCoroutine(AnimateReflectText(go.transform, text));
    }

    private IEnumerator AnimateReflectText(Transform textTransform, TextMeshPro text)
    {
        // 팝인: 순간적으로 확 커졌다가 원래 크기로 정착.
        const float popDuration = 0.12f;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            textTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, p);
            yield return null;
        }
        textTransform.localScale = Vector3.one;

        Vector3 startPos = textTransform.position;
        Vector3 endPos = startPos + Vector3.up * reflectTextRiseHeight;
        Color startColor = text.color;

        t = 0f;
        while (t < reflectTextLifetime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / reflectTextLifetime);

            textTransform.position = Vector3.Lerp(startPos, endPos, p);

            Color c = startColor;
            c.a = startColor.a * (1f - p);
            text.color = c;

            yield return null;
        }

        if (textTransform != null) Destroy(textTransform.gameObject);
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(LDY_HitEffect와 동일한 방식).
    private LineRenderer BuildLine(string name, Color color, float width, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.sortingOrder = sortingOrder;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);

        line.material = material;
        line.startColor = color;
        line.endColor = color;

        return line;
    }
}

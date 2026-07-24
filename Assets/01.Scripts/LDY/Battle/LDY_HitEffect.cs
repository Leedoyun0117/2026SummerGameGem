using UnityEngine;

// 무기 적중 이펙트(레이저) 프리팹의 루트에 붙이는 컴포넌트.
// LDY_AttackTargetController가 이 오브젝트를 effectOrigin 위치에 Instantiate한 직후 TargetPosition에
// 맞은 적의 월드 좌표를 넣어준다. Awake에서 스스로 LineRenderer 두 개(밝은 코어 + 은은한 글로우)를 만들고,
// 매 프레임 자기 위치(발사 위치) -> TargetPosition(적 위치)까지 노란색 레이저를 그리는데, 중간 지점들을
// Perlin 노이즈로 살짝 흔들어서 전기가 일렁이는 것처럼 보이게 한다.
// 별도 프리팹 설정 없이 빈 GameObject에 이 스크립트 하나만 붙여서 저장하면 바로 동작한다.
public class LDY_HitEffect : MonoBehaviour, ILDY_EffectTarget
{
    public Vector3 TargetPosition { get; set; }

    [Header("레이저 색/두께")]
    [SerializeField] private Color coreColor = new Color(1f, 0.95f, 0.55f, 1f);   // 밝은 노란 코어
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f, 0.45f); // 은은하게 번지는 바깥 글로우
    [SerializeField] private float coreWidth = 0.08f;
    [SerializeField] private float glowWidth = 0.3f;

    [Header("전기처럼 일렁이는 효과")]
    [SerializeField] private int segmentCount = 14;   // 많을수록 더 잘게 지글거림
    [SerializeField] private float jitterAmount = 0.12f;
    [SerializeField] private float jitterSpeed = 30f;

    private LineRenderer coreLine;
    private LineRenderer glowLine;

    private void Awake()
    {
        glowLine = BuildLine("LaserGlow", glowColor, glowWidth, 9);
        coreLine = BuildLine("LaserCore", coreColor, coreWidth, 10);
    }

    private void Update()
    {
        DrawJitteredLine(glowLine);
        DrawJitteredLine(coreLine);
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(LDY_AttackTargetController 하이라이트와 동일한 방식).
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

    private void DrawJitteredLine(LineRenderer line)
    {
        if (line == null) return;

        Vector3 start = transform.position;
        Vector3 end = TargetPosition;
        Vector3 dir = end - start;
        Vector3 normal = new Vector3(-dir.y, dir.x, 0f).normalized;

        line.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            float t = segmentCount <= 1 ? 0f : (float)i / (segmentCount - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            // 시작점/끝점은 발사 위치·적 위치에 정확히 붙어야 하니 고정하고, 중간 지점들만 흔든다.
            if (i != 0 && i != segmentCount - 1)
            {
                float noise = (Mathf.PerlinNoise(t * 5f, Time.time * jitterSpeed) - 0.5f) * 2f * jitterAmount;
                point += normal * noise;
            }

            line.SetPosition(i, point);
        }
    }
}

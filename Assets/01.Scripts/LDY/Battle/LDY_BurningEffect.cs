using System.Collections;
using UnityEngine;

// 몹이 불타고 있는 것처럼 보이게 하는 순수 시각 효과(데미지 로직 없음). 스프라이트 "맨 위 가장자리"를
// 기준으로 여러 개의 불꽃 혀(LineRenderer)가 위로 솟아오르며 Perlin 노이즈로 좌우로 흔들리고 높이도
// 출렁인다. 크기 관련 수치(minHeightRatio/maxHeightRatio/flameWidthRatio/swayAmountRatio)는 전부
// 스프라이트 크기에 대한 비율이라, 작은 적이든 큰 보스든 프리팹마다 따로 맞출 필요 없이 자동으로 스케일된다.
// 항상 월드 좌표(useWorldSpace=true)로 그린다 - 부모 트랜스폼에 스케일이 걸려 있으면(예: 작은 공
// 모양 적) 로컬 좌표가 그 스케일만큼 같이 줄어들어 위치/크기가 어긋나기 때문에, 스프라이트의 실제
// 월드 바운드를 매 프레임 직접 읽어서 위치를 계산한다.
// 색은 이 프로젝트의 다른 이펙트들(LDY_HitEffect 등)과 동일하게 재질의 _BaseColor로 낸다 - URP
// Unlit 셰이더는 LineRenderer의 정점 색(startColor/endColor/colorGradient)을 무시하고 재질 자체의
// _BaseColor만 반영하기 때문에, 라인 하나는 한 순간엔 단색이고, 그 단색을 매 프레임 뿌리(노랑)->중간
// (주황)->끝(빨강) 사이에서 그 불꽃의 현재 높이에 맞춰 바꿔서 여러 불꽃이 서로 다른 색으로 보이게 한다.
// 끝이 가늘어지는 건 색이 아니라 widthCurve(폭)로 표현한다.
// 추가로 몸통 스프라이트 자체도 은은하게 붉게 달아오르는 깜빡임을 준다.
// LDY_CrackleEffect처럼 적 몸 위에 자식으로 붙여서(또는 적 루트에 직접 붙여서) 쓰면 되고,
// StartBurning()/StopBurning()으로 언제든 켜고 끌 수 있다(기본은 Awake에서 자동 시작).
// 별도 프리팹 설정 없이 빈 GameObject에 이 스크립트 하나만 붙여서 저장하면 바로 동작한다.
public class LDY_BurningEffect : MonoBehaviour
{
    [SerializeField] private bool playOnAwake = true;

    [Header("불꽃 개수 / 뿌리 퍼짐(스프라이트 폭 비율)")]
    [SerializeField] private int flameCount = 5;
    [SerializeField] private int segmentsPerFlame = 6;
    [SerializeField] private float rootSpreadRatio = 0.6f;

    [Header("불꽃 두께/높이 (스프라이트 크기에 대한 비율)")]
    [SerializeField] private float flameWidthRatio = 0.16f;
    [SerializeField] private float minHeightRatio = 0.6f;
    [SerializeField] private float maxHeightRatio = 1.1f;
    [SerializeField] private float swayAmountRatio = 0.14f;

    [Header("불꽃 흔들림/출렁임 속도")]
    [SerializeField] private float swaySpeed = 2.5f;
    [SerializeField] private float flickerSpeed = 4f; // 높이가 출렁이는 속도

    [Header("불꽃 색 (뿌리 -> 중간 -> 끝, 불꽃 높이에 따라 이 사이에서 색이 바뀜)")]
    [SerializeField] private Color rootColor = new Color(1f, 0.95f, 0.4f, 1f);
    [SerializeField] private Color midColor = new Color(1f, 0.5f, 0.05f, 1f);
    [SerializeField] private Color tipColor = new Color(0.9f, 0.15f, 0.05f, 1f);

    [Header("몸체 스프라이트가 달아오르는 깜빡임")]
    [SerializeField] private bool tintBody = true;
    [SerializeField] private Color emberTint = new Color(1f, 0.35f, 0.1f, 1f);
    [SerializeField] private float tintFlickerSpeed = 6f;
    [SerializeField] private float tintFlickerStrength = 0.35f;

    private LineRenderer[] flames;
    private Material[] flameMaterials;
    private float[] flameSeeds;
    private float bodyWidth = 0.6f;
    private float bodyHeight = 0.6f;
    private bool burning;

    private float sizeMultiplier = 1f;
    private Coroutine flareRoutine;

    private SpriteRenderer bodySprite;
    private Color bodyOriginalColor;
    private float tintSeed;

    private void Awake()
    {
        bodySprite = GetComponentInParent<SpriteRenderer>();
        if (bodySprite != null) bodyOriginalColor = bodySprite.color;
        tintSeed = Random.value * 100f;

        if (playOnAwake) StartBurning();
    }

    public void StartBurning()
    {
        if (bodySprite != null)
        {
            bodyWidth = bodySprite.bounds.size.x;
            bodyHeight = bodySprite.bounds.size.y;
        }

        if (flames == null) BuildFlames();
        foreach (LineRenderer line in flames)
        {
            if (line != null) line.gameObject.SetActive(true);
        }

        burning = true;
    }

    public void StopBurning()
    {
        burning = false;

        if (flames != null)
        {
            foreach (LineRenderer line in flames)
            {
                if (line != null) line.gameObject.SetActive(false);
            }
        }

        if (bodySprite != null) bodySprite.color = bodyOriginalColor;
    }

    // 불꽃을 targetMultiplier배 크기로 rampDuration 동안 키웠다가, holdDuration만큼 유지한 뒤,
    // 다시 rampDuration 동안 원래 크기(1배)로 줄인다. 파편 공격 등 특정 순간에 연출을 강조할 때 쓴다.
    public void FlareUp(float targetMultiplier, float rampDuration, float holdDuration)
    {
        if (flareRoutine != null) StopCoroutine(flareRoutine);
        flareRoutine = StartCoroutine(FlareRoutine(targetMultiplier, rampDuration, holdDuration));
    }

    private IEnumerator FlareRoutine(float targetMultiplier, float rampDuration, float holdDuration)
    {
        yield return AnimateSizeMultiplier(targetMultiplier, rampDuration);
        if (holdDuration > 0f) yield return new WaitForSeconds(holdDuration);
        yield return AnimateSizeMultiplier(1f, rampDuration);
    }

    private IEnumerator AnimateSizeMultiplier(float to, float duration)
    {
        float from = sizeMultiplier;
        if (duration <= 0f)
        {
            sizeMultiplier = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            sizeMultiplier = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        sizeMultiplier = to;
    }

    private void BuildFlames()
    {
        flames = new LineRenderer[flameCount];
        flameMaterials = new Material[flameCount];
        flameSeeds = new float[flameCount];

        for (int i = 0; i < flameCount; i++)
        {
            flames[i] = BuildLine(out flameMaterials[i]);
            flameSeeds[i] = Random.value * 100f;
        }
    }

    private void Update()
    {
        if (!burning || flames == null) return;

        for (int i = 0; i < flames.Length; i++) RedrawFlame(flames[i], flameMaterials[i], i);

        if (tintBody && bodySprite != null)
        {
            float flicker = Mathf.PerlinNoise(tintSeed, Time.time * tintFlickerSpeed);
            bodySprite.color = Color.Lerp(bodyOriginalColor, emberTint, flicker * tintFlickerStrength);
        }
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(다른 이펙트들과 동일한 방식). 위치는 항상
    // 월드 좌표로 직접 계산해서 넣으므로 useWorldSpace = true를 쓴다(부모 스케일에 안 휘둘리게).
    // 폭은 끝에서 뾰족해지는 widthCurve로 한 번만 세팅해두고, 색/위치는 매 프레임 다시 갱신한다.
    private LineRenderer BuildLine(out Material material)
    {
        GameObject go = new GameObject("FlameLine");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.numCapVertices = 3;
        line.sortingOrder = 16;
        line.positionCount = segmentsPerFlame;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        material = new Material(shader);
        line.material = material;

        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.15f, 1f), new Keyframe(0.6f, 0.55f), new Keyframe(1f, 0.05f));

        return line;
    }

    // 스프라이트의 "맨 위 가장자리" 한가운데 부근에서 여러 갈래가 솟아오르는 것처럼 뿌리 위치를
    // 잡는다(공 모양 적이라면 위쪽 곡면 폭에 맞춰 자연스럽게 좁게 퍼짐). 위로 갈수록 Perlin 노이즈로
    // 좌우로 흔들리고, 높이 자체도 노이즈로 출렁여서 활활 타오르는 느낌을 준다. 색은 재질의
    // _BaseColor로 내는데(정점 그라데이션은 이 셰이더에서 무시됨), 그 순간의 높이가 클수록(더
    // 활활 타오를수록) 뿌리색에서 끝색 쪽으로 옮겨가서 불꽃마다 색이 계속 바뀌는 것처럼 보인다.
    private void RedrawFlame(LineRenderer line, Material material, int index)
    {
        if (line == null || bodySprite == null) return;

        Bounds bounds = bodySprite.bounds;
        float seed = flameSeeds[index];

        float rootX = bounds.center.x + ((index + 0.5f) / flameCount - 0.5f) * bodyWidth * rootSpreadRatio;
        float rootY = bounds.center.y + bounds.extents.y; // 스프라이트 맨 위 가장자리
        float rootZ = bounds.center.z;

        float heightNoise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        float height = bodyHeight * Mathf.Lerp(minHeightRatio, maxHeightRatio, heightNoise) * sizeMultiplier;
        line.widthMultiplier = flameWidthRatio * bodyWidth * sizeMultiplier;

        for (int i = 0; i < segmentsPerFlame; i++)
        {
            float t = (float)i / (segmentsPerFlame - 1);
            float y = rootY + t * height;

            float sway = (Mathf.PerlinNoise(seed + 10f, Time.time * swaySpeed + t * 3f) - 0.5f) * 2f * swayAmountRatio * bodyWidth * t;
            float x = rootX + sway;

            line.SetPosition(i, new Vector3(x, y, rootZ));
        }

        Color flameColor = LerpTriColor(rootColor, midColor, tipColor, heightNoise);
        if (material != null)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", flameColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", flameColor);
        }
        line.startColor = flameColor;
        line.endColor = flameColor;
    }

    private static Color LerpTriColor(Color a, Color b, Color c, float t)
    {
        return t < 0.5f ? Color.Lerp(a, b, t * 2f) : Color.Lerp(b, c, (t - 0.5f) * 2f);
    }

    // Material은 인스턴스별로 새로 만들어졌으니 오브젝트가 파괴될 때 같이 정리한다.
    private void OnDestroy()
    {
        if (flameMaterials == null) return;
        foreach (Material material in flameMaterials)
        {
            if (material != null) Destroy(material);
        }
    }
}

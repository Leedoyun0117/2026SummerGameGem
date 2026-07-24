using UnityEngine;

// 노말(NoAbility) 적 몸 옆에 턴 시간이 흐르는 동안 파편이 점점 모이는(충전되는) 연출.
// LDY_BattleTurnManager가 매 프레임 SetProgress(0~1, 턴 진행도)를 불러주면, 그 진행도에 비례해서
// 점점 더 많은 파편 조각이 나타나 옆에 모이고, 각자 제자리에서 살짝 위아래로 흔들리며 반짝인다.
// 시간 초과로 실제 공격이 나갈 때(Release) 모인 파편들이 확 사라지며 정리된다.
// 별도 프리팹 설정 없이 적 자식으로 빈 GameObject를 만들어 이 스크립트만 붙여두면 바로 동작한다
// (LDY_Enemy가 GetComponentInChildren으로 자동으로 찾아서 연결한다).
public class LDY_FragmentChargeEffect : MonoBehaviour
{
    [Header("모이는 파편 개수(턴이 끝날 때까지 전부 모임) / 배치")]
    [SerializeField] private int maxFragments = 8;
    [SerializeField] private float clusterRadius = 0.35f;
    [SerializeField] private Vector2 clusterOffset = new Vector2(0.5f, 0.3f); // 몹 기준 옆쪽 위치
    [SerializeField] private float fragmentSize = 0.08f;

    [Header("색 (갈색)")]
    [SerializeField] private Color fragmentColor = new Color(0.45f, 0.27f, 0.1f, 1f);

    [Header("released 될 때 사라지는 시간")]
    [SerializeField] private float releaseDuration = 0.2f;

    private LineRenderer[] fragments;
    private Material[] fragmentMaterials;
    private float[] seeds;
    private float progress;
    private bool released;
    private float releaseTimer;

    private void Awake()
    {
        fragments = new LineRenderer[maxFragments];
        fragmentMaterials = new Material[maxFragments];
        seeds = new float[maxFragments];

        for (int i = 0; i < maxFragments; i++)
        {
            fragments[i] = BuildFragment(out fragmentMaterials[i]);
            seeds[i] = Random.value * 100f;
            fragments[i].gameObject.SetActive(false);
        }
    }

    // 0~1 진행도. LDY_Enemy.SetFragmentChargeProgress를 통해 LDY_BattleTurnManager가 매 프레임 넘겨준다.
    public void SetProgress(float t)
    {
        if (released) return;
        progress = Mathf.Clamp01(t);
    }

    // 시간 초과로 실제 공격이 나갈 때 호출 - 모인 파편들이 확 사라진다.
    public void Release()
    {
        if (released) return;
        released = true;
        releaseTimer = 0f;
    }

    // 사망 연출처럼 줄어드는 연출(releaseDuration) 없이 즉시 안 보이게 해야 할 때 씀.
    public void ForceHide()
    {
        released = false;
        progress = 0f;
        transform.localScale = Vector3.one;

        if (fragments == null) return;
        foreach (LineRenderer line in fragments)
        {
            if (line != null) line.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (released)
        {
            releaseTimer += Time.deltaTime;
            float shrink = 1f - Mathf.Clamp01(releaseTimer / releaseDuration);
            transform.localScale = Vector3.one * shrink;

            if (releaseTimer >= releaseDuration)
            {
                foreach (LineRenderer line in fragments)
                {
                    if (line != null) line.gameObject.SetActive(false);
                }
                transform.localScale = Vector3.one;
                progress = 0f;
                released = false;
            }
            return;
        }

        int activeCount = Mathf.RoundToInt(progress * maxFragments);
        for (int i = 0; i < fragments.Length; i++)
        {
            bool shouldBeActive = i < activeCount;
            if (fragments[i].gameObject.activeSelf != shouldBeActive) fragments[i].gameObject.SetActive(shouldBeActive);
            if (shouldBeActive) RedrawFragment(fragments[i], fragmentMaterials[i], i);
        }
    }

    // URP Unlit 셰이더는 LineRenderer의 정점 색(startColor/endColor)을 무시하고 재질의 _BaseColor만
    // 반영하므로(LDY_BurningEffect에서 확인됨), 색은 매 프레임 재질에 직접 넣는다.
    private void RedrawFragment(LineRenderer line, Material material, int index)
    {
        float seed = seeds[index];
        float angle = (360f / maxFragments) * index;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 basePos = transform.position + new Vector3(clusterOffset.x, clusterOffset.y, 0f)
            + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * clusterRadius;

        float bob = Mathf.Sin(Time.time * 3f + seed) * 0.03f;
        Vector3 center = basePos + new Vector3(0f, bob, 0f);

        float size = fragmentSize;
        line.positionCount = 4;
        line.SetPosition(0, center + new Vector3(0f, size, 0f));
        line.SetPosition(1, center + new Vector3(size, 0f, 0f));
        line.SetPosition(2, center + new Vector3(0f, -size, 0f));
        line.SetPosition(3, center + new Vector3(-size, 0f, 0f));

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", fragmentColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", fragmentColor);
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(다른 이펙트들과 동일한 방식). 렌더 큐도 UI보다
    // 확실히 뒤(위)로 밀어서 화면 아래쪽 UI에 가려지지 않게 한다.
    private LineRenderer BuildFragment(out Material material)
    {
        GameObject go = new GameObject("ChargeFragment");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = 0.03f;
        line.numCapVertices = 2;
        line.sortingOrder = 13;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        material = new Material(shader);
        material.renderQueue = 4000;
        line.material = material;

        return line;
    }

    // Material은 인스턴스별로 새로 만들어졌으니 오브젝트가 파괴될 때 같이 정리한다.
    private void OnDestroy()
    {
        if (fragmentMaterials == null) return;
        foreach (Material material in fragmentMaterials)
        {
            if (material != null) Destroy(material);
        }
    }
}

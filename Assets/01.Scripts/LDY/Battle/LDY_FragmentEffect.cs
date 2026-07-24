using System.Collections;
using UnityEngine;

// 노말(NoAbility) 적이 턴 시간 초과로 원거리 공격할 때 쓰는 파편 이펙트.
// 자기 위치(적)에서 TargetPosition(플레이어)까지 작은 파편 조각 여러 개가 살짝 포물선을 그리며
// 시차를 두고 흩날려 날아간다. 발사 빔(LDY_HitEffect)과 같은 방식(ILDY_EffectTarget)으로
// LDY_AttackTargetController/LDY_Enemy가 시작/끝 위치를 넣어준다.
// 별도 프리팹 설정 없이 빈 GameObject에 이 스크립트 하나만 붙여서 저장하면 바로 동작한다.
public class LDY_FragmentEffect : MonoBehaviour, ILDY_EffectTarget, ILDY_TravelEffect
{
    public Vector3 TargetPosition { get; set; }
    public float TravelDuration => travelDuration;

    [Header("파편 개수/모양")]
    [SerializeField] private int fragmentCount = 6;
    [SerializeField] private float fragmentWidth = 0.12f;
    [SerializeField] private float tailLength = 0.25f;
    [SerializeField] private float spread = 0.3f; // 출발/도착 지점이 이 반경 안에서 랜덤하게 흩어짐
    [SerializeField] private float arcHeight = 0.3f; // 날아가는 중간에 살짝 튀어오르는 높이(포물선)

    [Header("색 (갈색)")]
    [SerializeField] private Color fragmentColor = new Color(0.45f, 0.27f, 0.1f, 1f);

    [Header("날아가는 시간 / 조각마다 출발 시점을 살짝씩 다르게")]
    [SerializeField] private float travelDuration = 0.5f;
    [SerializeField] private float staggerRange = 0.15f;

    private Vector3 targetSnapshot;

    private void Awake()
    {
        Debug.Log($"[LDY_FragmentEffect] Awake @ {transform.position}");
        // TargetPosition은 Instantiate 직후 호출한 쪽(LDY_Enemy)이 설정해주는데, 코루틴이 그 설정보다
        // 먼저 읽어버리면 (0,0,0) 같은 잘못된 좌표로 날아가서 안 보이게 될 수 있다 - 한 프레임 쉬고 나서
        // 좌표를 스냅샷 떠서 이 문제를 피한다.
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        targetSnapshot = TargetPosition;
        Debug.Log($"[LDY_FragmentEffect] DelayedStart 재개, targetSnapshot={targetSnapshot}, fragmentCount={fragmentCount}");

        for (int i = 0; i < fragmentCount; i++) StartCoroutine(AnimateFragment());
        Destroy(gameObject, travelDuration + staggerRange + 0.3f);
    }

    private IEnumerator AnimateFragment()
    {
        float startDelay = Random.Range(0f, staggerRange);
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        GameObject go = new GameObject("Fragment");
        go.transform.SetParent(transform, false);
        Debug.Log($"[LDY_FragmentEffect] Fragment 생성, start={transform.position}, targetSnapshot={targetSnapshot}");

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = fragmentWidth;
        line.positionCount = 2;
        line.numCapVertices = 4;
        line.sortingOrder = 12;

        // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수
        // 있어서 URP Unlit 셰이더를 우선 사용한다. 이 셰이더는 LineRenderer의 정점 색(startColor/
        // endColor)을 무시하고 재질 자체의 _BaseColor만 반영하므로, 색은 매 프레임 재질에 직접 넣는다
        // (LDY_BurningEffect와 동일한 방식).
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.renderQueue = 4000; // UI(Screen Space Camera)에 가려지지 않도록 확실히 위로.
        line.material = material;

        Vector3 startOffset = Random.insideUnitCircle * spread;
        Vector3 endOffset = Random.insideUnitCircle * spread;
        Vector3 start = transform.position + startOffset;
        Vector3 end = targetSnapshot + endOffset;
        Vector3 dir = (end - start).sqrMagnitude > 0.0001f ? (end - start).normalized : Vector3.right;

        float t = 0f;
        float duration = travelDuration - startDelay;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            Vector3 pos = Vector3.Lerp(start, end, p);
            pos += Vector3.up * Mathf.Sin(p * Mathf.PI) * arcHeight; // 살짝 포물선을 그리며 날아감

            Vector3 tail = pos - dir * tailLength;
            line.SetPosition(0, tail);
            line.SetPosition(1, pos);

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", fragmentColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", fragmentColor);

            yield return null;
        }

        Destroy(go);
    }
}

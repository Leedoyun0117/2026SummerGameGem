using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 무기 UI에서 무기를 고르면 그 무기의 공격 범위(패턴)를 앵커 칸 기준으로 계산해서 빨간 하이라이트로
// 보여주고, 방향키로 앵커(공격 대상 위치)를 옮길 수 있게 해준다.
// 보드를 "4개 링(행) x 12칸(열)" 격자로 보고 LDY_AttackShapeUtility의 오프셋을 그대로 적용한다.
// 하이라이트는 지름선(LDY_RadialLineController)과 같은 방식 - PNG 마커가 아니라 LineRenderer로
// 각 칸의 테두리(안쪽 호 + 바깥쪽 호 + 양옆 반지름 선)를 그려서 그 칸 영역 자체를 윤곽으로 보여준다.
// 링 자체를 회전시키는 입력(LDY_RingSelectionManager)과는 별개 모드로 동작 - 무기가 선택된 동안은
// 방향키가 링 회전 대신 이 앵커를 옮기는 데 쓰인다(LDY_RingSelectionManager 쪽에서 분기 처리).
public class LDY_AttackTargetController : MonoBehaviour
{
    public static LDY_AttackTargetController Instance { get; private set; }

    [Header("안쪽 -> 바깥쪽 순서로 등록된 동심원 링들")]
    [SerializeField] private LDY_RingController[] ringsInnerToOuter;

    // 각 링의 안쪽/바깥쪽 경계 반지름(ringsInnerToOuter와 같은 순서·같은 길이여야 함).
    // 칸 하나의 테두리를 그릴 때 이 반지름 구간을 그대로 쓴다(맨 안쪽 링은 innerBound가 0 -> 중심까지 뾰족하게 닫힘).
    [Header("각 링의 안쪽/바깥쪽 경계 반지름")]
    [SerializeField] private float[] ringInnerBounds;
    [SerializeField] private float[] ringOuterBounds;

    [Header("하이라이트 곡선 해상도 (칸 하나당 호를 이루는 점 개수)")]
    [SerializeField] private int arcResolution = 8;

    [Header("무기 적중 이펙트(레이저 등)가 나오는 시작 위치 - 비워두면 이 컨트롤러의 위치")]
    [SerializeField] private Transform effectOrigin;

    public bool IsTargeting { get; private set; }
    public LDY_Weapon CurrentWeapon { get; private set; }

    // 공격 이펙트(빔/관통 연출)가 재생되는 동안 true. LDY_BattleTurnManager는 이동안 턴 시간을 멈추고,
    // LDY_RingSelectionManager는 이동안 모든 입력을 무시한다(연출 중 다른 조작이 끼어들지 못하게).
    public bool IsResolvingEffect { get; private set; }

    // 공격이 실행되면 대상 슬롯 목록을 흘려보낸다. 실제 데미지/적 처치 로직은 적 종류가 정해진 뒤
    // 이 이벤트에 연결하면 됨 - 지금은 로그만 남긴다.
    public event Action<List<RingSlot>> OnAttackExecuted;

    private int anchorRingIndex;
    private int anchorSegmentIndex;
    private readonly List<LineRenderer> highlightPool = new List<LineRenderer>();

    private struct TargetCell
    {
        public int ringIndex;
        public int segmentIndex;
        public RingSlot slot;
    }

    private void Awake()
    {
        Instance = this;
    }

    // ----------------- 무기 선택 -----------------

    public void SetWeapon(LDY_Weapon weapon)
    {
        CurrentWeapon = weapon;
        IsTargeting = weapon != null;

        // 관통 무기는 항상 앞칸(0번 링)부터 끝까지 전체를 뚫어야 하므로, 위/아래로 앵커 링을 옮겨서
        // 일부만 걸치는(뒤쪽만 맞는) 상황이 아예 생기지 않게 매번 0으로 고정해서 시작한다.
        if (weapon != null && weapon.isPiercing) anchorRingIndex = 0;

        RefreshHighlights();
    }

    public void ClearWeapon()
    {
        CurrentWeapon = null;
        IsTargeting = false;
        HideAllHighlights();
    }

    // ----------------- 앵커(공격 대상) 이동 -----------------

    public void MoveCursor(int ringDelta, int segmentDelta)
    {
        if (!IsTargeting) return;
        if (ringsInnerToOuter == null || ringsInnerToOuter.Length == 0) return;

        // 관통 무기는 항상 0번 링부터 전체를 뚫으므로 위/아래로 앵커 링을 옮기지 못하게 막는다
        // (옮길 수 있으면 범위 밖으로 잘린 일부 칸만 맞는 상황이 생겨서 "1->2->3->4 순서로 뚫는" 연출이 깨짐).
        bool lockRing = CurrentWeapon != null && CurrentWeapon.isPiercing;
        if (!lockRing)
        {
            anchorRingIndex = Mathf.Clamp(anchorRingIndex + ringDelta, 0, ringsInnerToOuter.Length - 1);
        }

        int segCount = GetSegmentCount();
        anchorSegmentIndex = ((anchorSegmentIndex + segmentDelta) % segCount + segCount) % segCount;

        RefreshHighlights();
    }

    private int GetSegmentCount()
    {
        if (ringsInnerToOuter == null || ringsInnerToOuter.Length == 0 || ringsInnerToOuter[0].Ring == null) return 12;
        return Mathf.Max(ringsInnerToOuter[0].Ring.SlotCount, 1);
    }

    // ----------------- 대상 칸 계산 -----------------

    private List<TargetCell> GetTargetedCells()
    {
        var result = new List<TargetCell>();
        if (CurrentWeapon == null || ringsInnerToOuter == null || ringsInnerToOuter.Length == 0) return result;

        List<Vector2Int> offsets = LDY_AttackShapeUtility.GetOffsets(CurrentWeapon.shape);
        int segCount = GetSegmentCount();

        foreach (Vector2Int offset in offsets)
        {
            int ringIndex = anchorRingIndex + offset.x;
            if (ringIndex < 0 || ringIndex >= ringsInnerToOuter.Length) continue; // 링 방향은 순환하지 않고 범위 밖이면 제외

            LDY_RingController ring = ringsInnerToOuter[ringIndex];
            if (ring == null || ring.Ring == null) continue;

            int segmentIndex = ((anchorSegmentIndex + offset.y) % segCount + segCount) % segCount;
            result.Add(new TargetCell { ringIndex = ringIndex, segmentIndex = segmentIndex, slot = ring.Ring.GetSlot(segmentIndex) });
        }

        return result;
    }

    private List<RingSlot> GetTargetedSlots()
    {
        List<TargetCell> cells = GetTargetedCells();
        var slots = new List<RingSlot>(cells.Count);
        foreach (TargetCell cell in cells) slots.Add(cell.slot);
        return slots;
    }

    // ----------------- 하이라이트 표시 -----------------

    private void RefreshHighlights()
    {
        if (!IsTargeting)
        {
            HideAllHighlights();
            return;
        }

        List<TargetCell> cells = GetTargetedCells();
        EnsurePoolSize(cells.Count);

        for (int i = 0; i < highlightPool.Count; i++)
        {
            if (i < cells.Count)
            {
                DrawCellOutline(highlightPool[i], cells[i].ringIndex, cells[i].segmentIndex);
                highlightPool[i].gameObject.SetActive(true);
            }
            else
            {
                highlightPool[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideAllHighlights()
    {
        foreach (LineRenderer line in highlightPool)
        {
            if (line != null) line.gameObject.SetActive(false);
        }
    }

    // 칸 하나의 테두리(안쪽 호 -> 바깥쪽 호 -> loop으로 자동 연결되는 양옆 반지름 선)를 그린다.
    // 맨 안쪽 레이어처럼 innerRadius가 0이면 안쪽 호가 중심의 한 점으로 뭉개져서 자연스럽게 뾰족한 조각이 된다.
    private void DrawCellOutline(LineRenderer line, int ringIndex, int segmentIndex)
    {
        float innerR = (ringInnerBounds != null && ringIndex < ringInnerBounds.Length) ? ringInnerBounds[ringIndex] : 0f;
        float outerR = (ringOuterBounds != null && ringIndex < ringOuterBounds.Length) ? ringOuterBounds[ringIndex] : innerR + 1f;

        int segCount = GetSegmentCount();
        float sliceAngle = 360f / segCount;
        float centerAngle = segmentIndex * sliceAngle;
        float startDeg = centerAngle - sliceAngle * 0.5f;
        float endDeg = centerAngle + sliceAngle * 0.5f;

        int arcRes = Mathf.Max(arcResolution, 2);
        Vector3 center = transform.position;

        line.loop = true;
        line.positionCount = arcRes * 2;

        // 안쪽 호: start -> end
        for (int i = 0; i < arcRes; i++)
        {
            float t = (float)i / (arcRes - 1);
            float rad = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, t);
            line.SetPosition(i, center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * innerR);
        }

        // 바깥쪽 호: end -> start (역방향으로 되돌아와서 loop이 자연스럽게 닫힘)
        for (int i = 0; i < arcRes; i++)
        {
            float t = (float)i / (arcRes - 1);
            float rad = Mathf.Deg2Rad * Mathf.Lerp(endDeg, startDeg, t);
            line.SetPosition(arcRes + i, center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * outerR);
        }
    }

    // 이 프로젝트는 URP를 쓰기 때문에 빌트인 전용 셰이더(Sprites/Default)를 쓰면 마젠타로 보일 수 있어서
    // URP에 기본 포함된 Unlit 셰이더를 우선 사용한다(LDY_RadialLineController와 동일한 방식).
    private void EnsurePoolSize(int count)
    {
        while (highlightPool.Count < count)
        {
            GameObject go = new GameObject($"AttackCellOutline_{highlightPool.Count}");
            go.transform.SetParent(transform);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.12f;
            line.numCapVertices = 2;
            line.sortingOrder = 20; // 선택 하이라이트보다도 위에

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);

            Color red = new Color(1f, 0.15f, 0.15f, 0.95f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", red);
            if (material.HasProperty("_Color")) material.SetColor("_Color", red);

            line.material = material;
            line.startColor = red;
            line.endColor = red;

            go.SetActive(false);
            highlightPool.Add(line);
        }
    }

    // ----------------- 공격 실행 -----------------

    // 무기 UI의 "공격" 버튼 등에서 호출. 대상 슬롯에 적(LDY_Enemy)이 있으면:
    // - 맞은 무기 모양이 그 적의 반사 모양과 일치하면(WeaponReflector) 죽이지 않고 플레이어가 반사 데미지를 입는다.
    // - 관통(isPiercing) 무기는 앞칸부터 순서대로 하나씩 처리하다가 반사를 만나면 그 자리에서 멈춘다(뒤쪽 보호).
    // - 그 외(비관통)는 기존처럼 대상 칸을 한 번에 전부 처리한다.
    public void ExecuteAttack()
    {
        if (!IsTargeting || CurrentWeapon == null) return;

        // OnAttackExecuted 구독자(예: LDY_WeaponUIController)가 이 이벤트 안에서 ClearWeapon()을 불러
        // CurrentWeapon이 곧바로 null이 될 수 있으므로, 이후에 쓸 값은 전부 미리 복사해둔다.
        LDY_Weapon weapon = CurrentWeapon;
        List<RingSlot> targeted = GetTargetedSlots();

        StartCoroutine(ResolveAttackRoutine(weapon, targeted));

        OnAttackExecuted?.Invoke(targeted);
    }

    // 공격 이펙트가 재생되는 동안(IsResolvingEffect) 턴 타이머는 멈추고 입력도 전부 막히고, 씬의
    // 모든 애니메이터(적 idle 등)도 같이 멈춘다 - 연출이 다 끝나야(관통이면 끝까지 진행되거나 반사로
    // 멈출 때까지, 비관통이면 hitEffectDuration만큼) 다시 풀린다.
    private IEnumerator ResolveAttackRoutine(LDY_Weapon weapon, List<RingSlot> targeted)
    {
        IsResolvingEffect = true;
        SetAllAnimatorsPaused(true);

        if (weapon.isPiercing)
        {
            yield return PierceRoutine(weapon, targeted);
        }
        else
        {
            yield return SimultaneousRoutine(weapon, targeted);
        }

        SetAllAnimatorsPaused(false);
        IsResolvingEffect = false;
    }

    // 이펙트 자체(레이저 등)는 Animator가 아니라 직접 좌표를 계산해서 그리므로 영향받지 않는다 -
    // 순수하게 캐릭터/UI 애니메이션만 멈춘다.
    private void SetAllAnimatorsPaused(bool paused)
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator animator in animators)
        {
            animator.speed = paused ? 0f : 1f;
        }
    }

    // 비관통 무기: 대상 칸을 한 번에 전부 처리하되(기존 방식), 가장 오래 걸리는 적중 이펙트가 끝날
    // 때까지는 이 코루틴이 끝나지 않게 해서 ResolveAttackRoutine이 IsResolvingEffect를 계속 true로 유지한다.
    private IEnumerator SimultaneousRoutine(LDY_Weapon weapon, List<RingSlot> targeted)
    {
        int hitCount = 0;

        foreach (RingSlot slot in targeted)
        {
            if (slot.occupant == null) continue;

            LDY_Enemy enemy = slot.occupant.GetComponent<LDY_Enemy>();
            if (enemy != null && enemy.TryReflect(weapon.shape))
            {
                continue; // 반사됨 - 적은 그대로 두고(안 죽음) 플레이어만 데미지를 입는다(TryReflect 내부 처리).
            }

            hitCount++;
            ResolveHit(slot.occupant, weapon.hitEffectPrefab, weapon.hitEffectDuration);
            slot.occupant = null;
        }

        Debug.Log($"[LDY_AttackTargetController] '{weapon.weaponName}' 공격 실행 - 대상 슬롯 {targeted.Count}개 중 {hitCount}개 처치");

        if (hitCount > 0 && weapon.hitEffectPrefab != null) yield return new WaitForSeconds(weapon.hitEffectDuration);
    }

    // 대상 칸을 앞(anchor)에서부터 순서대로 하나씩 처리한다. 빈 칸은 건너뛰고, 반사(WeaponReflector)를
    // 만나면 반사 연출만 내고 그 자리에서 멈춘다(그 뒤쪽 칸들은 아예 손대지 않음 - 반사한 적이 막아준 것).
    // 그 외의 적은: 발사 빔 + 적중 이펙트를 내고 hitEffectDuration만큼 기다렸다가 죽인 뒤 다음 칸으로 넘어간다.
    private IEnumerator PierceRoutine(LDY_Weapon weapon, List<RingSlot> targeted)
    {
        float stepDelay = Mathf.Max(weapon.hitEffectDuration, 0.05f);
        Vector3 origin = effectOrigin != null ? effectOrigin.position : transform.position;

        foreach (RingSlot slot in targeted)
        {
            if (slot.occupant == null) continue;

            LDY_Enemy enemy = slot.occupant.GetComponent<LDY_Enemy>();
            if (enemy != null && enemy.TryReflect(weapon.shape))
            {
                // 먼저 평소처럼 발사 빔이 적에게 직접 맞는 걸 보여준 다음에, 반사 이펙트가 적 -> 나에게로 나간다.
                SpawnBeamEffect(weapon.hitEffectPrefab, origin, slot.occupant.transform.position, stepDelay);
                yield return new WaitForSeconds(stepDelay);

                SpawnBeamEffect(weapon.reflectEffectPrefab, slot.occupant.transform.position, origin, stepDelay);
                yield return new WaitForSeconds(stepDelay); // 반사 이펙트가 재생되는 동안도 시간/입력을 계속 멈춰둔다.
                yield break; // 관통이 여기서 멈춘다 - 뒤쪽 칸은 보호됨.
            }

            GameObject occupant = slot.occupant;
            slot.occupant = null;

            SpawnBeamEffect(weapon.hitEffectPrefab, origin, occupant.transform.position, stepDelay);
            if (weapon.impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(weapon.impactEffectPrefab, occupant.transform.position, Quaternion.identity);
                Destroy(impact, stepDelay);
            }

            yield return new WaitForSeconds(stepDelay);

            if (occupant != null) Destroy(occupant);
        }
    }

    // fromPos -> toPos로 이어지는 빔 이펙트를 하나 띄운다(ILDY_EffectTarget을 구현했으면 시작/끝 위치를 그쪽에 알려줌).
    private void SpawnBeamEffect(GameObject prefab, Vector3 fromPos, Vector3 toPos, float lifetime)
    {
        if (prefab == null) return;

        GameObject beam = Instantiate(prefab, fromPos, Quaternion.identity);
        if (beam.TryGetComponent(out ILDY_EffectTarget effectTarget)) effectTarget.TargetPosition = toPos;

        Destroy(beam, lifetime);
    }

    // 이펙트가 없으면(hitEffectPrefab == null) 기존처럼 바로 파괴한다.
    // 이펙트가 있으면 effectOrigin 위치에 이펙트를 띄우고(LDY_HitEffect가 있으면 맞은 적의 좌표를 알려줌),
    // 이펙트가 재생되는 동안(hitEffectDuration) 적을 살려뒀다가 그 다음에 같이 파괴한다.
    private void ResolveHit(GameObject enemyObj, GameObject hitEffectPrefab, float hitEffectDuration)
    {
        if (hitEffectPrefab == null)
        {
            Destroy(enemyObj);
            return;
        }

        Vector3 origin = effectOrigin != null ? effectOrigin.position : transform.position;
        SpawnBeamEffect(hitEffectPrefab, origin, enemyObj.transform.position, hitEffectDuration);
        StartCoroutine(KillAfterDelay(enemyObj, hitEffectDuration));
    }

    private IEnumerator KillAfterDelay(GameObject target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (target != null) Destroy(target);
    }
}

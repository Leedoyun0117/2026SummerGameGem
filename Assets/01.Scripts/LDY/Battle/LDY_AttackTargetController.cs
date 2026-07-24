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

        // 어떤 무기든 앵커 링은 항상 0번(가장 안쪽)에서 시작한다 - 위/아래로 옮겨서 원래 안 닿아야 할
        // 먼 링까지 공격 범위가 넘어가는 걸 막기 위해서다(관통 무기뿐 아니라 망치/검도 동일하게 적용).
        anchorRingIndex = 0;

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

        // 어떤 무기든 위/아래로 앵커 링을 옮기지 못하게 막는다 - 옮길 수 있으면 원래 안 닿아야 할
        // 먼 링에 있는 적까지 공격 범위가 넘어가 버린다. 좌/우(세그먼트) 이동만 허용한다.
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
        List<TargetCell> cells = GetTargetedCells();
        List<RingSlot> targeted = new List<RingSlot>(cells.Count);
        foreach (TargetCell cell in cells) targeted.Add(cell.slot);

        StartCoroutine(ResolveAttackRoutine(weapon, targeted, cells));

        OnAttackExecuted?.Invoke(targeted);
    }

    // 공격 이펙트가 재생되는 동안(IsResolvingEffect) 턴 타이머는 멈추고 입력도 전부 막히고, 씬의
    // 모든 애니메이터(적 idle 등)도 같이 멈춘다 - 연출이 다 끝나야(관통이면 끝까지 진행되거나 반사로
    // 멈출 때까지, 비관통이면 hitEffectDuration만큼) 다시 풀린다.
    // 대상 칸 모양대로 SpriteMask를 깔아서(SpawnRangeMasks) 이펙트가 아무리 커도 그 칸 밖으로는 안 보이게 한다.
    private IEnumerator ResolveAttackRoutine(LDY_Weapon weapon, List<RingSlot> targeted, List<TargetCell> cells)
    {
        IsResolvingEffect = true;
        SetAllAnimatorsPaused(true);

        List<GameObject> rangeMasks = SpawnRangeMasks(cells);

        if (weapon.isPiercing)
        {
            yield return PierceRoutine(weapon, targeted);
        }
        else
        {
            yield return SimultaneousRoutine(weapon, targeted, cells);
        }

        foreach (GameObject mask in rangeMasks)
        {
            if (mask != null) Destroy(mask);
        }

        SetAllAnimatorsPaused(false);
        IsResolvingEffect = false;
    }

    // 대상 칸 하나하나마다 그 칸의 부채꼴을 근사하는 작은 회전된 사각형 SpriteMask를 깔아준다
    // (안쪽/바깥쪽 반지름을 세로 길이로, 그 반지름 중간 지점의 호 길이(chord)를 가로 길이로 사용).
    // 무기 이펙트 프리팹의 SpriteRenderer/ParticleSystemRenderer에서 Mask Interaction을
    // "Visible Inside Mask"로 설정해두면, 이펙트가 아무리 커도 이 칸 밖으로는 안 보이게 잘린다.
    private List<GameObject> SpawnRangeMasks(List<TargetCell> cells)
    {
        var masks = new List<GameObject>(cells.Count);
        int segCount = GetSegmentCount();
        float sliceAngle = 360f / segCount;
        Vector3 center = transform.position;
        Sprite sprite = GetOrCreateMaskSprite();

        foreach (TargetCell cell in cells)
        {
            float innerR = (ringInnerBounds != null && cell.ringIndex < ringInnerBounds.Length) ? ringInnerBounds[cell.ringIndex] : 0f;
            float outerR = (ringOuterBounds != null && cell.ringIndex < ringOuterBounds.Length) ? ringOuterBounds[cell.ringIndex] : innerR + 1f;
            float midR = (innerR + outerR) * 0.5f;
            float height = Mathf.Max(outerR - innerR, 0.01f);
            float chordWidth = 2f * midR * Mathf.Sin(sliceAngle * 0.5f * Mathf.Deg2Rad);

            float centerAngleDeg = cell.segmentIndex * sliceAngle;
            float rad = centerAngleDeg * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * midR;

            GameObject go = new GameObject("RangeMask");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, centerAngleDeg - 90f);
            go.transform.localScale = new Vector3(Mathf.Max(chordWidth, 0.01f), height, 1f);

            SpriteMask mask = go.AddComponent<SpriteMask>();
            mask.sprite = sprite;

            masks.Add(go);
        }

        return masks;
    }

    private Sprite maskSprite;

    // 1x1 흰색 정사각형 스프라이트 하나를 만들어서 캐싱해두고, 모든 range mask가 이걸 늘려서 재사용한다.
    private Sprite GetOrCreateMaskSprite()
    {
        if (maskSprite != null) return maskSprite;

        Texture2D tex = new Texture2D(2, 2);
        Color[] pixels = { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();

        maskSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        return maskSprite;
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

    // 비관통 무기: 대상 칸을 한 번에 전부 처리한다(기존 방식). 적마다 이펙트를 따로 띄우지 않고,
    // 대상 범위 전체(칸 여러 개를 합친 영역) 크기에 맞춰 이펙트를 한 번만 크게 띄운다 - 그래야 이펙트가
    // 좁은 한 칸 크기가 아니라 무기 범위 전체를 채우는 크기로 보인다. SpawnRangeMasks가 이 범위 밖은
    // 잘라주므로, 이펙트 프리팹 자체는 이보다 훨씬 커도 상관없다.
    private IEnumerator SimultaneousRoutine(LDY_Weapon weapon, List<RingSlot> targeted, List<TargetCell> cells)
    {
        int hitCount = 0;
        var toKill = new List<GameObject>();

        foreach (RingSlot slot in targeted)
        {
            if (slot.occupant == null) continue;

            LDY_Enemy enemy = slot.occupant.GetComponent<LDY_Enemy>();
            if (enemy != null && enemy.TryReflect(weapon.shape))
            {
                continue; // 반사됨 - 적은 그대로 두고(안 죽음) 플레이어만 데미지를 입는다(TryReflect 내부 처리).
            }

            hitCount++;
            toKill.Add(slot.occupant);
            slot.occupant = null;
        }

        Debug.Log($"[LDY_AttackTargetController] '{weapon.weaponName}' 공격 실행 - 대상 슬롯 {targeted.Count}개 중 {hitCount}개 처치");

        if (hitCount == 0) yield break;

        // 이펙트가 뜨는 시점에 맞춰 맞은 적들이 전부 빨개지며 흔들리고(+ 무기에 따라 지지직 효과) 반응한다.
        foreach (GameObject enemyObj in toKill)
        {
            if (enemyObj == null) continue;
            LDY_Enemy hitEnemy = enemyObj.GetComponent<LDY_Enemy>();
            if (hitEnemy != null) hitEnemy.PlayHitReaction(weapon);
        }

        if (weapon.hitEffectPrefab != null)
        {
            SpawnRangeFittedEffect(weapon, cells);
            yield return new WaitForSeconds(weapon.hitEffectDuration);
        }

        foreach (GameObject enemyObj in toKill)
        {
            if (enemyObj == null) continue;
            if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.SpawnDrops(enemyObj.transform.position);
            Destroy(enemyObj);
        }
    }

    // 대상 칸들을 전부 합친 범위(가장 안쪽~바깥쪽 링, 가장 왼쪽~오른쪽 칸)를 계산해서 이펙트를 띄운다.
    // weapon.hitEffectFromOrigin이 켜져 있으면 보드 중심(0,0)에서 범위 각도로 회전만 시켜서 내보낸다
    // (부채꼴/슬래시처럼 중심에서 뻗어나가는 효과용 - 사각형으로 늘려서 왜곡시키지 않는다. 대신
    // ILDY_RangeEffect를 구현했으면 반지름/각도 값을 그대로 넘겨줘서 파티클 Shape 모듈 쪽에서 직접
    // 그 모양을 만들게 한다). 꺼져 있으면 실제 범위의 한가운데 위치에 그대로 생성한다(그 자리에서
    // 터지는 효과용 - 예: 망치).
    private void SpawnRangeFittedEffect(LDY_Weapon weapon, List<TargetCell> cells)
    {
        GameObject prefab = weapon.hitEffectPrefab;
        if (prefab == null || cells == null || cells.Count == 0) return;

        int segCount = GetSegmentCount();
        float sliceAngle = 360f / segCount;

        int minRing = int.MaxValue, maxRing = int.MinValue;
        int minSeg = int.MaxValue, maxSeg = int.MinValue;
        foreach (TargetCell cell in cells)
        {
            minRing = Mathf.Min(minRing, cell.ringIndex);
            maxRing = Mathf.Max(maxRing, cell.ringIndex);
            minSeg = Mathf.Min(minSeg, cell.segmentIndex);
            maxSeg = Mathf.Max(maxSeg, cell.segmentIndex);
        }

        float innerR = (ringInnerBounds != null && minRing < ringInnerBounds.Length) ? ringInnerBounds[minRing] : 0f;
        float outerR = (ringOuterBounds != null && maxRing < ringOuterBounds.Length) ? ringOuterBounds[maxRing] : innerR + 1f;
        float totalAngle = (maxSeg - minSeg + 1) * sliceAngle;
        float centerAngleDeg = (minSeg + maxSeg) * 0.5f * sliceAngle;

        Vector3 spawnPos;
        if (weapon.hitEffectFromOrigin)
        {
            spawnPos = transform.position;
        }
        else
        {
            float midR = (innerR + outerR) * 0.5f;
            float rad = centerAngleDeg * Mathf.Deg2Rad;
            spawnPos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * midR;
        }

        GameObject effect = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, centerAngleDeg));
        effect.transform.localScale *= Mathf.Max(weapon.hitEffectScale, 0.01f);

        if (effect.TryGetComponent(out ILDY_RangeEffect rangeEffect))
        {
            rangeEffect.SetRange(innerR, outerR, totalAngle);
        }

        Destroy(effect, weapon.hitEffectDuration);
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
            if (enemy != null && enemy.WillReflect(weapon.shape))
            {
                // 1) 먼저 평소처럼(일반 적을 맞출 때와 동일하게) 발사 빔 + 적중 이펙트가 적에게 직접
                //    맞는 걸 보여준다(아직 반사/데미지 없음).
                SpawnBeamEffect(weapon.hitEffectPrefab, origin, slot.occupant.transform.position, stepDelay);
                if (weapon.impactEffectPrefab != null)
                {
                    GameObject impact = Instantiate(weapon.impactEffectPrefab, slot.occupant.transform.position, Quaternion.identity);
                    Destroy(impact, stepDelay);
                }
                yield return new WaitForSeconds(stepDelay);

                // 2) 반사 이펙트가 적 -> 나에게로 날아간다. 실제로 나에게 "도착"하는 시점(TravelDuration)에
                //    맞춰서 그때 가서야 데미지를 준다 - 쏘자마자 바로 맞는 게 아니라 날아오는 동안은 안전하다.
                GameObject reflectBeam = SpawnBeamEffect(weapon.reflectEffectPrefab, slot.occupant.transform.position, origin, stepDelay);
                float travelWait = (reflectBeam != null && reflectBeam.TryGetComponent(out ILDY_TravelEffect travelEffect))
                    ? Mathf.Min(travelEffect.TravelDuration, stepDelay)
                    : 0f;

                if (travelWait > 0f) yield return new WaitForSeconds(travelWait);
                enemy.ApplyReflectDamage();

                float remainingWait = stepDelay - travelWait;
                if (remainingWait > 0f) yield return new WaitForSeconds(remainingWait); // 반사 이펙트가 끝까지 재생되는 동안도 시간/입력을 계속 멈춰둔다.
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
            if (enemy != null) enemy.PlayHitReaction(weapon);

            yield return new WaitForSeconds(stepDelay);

            if (occupant != null)
            {
                if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.SpawnDrops(occupant.transform.position);
                Destroy(occupant);
            }
        }
    }

    // fromPos -> toPos로 이어지는 빔 이펙트를 하나 띄운다(ILDY_EffectTarget을 구현했으면 시작/끝 위치를 그쪽에 알려줌).
    // 생성된 인스턴스를 돌려줘서, 호출한 쪽에서 ILDY_TravelEffect 같은 걸 추가로 확인할 수 있게 한다.
    private GameObject SpawnBeamEffect(GameObject prefab, Vector3 fromPos, Vector3 toPos, float lifetime)
    {
        if (prefab == null) return null;

        GameObject beam = Instantiate(prefab, fromPos, Quaternion.identity);
        if (beam.TryGetComponent(out ILDY_EffectTarget effectTarget)) effectTarget.TargetPosition = toPos;

        Destroy(beam, lifetime);
        return beam;
    }

}

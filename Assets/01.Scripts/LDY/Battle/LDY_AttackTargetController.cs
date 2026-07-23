using System;
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

    public bool IsTargeting { get; private set; }
    public LDY_Weapon CurrentWeapon { get; private set; }

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

        anchorRingIndex = Mathf.Clamp(anchorRingIndex + ringDelta, 0, ringsInnerToOuter.Length - 1);

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

    // 무기 UI의 "공격" 버튼 등에서 호출. 일반전이므로 맞으면 그냥 즉사 - 대상 슬롯에 적이 있으면
    // 오브젝트를 파괴하고 슬롯을 비운다.
    public void ExecuteAttack()
    {
        if (!IsTargeting || CurrentWeapon == null) return;

        List<RingSlot> targeted = GetTargetedSlots();

        int hitCount = 0;
        foreach (RingSlot slot in targeted)
        {
            if (slot.occupant == null) continue;

            hitCount++;
            Destroy(slot.occupant);
            slot.occupant = null;
        }

        OnAttackExecuted?.Invoke(targeted);

        Debug.Log($"[LDY_AttackTargetController] '{CurrentWeapon.weaponName}' 공격 실행 - 대상 슬롯 {targeted.Count}개 중 {hitCount}개 처치");
    }
}

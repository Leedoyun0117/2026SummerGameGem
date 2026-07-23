using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 오리가미킹의 "지름선" 이동 - 동심원 보드를 가로지르는 지름선(예: 1시 방향 ~ 7시 방향처럼 안쪽부터
// 바깥쪽까지 모든 링을 관통하는 한 줄)을 좌/우로 고른 뒤, 위/아래로 밀어서 적을 링과 링 사이로
// 이동시키는 컴포넌트. 이 컴포넌트 자신은 BattleRing을 갖지 않고, 이미 존재하는 여러 개의
// LDY_RingController(동심원들)가 들고 있는 RingSlot들을 그때그때 하나의 리스트로 이어붙여서 사용한다.
//
// 좌/우: 지금 강조된 지름선을 바꾼다(오브젝트 이동 없음, 그냥 커서를 옮기는 느낌).
// 위/아래: 그 지름선을 따라 실제로 occupant를 한 칸씩 민다(끝까지 밀면 고리처럼 반대쪽 끝에서 다시 나타남).
public class LDY_RadialLineController : MonoBehaviour
{
    [Header("안쪽 -> 바깥쪽 순서로 등록된 동심원 링들 (전부 같은 슬롯 개수여야 함)")]
    [SerializeField] private LDY_RingController[] ringsInnerToOuter;

    [Header("회전 연출")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // 지름선 전체를 가로지르는 막대 대신, 맨 바깥 링에서 지금 선택된 두 칸(예: 1번/7번)의 "바깥 테두리"만
    // 노란 호(arc)로 강조한다. index 0 = 지름선의 이쪽 편, index 1 = 반대편(180도 반대쪽).
    // 스프라이트/투명도 문제를 아예 피하려고 PNG 대신 LineRenderer로 직접 곡선을 그린다.
    [Header("지름선 강조 표시 (선택되면 켜지고, 각 칸의 바깥 테두리 자리에 곡선을 그리는 LineRenderer 2개)")]
    [SerializeField] private LineRenderer[] highlightArcs;
    [SerializeField] private float highlightArcRadius = 13.5f; // 맨 바깥 링의 바깥 경계 반지름
    [SerializeField] private int highlightArcResolution = 12; // 곡선을 이루는 점 개수

    public bool IsSelected { get; private set; }
    public bool IsShifting { get; private set; }

    private int currentDiameterIndex;

    private void Awake()
    {
        SetHighlightArcsActive(false);
    }

    // ----------------- 선택 -----------------

    public void Select()
    {
        if (IsSelected) return;
        IsSelected = true;
        SetHighlightArcsActive(true);
        UpdateHighlightRotation();
    }

    public void Deselect()
    {
        if (!IsSelected) return;
        IsSelected = false;
        SetHighlightArcsActive(false);
    }

    private void SetHighlightArcsActive(bool active)
    {
        if (highlightArcs == null) return;
        foreach (LineRenderer arc in highlightArcs)
        {
            if (arc != null) arc.gameObject.SetActive(active);
        }
    }

    // ----------------- 지름선 고르기 (좌/우) -----------------

    // segmentCount가 12면 지름선은 6개(1-7시, 2-8시 ...)뿐이다 - i번째와 (i + segmentCount/2)번째 슬롯이
    // 같은 지름선의 반대쪽 끝이라서, 실질적으로 서로 다른 지름선은 segmentCount/2개밖에 없기 때문.
    public void CycleDiameter(int direction)
    {
        int diameterCount = GetDiameterCount();
        if (diameterCount <= 0) return;

        currentDiameterIndex = ((currentDiameterIndex + direction) % diameterCount + diameterCount) % diameterCount;
        UpdateHighlightRotation();
    }

    private int GetDiameterCount()
    {
        if (ringsInnerToOuter == null || ringsInnerToOuter.Length == 0 || ringsInnerToOuter[0].Ring == null) return 0;
        return Mathf.Max(ringsInnerToOuter[0].Ring.SlotCount / 2, 1);
    }

    private void UpdateHighlightRotation()
    {
        if (highlightArcs == null || highlightArcs.Length == 0) return;
        if (ringsInnerToOuter == null || ringsInnerToOuter.Length == 0 || ringsInnerToOuter[0].Ring == null) return;

        int segmentCount = ringsInnerToOuter[0].Ring.SlotCount;
        float sliceAngle = 360f / segmentCount;
        float baseAngle = currentDiameterIndex * sliceAngle;

        if (highlightArcs.Length > 0 && highlightArcs[0] != null)
        {
            DrawArc(highlightArcs[0], baseAngle, sliceAngle);
        }
        if (highlightArcs.Length > 1 && highlightArcs[1] != null)
        {
            DrawArc(highlightArcs[1], baseAngle + 180f, sliceAngle);
        }
    }

    // centerAngleDeg를 중심으로 sliceAngleDeg 폭만큼 바깥 테두리에 호를 그리되, 원의 중심에서부터 그 호까지
    // 이어지는 선(중심 -> 호 시작점 -> 호를 따라 곡선 -> 호 끝점 -> 다시 중심)으로 파이 조각 모양을 만든다.
    // 스프라이트가 아니라 월드 좌표를 직접 계산해서 그리기 때문에 스케일/투명도 문제가 생길 여지가 없다.
    private void DrawArc(LineRenderer line, float centerAngleDeg, float sliceAngleDeg)
    {
        float startDeg = centerAngleDeg - sliceAngleDeg * 0.5f;
        float endDeg = centerAngleDeg + sliceAngleDeg * 0.5f;

        int arcResolution = Mathf.Max(highlightArcResolution, 2);
        int totalPoints = arcResolution + 2; // 중심 -> 호(arcResolution개 점) -> 중심
        line.positionCount = totalPoints;

        Vector3 center = transform.position;
        line.SetPosition(0, center);

        for (int i = 0; i < arcResolution; i++)
        {
            float t = (float)i / (arcResolution - 1);
            float rad = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, t);
            Vector3 point = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * highlightArcRadius;
            line.SetPosition(i + 1, point);
        }

        line.SetPosition(totalPoints - 1, center);
    }

    // ----------------- 밀기 (위/아래) -----------------

    public void ShiftAlongLine(int direction)
    {
        if (IsShifting) return;
        if (ringsInnerToOuter == null || ringsInnerToOuter.Length == 0) return;
        StartCoroutine(ShiftRoutine(direction));
    }

    private IEnumerator ShiftRoutine(int direction)
    {
        IsShifting = true;

        List<RingSlot> lineSlots = ComposeLineSlots(currentDiameterIndex);
        List<RingSlot> changedSlots = BattleRing.ShiftOccupantsInList(lineSlots, direction, 1);

        var movers = LDY_SlotMoveAnimator.BuildMovers(changedSlots);
        yield return LDY_SlotMoveAnimator.Animate(movers, moveDuration, moveEase);

        IsShifting = false;
    }

    // 실제 지름선을 따라가는 물리적 순서로 슬롯을 이어붙인다: 바깥쪽(diameterIndex 쪽 가장자리) -> ... ->
    // 안쪽(diameterIndex 쪽) -> 안쪽(반대쪽, 중심을 사이에 두고 가장 가까움) -> ... -> 바깥쪽(반대쪽 가장자리).
    // 이래야 한 칸 밀 때 각 occupant가 바로 옆 링으로만 자연스럽게 넘어가고(안쪽 두 개가 중심에서 만나는 지점에서만
    // 링을 건너뜀), 양쪽 바깥 가장자리끼리 갑자기 화면을 가로질러 점프하지 않는다.
    private List<RingSlot> ComposeLineSlots(int diameterIndex)
    {
        var lineSlots = new List<RingSlot>(ringsInnerToOuter.Length * 2);
        int segmentCount = ringsInnerToOuter[0].Ring.SlotCount;
        int oppositeIndex = (diameterIndex + segmentCount / 2) % segmentCount;

        for (int r = ringsInnerToOuter.Length - 1; r >= 0; r--)
        {
            lineSlots.Add(ringsInnerToOuter[r].Ring.GetSlot(diameterIndex)); // 바깥 -> 안쪽 (이쪽 편)
        }
        for (int r = 0; r < ringsInnerToOuter.Length; r++)
        {
            lineSlots.Add(ringsInnerToOuter[r].Ring.GetSlot(oppositeIndex)); // 안쪽 -> 바깥 (반대편)
        }

        return lineSlots;
    }
}

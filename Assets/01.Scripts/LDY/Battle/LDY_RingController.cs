using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 오리가미킹 스타일 회전 패널(링) 컨트롤러. 2D 탑뷰 프로젝트 기준: 슬롯은 XY 평면에 배치된다.
// - 슬롯의 worldPosition은 BattleRing 생성 시 고정이고, 회전 시 BattleRing.ShiftOccupants로 occupant 참조만 재배치한다.
//   Transform.RotateAround 등으로 오브젝트 자체를 궤도로 굴리는 방식은 쓰지 않는다.
// - 새로 배정받은 슬롯의 occupant만 골라 현재 위치 -> 새 슬롯 위치로 Lerp 이동시킨다 (DOTween 붙일 거면 RotateRoutine 내부만 교체하면 됨).
// - 링 하나당 컨트롤러 하나. 탭 판정은 이 컴포넌트가 직접 하지 않는다 - 동심원으로 여러 링의 콜라이더가
//   겹칠 때 유니티의 OnMouseDown 매직 메서드에만 맡기면 어느 링이 눌렸는지 판정이 불안정하기 때문에,
//   LDY_RingSelectionManager가 매 프레임 Physics2D.OverlapPointAll로 클릭 지점의 모든 콜라이더를 모은 뒤
//   "반지름이 가장 작은(=가장 안쪽/가장 구체적인) 링"을 직접 골라 Select()를 호출해준다.
// Collider2D는 추상 타입이라 RequireComponent가 자동으로 못 붙여줌(AddComponent가 null을 반환하며 조용히 실패함) ->
// 구체 타입(CircleCollider2D)을 요구해서 탭 판정용 콜라이더가 항상 확실히 붙어있게 한다.
[RequireComponent(typeof(CircleCollider2D))]
public class LDY_RingController : MonoBehaviour
{
    [Header("식별자")]
    [SerializeField] private string ringId = "Ring_0";

    [Header("타일 배치 방식")]
    [SerializeField] private LDY_RingShape shape = LDY_RingShape.Circle;
    [SerializeField] private LDY_RingPlane plane = LDY_RingPlane.XY; // 2D 탑뷰: 화면의 가로/세로 = X/Y
    [SerializeField] private Transform center;

    [Header("원형 설정")]
    [SerializeField] private float circleRadius = 3f;
    [SerializeField] private int circleTileCount = 8;

    [Header("사각형 설정")]
    [SerializeField] private float squareHalfSize = 3f;
    [SerializeField] private int squareTilesPerSide = 4;

    [Header("일자형 줄(Line) 설정 - 오리가미킹의 위/아래(또는 좌/우)로 미는 패널")]
    [SerializeField] private LDY_LineOrientation lineOrientation = LDY_LineOrientation.Vertical;
    [SerializeField] private int lineSlotCount = 6;
    [SerializeField] private float lineSpacing = 1.5f;

    [Header("수동 타일 앵커 (비어있지 않으면 위 절차적 설정 대신 이 Transform들의 위치를 순서대로 사용)")]
    [SerializeField] private List<Transform> manualAnchors = new List<Transform>();

    [Header("회전 연출")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // 회전할 때 이 트랜스폼(파이 휠 배경 그림)도 슬롯 한 칸 각도만큼 같이 돌려서, 적이 조용히 칸만
    // 옮기는 게 아니라 "판 자체가 도는" 것처럼 보이게 한다. 타일 위치/콜라이더는 전혀 건드리지 않고
    // 오직 이 비주얼만 회전시킨다(비워두면 회전 연출 없이 지금처럼 동작).
    [SerializeField] private Transform wheelVisualTransform;

    // 선택되면 활성화되는 노란 테두리 표시 오브젝트(안쪽 경계선 + 바깥쪽 경계선 두 개를 자식으로 담는 루트, 비워둬도 무방).
    // 도넛 모양 링은 두 경계선이 다 강조돼야 "이 두 줄 사이 칸 전체"가 움직인다는 게 명확해진다.
    [Header("선택 강조 표시")]
    [SerializeField] private GameObject selectionHighlightRoot;

    public event Action<LDY_RingController> OnRotationComplete;
    public event Action<bool> OnSelectionChanged; // true = 선택됨, false = 선택 해제됨 (선택 하이라이트 연출용 훅)

    public BattleRing Ring { get; private set; }
    public bool IsRotating { get; private set; }
    public bool IsSelected { get; private set; }

    // 이 링의 탭 판정 반지름. LDY_RingSelectionManager가 동심원끼리 겹칠 때 "가장 작은 반지름" 기준으로
    // 어느 링을 클릭했는지 고른다 (SetTapRadius로 세팅, 기본값은 이 콜라이더의 초기 radius).
    public float TapRadius { get; private set; }

    private Camera cam;
    private Vector3 ringCenterWorld;
    private CircleCollider2D tapCollider;

    private void Awake()
    {
        cam = Camera.main;
        tapCollider = GetComponent<CircleCollider2D>();
        TapRadius = tapCollider != null ? tapCollider.radius : 0f;
        if (selectionHighlightRoot != null) selectionHighlightRoot.SetActive(false);
        BuildRing();
    }

    // 씬 생성 툴 등에서 동심원 레이어 간격에 맞게 탭 판정 반지름을 지정할 때 사용.
    public void SetTapRadius(float radius)
    {
        TapRadius = radius;
        if (tapCollider == null) tapCollider = GetComponent<CircleCollider2D>();
        if (tapCollider != null) tapCollider.radius = radius;
    }

    // ----------------- 슬롯 생성 -----------------

    private void BuildRing()
    {
        // 기본형(원형 + XY평면 + 수동 앵커 없음)은 BattleRing이 center/radius/segmentCount로부터
        // 직접 극좌표를 계산하게 한다 (요구사항 2의 계산식을 BattleRing 안에 그대로 둔다).
        if (IsPlainPolarRing())
        {
            Vector3 origin = center != null ? center.position : transform.position;
            ringCenterWorld = origin;
            Ring = new BattleRing(ringId, origin, circleRadius, circleTileCount);
            return;
        }

        // 사각형/축을 XZ로 쓰는 경우/수동 앵커 등은 좌표 목록을 미리 계산해서 넘겨준다.
        List<Vector3> positions = ComputeTilePositions();
        ringCenterWorld = ComputeCentroid(positions);
        Ring = new BattleRing(ringId, positions);
    }

    private bool IsPlainPolarRing()
    {
        return shape == LDY_RingShape.Circle
            && plane == LDY_RingPlane.XY
            && (manualAnchors == null || manualAnchors.Count == 0);
    }

    // 현재 Inspector 설정 기준으로 타일 월드 좌표를 계산한다. Play 모드가 아니어도(에디터 툴에서) 호출 가능.
    public List<Vector3> ComputeTilePositions()
    {
        return (manualAnchors != null && manualAnchors.Count > 0)
            ? BuildPositionsFromAnchors()
            : BuildProceduralPositions();
    }

    private List<Vector3> BuildPositionsFromAnchors()
    {
        var positions = new List<Vector3>(manualAnchors.Count);
        foreach (Transform t in manualAnchors)
        {
            if (t != null) positions.Add(t.position);
        }
        return positions;
    }

    private List<Vector3> BuildProceduralPositions()
    {
        Vector3 origin = center != null ? center.position : transform.position;
        switch (shape)
        {
            case LDY_RingShape.Circle:
                return GenerateCirclePositions(origin, circleRadius, circleTileCount);
            case LDY_RingShape.Line:
                return GenerateLinePositions(origin, lineSpacing, lineSlotCount, lineOrientation);
            default:
                return GenerateSquarePositions(origin, squareHalfSize, squareTilesPerSide);
        }
    }

    // 슬롯을 일자로 늘어놓는다. ShiftOccupants는 이 순서를 원형처럼(모듈로) 순환시키므로,
    // 오리가미킹의 줄 패널처럼 끝까지 밀면 반대쪽 끝에서 다시 나타나는 식으로 동작한다.
    private List<Vector3> GenerateLinePositions(Vector3 origin, float spacing, int count, LDY_LineOrientation orientation)
    {
        var positions = new List<Vector3>(Mathf.Max(count, 0));
        float totalLength = spacing * (count - 1);
        float start = -totalLength * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float offsetAlongLine = start + spacing * i;
            Vector3 offset = orientation == LDY_LineOrientation.Vertical
                ? new Vector3(0f, offsetAlongLine, 0f)
                : new Vector3(offsetAlongLine, 0f, 0f);
            positions.Add(origin + offset);
        }
        return positions;
    }

    private List<Vector3> GenerateCirclePositions(Vector3 origin, float radius, int count)
    {
        var positions = new List<Vector3>(Mathf.Max(count, 0));
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f / count) * i;
            Vector3 offset = plane == LDY_RingPlane.XZ
                ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius
                : new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            positions.Add(origin + offset);
        }
        return positions;
    }

    // 사각형 테두리를 시계 방향으로 한 바퀴 돌면서(모서리 중복 없이) 타일을 배치한다.
    private List<Vector3> GenerateSquarePositions(Vector3 origin, float halfSize, int tilesPerSide)
    {
        tilesPerSide = Mathf.Max(tilesPerSide, 2);
        Vector3[] corners = new Vector3[4];
        if (plane == LDY_RingPlane.XZ)
        {
            corners[0] = origin + new Vector3(-halfSize, 0f, -halfSize);
            corners[1] = origin + new Vector3(halfSize, 0f, -halfSize);
            corners[2] = origin + new Vector3(halfSize, 0f, halfSize);
            corners[3] = origin + new Vector3(-halfSize, 0f, halfSize);
        }
        else
        {
            corners[0] = origin + new Vector3(-halfSize, -halfSize, 0f);
            corners[1] = origin + new Vector3(halfSize, -halfSize, 0f);
            corners[2] = origin + new Vector3(halfSize, halfSize, 0f);
            corners[3] = origin + new Vector3(-halfSize, halfSize, 0f);
        }

        var positions = new List<Vector3>();
        for (int side = 0; side < 4; side++)
        {
            Vector3 from = corners[side];
            Vector3 to = corners[(side + 1) % 4];
            for (int i = 0; i < tilesPerSide - 1; i++)
            {
                float t = (float)i / (tilesPerSide - 1);
                positions.Add(Vector3.Lerp(from, to, t));
            }
        }
        return positions;
    }

    private Vector3 ComputeCentroid(List<Vector3> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            return center != null ? center.position : transform.position;
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 p in positions) sum += p;
        return sum / positions.Count;
    }

    // ----------------- 선택 -----------------

    // LDY_RingSelectionManager가 클릭 지점에서 이 링을 골랐을 때, 또는 UI 버튼의 OnClick에서 호출.
    public void Select()
    {
        if (IsSelected) return;
        IsSelected = true;
        if (selectionHighlightRoot != null) selectionHighlightRoot.SetActive(true);
        OnSelectionChanged?.Invoke(true);
    }

    public void Deselect()
    {
        if (!IsSelected) return;
        IsSelected = false;
        if (selectionHighlightRoot != null) selectionHighlightRoot.SetActive(false);
        OnSelectionChanged?.Invoke(false);
    }

    // ----------------- 스와이프 방향 판정 (LDY_RingSelectionManager에서 호출) -----------------

    public Vector3 GetWorldPointOnScreenRay(Vector2 screenPosition)
    {
        if (cam == null) cam = Camera.main;
        Vector3 normal = plane == LDY_RingPlane.XZ ? Vector3.up : Vector3.back;
        Plane mathPlane = new Plane(normal, ringCenterWorld);
        Ray ray = cam.ScreenPointToRay(screenPosition);
        return mathPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ringCenterWorld;
    }

    // 드래그 시작점 -> 중심을 잇는 반지름 벡터에 수직인 접선(tangent) 방향과 드래그 이동 벡터를 내적해서
    // 회전 방향(+1/-1)을 판정한다. 사각형 링에도 그대로 적용 가능 (중심 기준 접선 근사).
    public int ComputeSwipeDirection(Vector3 grabWorld, Vector3 dragDelta)
    {
        Vector3 normal = plane == LDY_RingPlane.XZ ? Vector3.up : Vector3.back;
        Vector3 radial = grabWorld - ringCenterWorld;
        if (radial.sqrMagnitude < 0.0001f) radial = Vector3.right;
        radial.Normalize();

        Vector3 tangent = Vector3.Cross(normal, radial);
        float dot = Vector3.Dot(dragDelta, tangent);
        return dot >= 0f ? 1 : -1;
    }

    // ----------------- 회전 -----------------

    // direction: +1 시계 방향, -1 반시계 방향. LDY_RingSelectionManager(방향키/스와이프)나
    // UI 버튼 등 어디서든 호출 가능한 공개 진입점.
    public void Rotate(int direction, int steps = 1)
    {
        if (IsRotating || Ring == null || Ring.SlotCount == 0) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.MapRotate);
        }

        StartCoroutine(RotateRoutine(direction, steps));
    }

    private IEnumerator RotateRoutine(int direction, int steps)
    {
        IsRotating = true; // 애니메이션 중에는 Rotate 재호출이 전부 무시되어 중복 회전을 막는다.

        // 1) 데이터부터 먼저 shift: 실제 오브젝트는 건드리지 않고 어떤 슬롯이 어떤 occupant를 새로 받았는지만 계산
        List<RingSlot> changedSlots = Ring.ShiftOccupants(direction, steps);

        // 판 비주얼 회전은 occupant 이동과 같은 시간 동안 병렬로(따로) 돌아가게 시작만 해두고 기다리지 않는다.
        if (wheelVisualTransform != null) StartCoroutine(RotateWheelVisual(direction, steps));

        // 2) 새로 배정받은 슬롯만 골라서 occupant를 (현재 위치 -> 새 슬롯 worldPosition)으로 이동
        var movers = LDY_SlotMoveAnimator.BuildMovers(changedSlots);
        yield return LDY_SlotMoveAnimator.Animate(movers, moveDuration, moveEase);

        IsRotating = false;
        OnRotationComplete?.Invoke(this);
    }

    // 파이 휠 배경 그림을 슬롯 한 칸 각도(direction*steps만큼)만큼 회전시킨다. 타일 좌표/콜라이더는
    // 전혀 안 건드리므로 탭 판정이나 occupant 위치 계산에는 아무 영향이 없다 - 순수 시각 효과.
    // direction=+1(반시계) -> +Z 오일러 회전, direction=-1(시계) -> -Z 오일러 회전 (기존 각도 공식과 동일한 방향 규칙).
    private IEnumerator RotateWheelVisual(int direction, int steps)
    {
        float sliceAngle = 360f / Mathf.Max(Ring.SlotCount, 1);
        float totalAngle = direction * sliceAngle * steps;

        Quaternion startRot = wheelVisualTransform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveEase.Evaluate(Mathf.Clamp01(elapsed / moveDuration));
            wheelVisualTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        wheelVisualTransform.localRotation = endRot;
    }

    // ----------------- 정렬(콤보) 체크 -----------------

    public List<GameObject> GetLongestAlignedRun(Func<GameObject, bool> predicate) => Ring.GetLongestAlignedRun(predicate);

    public bool IsAligned(int requiredRun, Func<GameObject, bool> predicate) => Ring.IsAligned(predicate, requiredRun);

    public bool IsEnemyAlignedByTag(int requiredRun, string enemyTag = "Enemy") => Ring.IsAligned(go => go.CompareTag(enemyTag), requiredRun);

    private void OnDrawGizmosSelected()
    {
        if (Ring == null) return;
        Gizmos.color = Color.cyan;
        foreach (RingSlot slot in Ring.slots)
        {
            Gizmos.DrawWireSphere(slot.worldPosition, 0.15f);
        }
    }
}

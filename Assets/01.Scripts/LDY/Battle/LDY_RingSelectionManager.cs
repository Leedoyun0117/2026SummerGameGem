using UnityEngine;
using UnityEngine.InputSystem;

// 씬에 하나 존재하는 "현재 선택된 것" 라우터. 원형 링(LDY_RingController) 또는 지름선(LDY_RadialLineController)
// 둘 중 하나를 선택 상태로 들고 있다가, 방향키/스와이프 입력을 그 선택된 대상에게만 라우팅해준다.
//
// 사용 흐름: 링을 탭(화면 클릭)하거나 숫자키로 먼저 선택 -> 그 다음 방향키 입력은 전부 여기서 받아서
// 지금 선택된 대상에게만 넘겨준다. 이렇게 해야 여러 링이 동시에 있어도 선택 안 된 것은 절대 반응하지 않는다.
//
// 이 프로젝트는 Active Input Handling이 New Input System Package로 설정되어 있어서
// 예전 UnityEngine.Input 클래스 대신 Keyboard.current / Mouse.current로 입력을 읽는다.
//
// 탭 판정(원형 링 전용): 동심원처럼 여러 링의 콜라이더가 겹칠 수 있으므로, 각 링의 OnMouseDown에 맡기지 않고
// 여기서 클릭 지점의 모든 콜라이더를 Physics2D.OverlapPointAll로 모은 뒤 "반지름이 가장 작은
// (=가장 안쪽/가장 구체적인) 링"을 직접 골라 선택한다.
public class LDY_RingSelectionManager : MonoBehaviour
{
    public static LDY_RingSelectionManager Instance { get; private set; }

    // 원형 링은 좌/우가 회전, 지름선(RadialLine)은 좌/우가 "어느 지름선인지 고르기" + 위/아래가 "그 줄 밀기"로
    // 서로 의미가 달라서 HandleKeyInput/HandleRadialKeyInput에서 각각 다르게 해석한다.
    [Header("방향키 입력")]
    [SerializeField] private Key rotateCwKey = Key.RightArrow;
    [SerializeField] private Key rotateCcwKey = Key.LeftArrow;
    [SerializeField] private Key shiftForwardKey = Key.DownArrow;
    [SerializeField] private Key shiftBackwardKey = Key.UpArrow;

    [Header("스와이프 입력 (원형 링 선택 시에만 - 화면 아무 곳에서나 드래그 가능)")]
    [SerializeField] private float swipeThreshold = 0.5f;

    [Header("무기 조준 중 공격 확정 키")]
    [SerializeField] private Key attackKey = Key.Space;

    public LDY_RingController SelectedRing { get; private set; }
    public LDY_RadialLineController SelectedRadialLine { get; private set; }

    private Camera cam;
    private bool isDragging;
    private Vector3 dragStartWorld;

    // 숫자키 1~4 = Ring_Layer1~4, 5번 = 지름선(RadialLine) 선택.
    private static readonly Key[] NumberKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
    };

    private static readonly Key[] NumpadKeys =
    {
        Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5,
        Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
    };

    private const int RadialLineNumberKeyIndex = 4; // 5번 키(인덱스 4)

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;

        // 예전 버전(KeyCode 타입)에서 저장된 값이 씬에 남아있으면 Key enum 범위를 벗어날 수 있어서
        // (예: KeyCode.RightArrow=275는 Key enum에 없는 값) 유효하지 않으면 기본값으로 되돌린다.
        if (!System.Enum.IsDefined(typeof(Key), rotateCwKey)) rotateCwKey = Key.RightArrow;
        if (!System.Enum.IsDefined(typeof(Key), rotateCcwKey)) rotateCcwKey = Key.LeftArrow;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return; // 키보드/마우스 장치가 아직 연결 안 됨

        // 무기가 선택되어 조준 중이면 방향키는 전부 공격 대상 커서를 옮기는 데 쓰고,
        // 링 탭/회전/지름선 입력은 전혀 받지 않는다(서로 입력이 겹치면 안 되므로 최우선으로 분기).
        if (LDY_AttackTargetController.Instance != null && LDY_AttackTargetController.Instance.IsTargeting)
        {
            HandleAttackTargetingInput();
            isDragging = false;
            return;
        }

        HandleTapSelection();
        HandleNumberKeySelection();

        if (SelectedRadialLine != null)
        {
            if (!SelectedRadialLine.IsShifting) HandleRadialKeyInput();
            isDragging = false;
            return; // 지름선 선택 중에는 스와이프(링 회전용) 입력은 받지 않는다.
        }

        if (SelectedRing == null || SelectedRing.IsRotating)
        {
            isDragging = false;
            return;
        }

        HandleKeyInput();
        HandleSwipeInput();
    }

    // ----------------- 선택 -----------------

    public void SelectRing(LDY_RingController ring)
    {
        if (SelectedRing == ring && SelectedRadialLine == null) return;

        DeselectAll();
        SelectedRing = ring;
        SelectedRing?.Select();
        isDragging = false;
    }

    public void SelectRadialLine(LDY_RadialLineController radial)
    {
        if (SelectedRadialLine == radial) return;

        DeselectAll();
        SelectedRadialLine = radial;
        SelectedRadialLine?.Select();
        isDragging = false;
    }

    public void Deselect()
    {
        DeselectAll();
    }

    private void DeselectAll()
    {
        SelectedRing?.Deselect();
        SelectedRing = null;
        SelectedRadialLine?.Deselect();
        SelectedRadialLine = null;
    }

    // 숫자키 1~4로 안쪽부터 바깥쪽 순서의 링을, 5로 지름선(RadialLine)을 바로 선택한다.
    // 탭 판정과 별개로 항상 확실하게 동작하는 선택 수단.
    // 이미 선택되어 있는 것과 같은 숫자를 다시 누르면 선택을 취소(토글)한다.
    private void HandleNumberKeySelection()
    {
        for (int i = 0; i < NumberKeys.Length; i++)
        {
            bool pressed = Keyboard.current[NumberKeys[i]].wasPressedThisFrame
                || Keyboard.current[NumpadKeys[i]].wasPressedThisFrame;
            if (!pressed) continue;

            if (i == RadialLineNumberKeyIndex)
            {
                LDY_RadialLineController radial = LDY_BattleBoardManager.Instance != null
                    ? LDY_BattleBoardManager.Instance.RadialLine
                    : null;

                if (radial != null)
                {
                    if (SelectedRadialLine == radial) Deselect();
                    else SelectRadialLine(radial);
                }
            }
            else
            {
                LDY_RingController ring = GetRingByLayerIndex(i);
                if (ring != null)
                {
                    if (SelectedRing == ring) Deselect();
                    else SelectRing(ring);
                }
            }
            return;
        }
    }

    // LDY_BattleBoardManager에 등록된 순서(=하이러키에 생성된 순서, Ring_Layer1, 2, 3...)를 그대로 인덱스로 쓴다.
    private LDY_RingController GetRingByLayerIndex(int index)
    {
        if (LDY_BattleBoardManager.Instance == null) return null;
        var rings = LDY_BattleBoardManager.Instance.Rings;
        return index >= 0 && index < rings.Count ? rings[index] : null;
    }

    // 마우스를 누른 지점에 겹쳐 있는 모든 링 콜라이더 중 반지름이 가장 작은 것을 골라 선택한다.
    // 아무 링도 없는 빈 공간을 클릭하면 기존 선택은 그대로 유지된다(선택 해제하지 않음).
    private void HandleTapSelection()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 worldPoint = GetMouseWorldXY();
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        LDY_RingController best = null;
        float bestRadius = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            LDY_RingController ring = hit.GetComponent<LDY_RingController>();
            if (ring == null) continue;
            if (ring.TapRadius < bestRadius)
            {
                bestRadius = ring.TapRadius;
                best = ring;
            }
        }

        if (best != null) SelectRing(best);
    }

    // 마우스 스크린 좌표를 카메라 기준 z=0 평면(보드가 놓인 평면) 위의 월드 좌표로 변환한다.
    private Vector2 GetMouseWorldXY()
    {
        Vector3 screen = Mouse.current.position.ReadValue();
        screen.z = -cam.transform.position.z; // 카메라 ~ z=0 평면까지의 거리
        return cam.ScreenToWorldPoint(screen);
    }

    // 원형 링이 선택된 상태에서의 방향키: 네 방향 다 인식.
    // 슬롯 인덱스가 증가하는 방향(+1)이 화면상 반시계 방향이라서(각도 계산상 인덱스 증가 = 반시계),
    // "오른쪽=시계, 왼쪽=반시계"가 되려면 오른쪽/아래는 -1, 왼쪽/위는 +1을 넘겨야 한다.
    private void HandleKeyInput()
    {
        if (IsKeyPressedThisFrame(rotateCwKey) || IsKeyPressedThisFrame(shiftForwardKey)) SelectedRing.Rotate(-1);
        else if (IsKeyPressedThisFrame(rotateCcwKey) || IsKeyPressedThisFrame(shiftBackwardKey)) SelectedRing.Rotate(1);
    }

    // 지름선이 선택된 상태에서의 방향키: 좌/우 = 어느 지름선인지 고르기, 위/아래 = 그 줄 밀기.
    private void HandleRadialKeyInput()
    {
        if (IsKeyPressedThisFrame(rotateCwKey)) SelectedRadialLine.CycleDiameter(1);
        else if (IsKeyPressedThisFrame(rotateCcwKey)) SelectedRadialLine.CycleDiameter(-1);
        else if (IsKeyPressedThisFrame(shiftForwardKey)) SelectedRadialLine.ShiftAlongLine(1);
        else if (IsKeyPressedThisFrame(shiftBackwardKey)) SelectedRadialLine.ShiftAlongLine(-1);
    }

    // 무기 조준 중일 때의 방향키: 좌/우 = 세그먼트(칸) 이동, 위/아래 = 링(행) 이동. 스페이스바 = 공격 확정.
    // 링 회전과 같은 이유로 오른쪽은 -1(시계 방향으로 이동), 왼쪽은 +1(반시계 방향으로 이동)을 넘긴다.
    private void HandleAttackTargetingInput()
    {
        var attack = LDY_AttackTargetController.Instance;

        if (IsKeyPressedThisFrame(attackKey))
        {
            attack.ExecuteAttack();
            attack.ClearWeapon();
            return;
        }

        if (IsKeyPressedThisFrame(rotateCwKey)) attack.MoveCursor(0, -1);
        else if (IsKeyPressedThisFrame(rotateCcwKey)) attack.MoveCursor(0, 1);
        else if (IsKeyPressedThisFrame(shiftForwardKey)) attack.MoveCursor(1, 0);
        else if (IsKeyPressedThisFrame(shiftBackwardKey)) attack.MoveCursor(-1, 0);
    }

    // Key enum 범위를 벗어난 값(예전 KeyCode 데이터가 잘못 넘어온 경우)이 들어와도 크래시 없이 무시한다.
    private static bool IsKeyPressedThisFrame(Key key)
    {
        if (!System.Enum.IsDefined(typeof(Key), key)) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }

    // 선택된 링 기준으로 방향을 판정하되, 드래그 자체는 화면 어디서 시작하든 상관없다
    // (탭으로 이미 어떤 링을 회전시킬지 정했기 때문).
    private void HandleSwipeInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragStartWorld = SelectedRing.GetWorldPointOnScreenRay(Mouse.current.position.ReadValue());
        }
        else if (Mouse.current.leftButton.isPressed && isDragging)
        {
            Vector3 current = SelectedRing.GetWorldPointOnScreenRay(Mouse.current.position.ReadValue());
            Vector3 delta = current - dragStartWorld;
            if (delta.magnitude < swipeThreshold) return;

            int direction = SelectedRing.ComputeSwipeDirection(dragStartWorld, delta);
            isDragging = false; // 한 번의 스와이프는 한 칸만 회전시키고 소비한다 (중복 트리거 방지)
            SelectedRing.Rotate(direction);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}

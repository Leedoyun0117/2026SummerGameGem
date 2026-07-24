using TMPro;
using UnityEngine;

public class KTH_TurnManager : MonoBehaviour
{
    public static KTH_TurnManager Instance { get; private set; }

    public enum TurnState
    {
        PlayerInput,    // 플레이어 링 조작 턴
        PlayerMove,     // 플레이어 이동 진행 중 (입력 차단)
        BossTurn        // 보스 공격 턴 (입력 차단)
    }

    [Header("참조 스크립트")]
    [SerializeField] private KTH_BossTurn bossTurn;
    [SerializeField] private KTH_PlayerMovement playerMovement;

    [Header("턴 설정")]
    [Tooltip("플레이어가 이만큼 움직이면 이동 단계로 넘어감")]
    [SerializeField] private int movesPerPlayerTurn = 1;

    [Header("UI")]
    [SerializeField] private TMP_Text remainingTurnText;
    [SerializeField] private string remainingTurnFormat = "남은 턴 : {0}";

    private int moveCount;
    public int MoveCount => moveCount;
    public int MovesPerPlayerTurn => movesPerPlayerTurn;
    public int RemainingTurns => Mathf.Max(0, movesPerPlayerTurn - moveCount);

    private TurnState currentState = TurnState.PlayerInput;
    public TurnState CurrentState => currentState;

    private bool wasRingRotating;
    private bool wasRadialShifting;

    // 🔥 플레이어 시작 위치 및 회전 저장 변수
    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateRemainingTurnUI();

        // 🔥 게임 시작 시 플레이어의 최초 위치/회전값 저장
        if (playerMovement != null)
        {
            playerStartPosition = playerMovement.transform.position;
            playerStartRotation = playerMovement.transform.rotation;
        }
    }

    private void Update()
    {
        // 플레이어 조작 상태가 아니거나 남은 턴이 0 이하인 경우 입력 차단
        if (currentState != TurnState.PlayerInput || RemainingTurns <= 0) return;
        if (LDY_RingSelectionManager.Instance == null) return;

        var ring = LDY_RingSelectionManager.Instance.SelectedRing;
        var radial = LDY_RingSelectionManager.Instance.SelectedRadialLine;

        bool ringIsRotating = ring != null && ring.IsRotating;
        bool radialIsShifting = radial != null && radial.IsShifting;

        bool ringStartedRotating = ringIsRotating && !wasRingRotating;
        bool radialStartedShifting = radialIsShifting && !wasRadialShifting;

        if (ringStartedRotating || radialStartedShifting)
        {
            OnPlayerMoved();
        }

        wasRingRotating = ringIsRotating;
        wasRadialShifting = radialIsShifting;
    }

    private void OnPlayerMoved()
    {
        moveCount++;
        Debug.Log($"[TurnManager] 플레이어 조작 {moveCount}/{movesPerPlayerTurn}");

        UpdateRemainingTurnUI();

        // 턴을 모두 소비했을 때
        if (moveCount >= movesPerPlayerTurn)
        {
            currentState = TurnState.PlayerMove;

            if (remainingTurnText != null)
                remainingTurnText.text = string.Format(remainingTurnFormat, 0);

            Debug.Log("[TurnManager] 턴 소진 -> 링 선택 매니저 비활성화 및 이동 시작");

            if (LDY_RingSelectionManager.Instance != null)
            {
                var selectedRing = LDY_RingSelectionManager.Instance.SelectedRing;
                if (selectedRing != null) selectedRing.Deselect();

                LDY_RingSelectionManager.Instance.enabled = false;
            }

            // 🔥 이동 시작 전, 현재(시작) 위치를 다시 한번 확실하게 저장
            if (playerMovement != null)
            {
                playerStartPosition = playerMovement.transform.position;
                playerStartRotation = playerMovement.transform.rotation;

                playerMovement.StartMovement();
            }
        }
    }

    /// <summary>
    /// 최종 콜라이더 충돌 시 또는 보스 턴 종료 시 호출
    /// </summary>
    public void NextTurn()
    {
        switch (currentState)
        {
            // 플레이어 이동 중 최종 콜라이더 충돌 -> 보스 턴 시작
            case TurnState.PlayerMove:
            case TurnState.PlayerInput:
                currentState = TurnState.BossTurn;
                Debug.Log("[TurnManager] 최종 콜라이더 충돌! -> 보스 턴 시작");

                if (bossTurn != null)
                {
                    bossTurn.TakeTurn();
                }
                break;

            // 보스 턴 종료 -> 다시 플레이어 조작 턴으로 복귀, 위치 리셋 및 턴 충전
            case TurnState.BossTurn:
                currentState = TurnState.PlayerInput;
                moveCount = 0; // 턴 충전
                wasRingRotating = false;
                wasRadialShifting = false;

                // 🔥 보스 턴 종료 -> 플레이어 턴으로 돌아올 때 새로운 적 랜덤 스폰!
                KTH_RandomEnemySpawner spawner = FindFirstObjectByType<KTH_RandomEnemySpawner>();
                if (spawner != null)
                {
                    spawner.OnMyTurnSpawn();
                }

                // 링 선택 매니저 다시 활성화
                if (LDY_RingSelectionManager.Instance != null)
                {
                    LDY_RingSelectionManager.Instance.enabled = true;
                }

                UpdateRemainingTurnUI();
                break;
        }
    }

    private void UpdateRemainingTurnUI()
    {
        if (remainingTurnText == null) return;
        remainingTurnText.text = string.Format(remainingTurnFormat, RemainingTurns);
    }
}

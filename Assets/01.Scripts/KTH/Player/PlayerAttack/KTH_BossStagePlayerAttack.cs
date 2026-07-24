using UnityEngine;
using DG.Tweening;

// 🔥 [핵심] PlayerMovement가 같은 트리거 이벤트에서 tile.UseTile()을 호출해 tile.IsUsed를 true로
// 만들어버리기 전에, 이 스크립트의 OnTriggerEnter2D가 먼저 실행되도록 순서를 강제합니다.
// (값이 작을수록 먼저 실행됨. 기본 실행 순서는 0이므로 -100이면 항상 먼저 돕니다)
[DefaultExecutionOrder(-100)]
public class KTH_BossStagePlayerAttack : MonoBehaviour
{
    [Header("보스 참조")]
    [SerializeField] private KTH_BossHealthSystem bossHealth;
    [Tooltip("링(층) 판정의 기준 중심점. 비워두면 bossHealth의 위치를 중심으로 사용합니다.\n보스 트랜스폼이 링 중심과 안 맞는 경우, 실제 보드 중심 오브젝트를 여기에 직접 꽂아주세요.")]
    [SerializeField] private Transform boardCenterOverride;

    [Header("망치 데이터 / 데미지 설정")]
    [SerializeField] private int baseDamage = 20;

    [Header("공격 판정 설정 (보스 중심 링/층 기반)")]
    [Tooltip("보스를 중심으로 몇 번째 링(층)까지 유효타로 인정할지. 예: 2면 1층+2층 모두 유효")]
    [SerializeField] private int validLayerCount = 2;
    [Tooltip("보스를 중심으로 한 각 링(층)의 바깥쪽 경계 반지름. 안쪽 링(1층)부터 순서대로 입력하세요.\n예) 1층 경계 2, 2층 경계 4, 3층 경계 6, 4층 경계 8 → [2, 4, 6, 8]")]
    [SerializeField] private float[] ringOuterRadii = new float[] { 2f, 4f, 6f, 8f };

    [Header("연출 설정 (전부 선택 사항, 비워도 동작함)")]
    [Tooltip("망치 스프라이트/오브젝트. 비워두면 대기 시간만큼만 기다렸다가 판정")]
    [SerializeField] private Transform hammerVisual;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private float swingAngle = 70f;
    [SerializeField] private float impactPunchScale = 0.25f;
    [SerializeField] private float impactShakeDuration = 0.2f;
    [SerializeField] private float impactShakeStrength = 0.3f;

    [Header("자체 감지 설정 (PlayerMovement 호출 없이 동작)")]
    [Tooltip("체크 해제 시 PlayerMovement가 SetHammerActive(true)를 호출해줘야만 동작. 체크하면 이 스크립트가 스스로 어택 타일 진입을 감지함")]
    [SerializeField] private bool selfDetectAttackTile = true;

    [Header("SPACE UI")]
    [SerializeField] private GameObject spaceObject;

    [Header("히트 이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;
    [SerializeField] private float effectDestroyTime = 2f;

    private float finalDamage;
    private bool canUseHammer = false;
    private bool isAttacking = false; // 스윙 연출 도중 중복 입력 방지
    public bool CanUseHammer => canUseHammer;

    /// <summary>
    /// 🔥 [핵심 수정] 링(층) 판정/기즈모의 기준 중심점을 한 곳에서 계산합니다.
    /// 우선순위: boardCenterOverride > bossHealth > (에디터 한정) 씬에서 보스 재탐색 > 이 오브젝트 자신
    /// 기존에는 bossHealth가 null이면 바로 transform.position(자기 자신)으로 빠져서,
    /// Edit 모드(Start()가 아직 안 돈 상태)에서 기즈모가 엉뚱한 곳에 그려지는 문제가 있었습니다.
    /// </summary>
    private Vector3 GetBoardCenter()
    {
        if (boardCenterOverride != null)
        {
            return boardCenterOverride.position;
        }

        if (bossHealth != null)
        {
            return bossHealth.transform.position;
        }

#if UNITY_EDITOR
        // Edit 모드에서는 Start()가 실행되지 않으므로 기즈모용으로 한 번 더 찾아봅니다.
        if (!Application.isPlaying)
        {
            KTH_BossHealthSystem foundBoss = FindFirstObjectByType<KTH_BossHealthSystem>();
            if (foundBoss != null)
            {
                return foundBoss.transform.position;
            }
        }
#endif

        return transform.position;
    }

    private void Start()
    {
        if (bossHealth == null)
        {
            bossHealth = FindFirstObjectByType<KTH_BossHealthSystem>();
        }

        ApplyItemDamageBonus();

        if (spaceObject != null)
        {
            spaceObject.SetActive(false);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
            return;

        Transform spawn = effectSpawnPoint != null ? effectSpawnPoint : bossHealth.transform;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            spawn.position,
            Quaternion.identity);

        Destroy(effect, effectDestroyTime);
    }

    /// <summary>
    /// 🔥 [핵심] PlayerMovement가 이미 트리거 콜라이더로 "지금 어떤 타일 위에 있는지"를 관리하고 있으므로,
    /// 같은 콜라이더의 Enter/Exit 이벤트를 이 스크립트에서도 그대로 받아서 재사용합니다.
    /// (한 오브젝트에 붙은 여러 컴포넌트의 OnTrigger 콜백은 유니티가 각각 독립적으로 호출해줍니다)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!selfDetectAttackTile || isAttacking)
            return;

        KTH_Tile tile = other.GetComponentInParent<KTH_Tile>();
        if (tile == null)
            return;

        if (tile.CurrentTileType != TileType.Attack)
            return;

        if (!IsWithinValidLayer(tile.transform.position))
        {
            SetHammerActive(false);
            return;
        }

        SetHammerActive(true);
    }

    /// <summary>
    /// 어택 타일 위에서 벗어나는 순간 canUseHammer를 다시 false로 되돌립니다.
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!selfDetectAttackTile || isAttacking) return;

        KTH_Tile tile = other.GetComponentInParent<KTH_Tile>();
        if (tile == null || tile.CurrentTileType != TileType.Attack) return;

        SetHammerActive(false);
    }

    private void ApplyItemDamageBonus()
    {
        if (JCY_RunProgress.Instance != null)
        {
            finalDamage = baseDamage + JCY_RunProgress.Instance.explosionDamageBonus;
        }
        else
        {
            finalDamage = baseDamage;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (isAttacking) return; // 스윙 중에는 재입력 무시

        Debug.Log($"[1. 키 입력 확인] 스페이스바 누름! | 현재 canUseHammer: {canUseHammer} | bossHealth 존재 여부: {bossHealth != null}");

        if (canUseHammer)
        {
            ExecuteHammerAttack();
        }
        else
        {
            Debug.LogError("[실패 원인] canUseHammer가 false입니다! SetHammerActive(true)가 정상 호출되었는지, 혹은 다른 곳에서 바로 false로 꺼버리지 않았는지 확인하세요.");
        }
        if (!isAttacking && selfDetectAttackTile)
        {
            bool canAttack = IsWithinValidLayer(transform.position);

            if (canAttack != canUseHammer)
            {
                SetHammerActive(canAttack);
            }
        }
    }

    public void ExecuteHammerAttack()
    {
        Debug.Log("[2. 공격 함수 진입] ExecuteHammerAttack 시작");

        if (!canUseHammer || isAttacking)
        {
            Debug.LogError("[공격 중단] canUseHammer가 false이거나 이미 공격 중입니다.");
            return;
        }

        if (bossHealth == null)
        {
            bossHealth = FindFirstObjectByType<KTH_BossHealthSystem>();
            if (bossHealth == null)
            {
                Debug.LogError("[공격 중단] 씬에서 KTH_BossHealthSystem을 찾을 수 없습니다!");
                return;
            }
        }

        // 입력을 즉시 잠그고(canUseHammer=false) 스윙 연출을 시작
        SetHammerActive(false);
        isAttacking = true;

        RotateTowardsBoss();
        PlayHammerSwing();
    }

    /// <summary>
    /// 🔨 망치 스윙 연출 → 판정 → 턴 종료까지 이어지는 시퀀스
    /// hammerVisual이 없으면 스윙 시간만큼 대기 후 바로 판정합니다.
    /// </summary>
    private void PlayHammerSwing()
    {
        Sequence seq = DOTween.Sequence();

        if (hammerVisual != null)
        {
            hammerVisual.localRotation = Quaternion.identity;
            seq.Append(hammerVisual.DOPunchRotation(new Vector3(0f, 0f, -swingAngle), swingDuration, 1, 0f));
        }
        else
        {
            seq.AppendInterval(swingDuration);
        }

        seq.AppendCallback(ResolveHammerHit);
        seq.AppendCallback(FinishAttack);
    }

    private void ResolveHammerHit()
    {
        bool isHit = CheckLayerAttackHit();

        if (isHit)
        {
            
            bossHealth.TakeDamage(finalDamage);
            SpawnHitEffect();
            Debug.Log($"💥 [성공] 망치 공격 성공! 데미지: {finalDamage}");

            if (hammerVisual != null)
            {
                hammerVisual.DOPunchScale(Vector3.one * impactPunchScale, 0.2f, 6, 0.7f);
            }

            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(impactShakeDuration, impactShakeStrength);
            }
        }
        else
        {
            Debug.LogWarning("🛡️ [빗나감] 공격 범위 내에 보스가 없습니다.");
        }
    }

    private void FinishAttack()
    {
        isAttacking = false;
        SetHammerActive(false);

        if (KTH_TurnManager.Instance != null)
        {
            KTH_TurnManager.Instance.NextTurn();
        }
        else
        {
            Debug.LogWarning("TurnManager Instance가 null입니다.");
        }
    }

    public void SetHammerActive(bool isActive)
    {
        Debug.Log($"SetHammerActive 호출됨 : {isActive}");

        canUseHammer = isActive;

        if (spaceObject != null)
        {
            Debug.Log($"spaceObject : {spaceObject.name}");
            spaceObject.SetActive(isActive);
        }
        else
        {
            Debug.LogError("spaceObject가 비어있음!");
        }
    }

    private void RotateTowardsBoss()
    {
        if (bossHealth == null) return;

        Vector3 dirToBoss = (bossHealth.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(dirToBoss.y, dirToBoss.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 🔥 [핵심] 보스를 중심으로 플레이어가 몇 번째 링(층)에 서 있는지 계산해서,
    /// 그 층이 validLayerCount 이내면 무조건 유효타로 판정합니다. (각도는 보지 않음)
    /// </summary>
    private bool CheckLayerAttackHit()
    {
        if (bossHealth == null) return false;

        bool isValid = IsWithinValidLayer(transform.position);

        int playerLayer = GetLayerIndexFromCenter(transform.position, GetBoardCenter());
        Debug.Log($"[레이어 판정] 플레이어 위치 = {playerLayer}층 | 유효 범위 = 1~{validLayerCount}층 | 결과: {(isValid ? "✅ 유효타" : "❌ 무효")}");

        return isValid;
    }

    /// <summary>
    /// 주어진 월드 좌표가 보스 기준 유효 층(1 ~ validLayerCount) 안에 있는지 확인합니다.
    /// OnTriggerEnter2D의 활성화 조건과 CheckLayerAttackHit의 판정 조건에서 공통으로 사용합니다.
    /// </summary>
    private bool IsWithinValidLayer(Vector3 worldPos)
    {
        int layer = GetLayerIndexFromCenter(worldPos, GetBoardCenter());
        return layer >= 1 && layer <= validLayerCount;
    }

    /// <summary>
    /// 중심(보스)으로부터의 거리로 몇 번째 링(층)에 있는지 계산합니다.
    /// ringOuterRadii는 안쪽 링부터 순서대로 "바깥쪽 경계 반지름"을 담고 있어야 합니다.
    /// 1층부터 시작하며, 모든 경계를 벗어나면 (판 밖) ringOuterRadii.Length + 1을 반환합니다.
    /// </summary>
    private int GetLayerIndexFromCenter(Vector3 worldPos, Vector3 centerPos)
    {
        float distance = Vector2.Distance(worldPos, centerPos);

        for (int i = 0; i < ringOuterRadii.Length; i++)
        {
            if (distance <= ringOuterRadii[i])
            {
                return i + 1; // 1층부터 시작
            }
        }

        return ringOuterRadii.Length + 1; // 마지막 링보다 바깥 = 판정 밖
    }

    /// <summary>
    /// 🎨 망치 준비 상태일 때 Scene/Game 뷰에 보스를 중심으로 한 링(층) 기즈모 표시
    /// 유효 판정 층은 초록색, 그 외는 자홍색으로 구분됩니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!canUseHammer && !Application.isPlaying) return;
        if (ringOuterRadii == null || ringOuterRadii.Length == 0) return;

        Vector3 centerPos = GetBoardCenter();

        for (int i = 0; i < ringOuterRadii.Length; i++)
        {
            bool isValidLayer = (i + 1) <= validLayerCount;
            Gizmos.color = isValidLayer ? new Color(0f, 1f, 0.3f, 0.7f) : new Color(1f, 0f, 1f, 0.35f);
            DrawGizmoCircle(centerPos, ringOuterRadii[i]);
        }
    }

    private void DrawGizmoCircle(Vector3 center, float radius, int segments = 48)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float rad = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
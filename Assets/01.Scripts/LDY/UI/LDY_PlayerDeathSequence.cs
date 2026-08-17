using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이어 사망 연출 (마리오 오디세이 스타일):
// ① 화면이 플레이어 위치만 남기고 검게 좁아짐(자체 구현 아이리스 - LDY_SceneTransition에 기대지 않음, 맵 씬을
//    거치지 않고 전투 씬만 단독으로 Play해도 항상 동작해야 하기 때문) → ② 그 구멍 안에서 플레이어가 살짝
//    튀었다 떨어짐 → ③ 2초 뒤 구멍이 마저 메워져 완전히 검게 됨 → ④ "Defeat" 텍스트 페이드인 → ⑤ 지정한
//    씬으로 이동.
// KTH_PlayerHealth.OnPlayerDied를 구독해서 죽었을 때 한 번만 재생된다.
public class LDY_PlayerDeathSequence : MonoBehaviour
{
    [SerializeField] private KTH_PlayerHealth playerHealth;

    [Header("아이리스 구멍의 중심이자 살짝 튀었다 떨어지는 연출 대상 - 원하는 오브젝트를 직접 끌어다 놓으면 됨 " +
        "(UI RectTransform이든 월드 스프라이트 Transform이든 상관없음). 비워두면 씬의 LDY_PlayerHealthUI 하트 이미지를 대신 씀")]
    [SerializeField] private Transform playerImage;

    [Header("검은 원형 아이리스 (RawImage 하나 필요 - 전체화면을 덮게 배치)")]
    [SerializeField] private RawImage irisOverlay;
    [SerializeField] private Color irisColor = Color.black;
    [SerializeField] private int textureSize = 256;
    [Tooltip("전체 화면이 안 가려진 상태로 취급할 시작 반경 (화면 대각선보다 커야 함)")]
    [SerializeField] private float openRadius = 1.5f;
    [SerializeField] private float startRadius = 0.16f;
    [SerializeField] private float edgeSoftness = 0.015f;
    [Tooltip("openRadius에서 startRadius(플레이어만 보이는 구멍)까지 좁혀지는 데 걸리는 시간")]
    [SerializeField] private float fillInDuration = 0.5f;
    [Tooltip("구멍이 열린 채로 유지되는 시간 - 그 사이에 플레이어가 살짝 튀었다 떨어짐")]
    [SerializeField] private float holdDuration = 2f;
    [Tooltip("startRadius에서 0(완전히 메워짐)까지 좁혀지는 데 걸리는 시간")]
    [SerializeField] private float closeDuration = 1f;
    [SerializeField] private float updatesPerSecond = 24f;

    [Header("구멍 안에서 살짝 튀었다 떨어지는 연출 (마리오 오디세이 느낌)")]
    [SerializeField] private float riseHeight = 10f;
    [SerializeField] private float riseDuration = 0.15f;
    [SerializeField] private float fallDistance = 400f;
    [SerializeField] private float fallDuration = 0.5f;

    [Header("Defeat 연출 중 가려야 할 전투 UI (무기 패널, 턴 타이머 등) - 사망 시 자동으로 비활성화됨. " +
        "무기 조준 하이라이트는 여기 넣지 않아도 LDY_AttackTargetController.ClearWeapon()으로 자동 정리됨")]
    [SerializeField] private GameObject[] hideOnDeath;

    [Header("Defeat 텍스트 및 이동할 씬")]
    [Tooltip("Defeat 캔버스를 씬에 기본 비활성화 상태로 놔두고 싶으면 그 캔버스(또는 최상위) GameObject를 연결 - " +
        "사망 시 자동으로 켜준다. 비워두면 defeatGroup이 붙은 오브젝트 자신을 켬")]
    [SerializeField] private GameObject defeatCanvasRoot;
    [SerializeField] private CanvasGroup defeatGroup;
    [SerializeField] private TextMeshProUGUI defeatText;
    [SerializeField] private float defeatFadeDuration = 0.5f;
    [SerializeField] private float defeatHoldDuration = 1.5f;
    [SerializeField] private string destinationSceneName = "LDY_MapScene";

    private Texture2D texture;
    private Color32[] pixels;

    private void Awake()
    {
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        pixels = new Color32[textureSize * textureSize];

        if (irisOverlay != null)
        {
            irisOverlay.texture = texture;
            irisOverlay.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (playerHealth == null) playerHealth = KTH_PlayerHealth.Instance;

        if (playerImage == null)
        {
            LDY_PlayerHealthUI healthUI = FindFirstObjectByType<LDY_PlayerHealthUI>();
            if (healthUI != null) playerImage = healthUI.HeartRectTransform;
        }

        if (playerHealth != null) playerHealth.OnPlayerDied += HandlePlayerDied;

        if (defeatGroup != null) defeatGroup.alpha = 0f;

        if (defeatCanvasRoot != null) defeatCanvasRoot.SetActive(false);
        else if (defeatGroup != null) defeatGroup.gameObject.SetActive(false);

        if (defeatText != null) defeatText.text = "Defeat";
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // 무기 조준 하이라이트(빨간 칸 테두리)가 죽는 순간까지 켜져 있던 상태로 남아있으면 아이리스/Defeat
        // 위로 그대로 겹쳐 보인다 - 사망 연출을 시작하기 전에 확실히 꺼준다.
        if (LDY_AttackTargetController.Instance != null) LDY_AttackTargetController.Instance.ClearWeapon();

        // NoAbility 적의 파편 충전 이펙트(작은 다이아몬드들)도 같은 이유로 애니메이션 없이 즉시 꺼버린다.
        foreach (LDY_FragmentChargeEffect charge in FindObjectsByType<LDY_FragmentChargeEffect>(FindObjectsSortMode.None))
        {
            charge.ForceHide();
        }

        foreach (GameObject go in hideOnDeath)
        {
            if (go != null) go.SetActive(false);
        }

        // 화면 정중앙(0.5, 0.5)을 기준으로 구멍을 만든다 - 플레이어 오브젝트의 실제 스크린 좌표를 추적하려던
        // 방식은 Canvas 종류(Overlay/Camera)나 좌표계에 따라 어긋나기 쉬워서 더 단순하고 확실한 중앙 고정으로 바꿈.
        Vector2 screenUV = new Vector2(0.5f, 0.5f);
        Vector2 originalStartPos = GetCurrentPos();

        yield return StartCoroutine(PlayIrisRoutine(screenUV));

        // 화면이 이미 완전히 검게 덮인 뒤라 안 보임 - 이 오브젝트가 다음 씬까지 유지되는 지속 오브젝트(하트 UI 등)일 수
        // 있어서, 떨어진 채로 남지 않도록 제자리로 되돌려둔다.
        SetPos(originalStartPos);

        if (defeatCanvasRoot != null) defeatCanvasRoot.SetActive(true);
        else if (defeatGroup != null) defeatGroup.gameObject.SetActive(true);

        if (defeatGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(defeatGroup, 0f, 1f, defeatFadeDuration));

        yield return new WaitForSecondsRealtime(defeatHoldDuration);

        // 체력/별의 조각 UI는 DontDestroyOnLoad라서 씬을 옮겨도 안 죽고 다음 화면까지 그대로 따라와 보인다.
        // Destroy하면 다음 판을 시작할 때 다시 만들 방법이 없으니, SetActive(false)로만 꺼서 나중에 재사용한다.
        // destinationSceneName이 다시 플레이 가능한 씬(예: LDY_TestScene)이므로, 죽은 상태(IsDead)와
        // 체력을 여기서 미리 초기화해둔다 - 안 그러면 되돌아간 씬에서 IsDead가 true로 남아 TakeDamage가
        // 계속 무시된다.
        if (KTH_PlayerHealth.Instance != null)
        {
            KTH_PlayerHealth.Instance.ResetForNewRun();
            KTH_PlayerHealth.Instance.transform.root.gameObject.SetActive(false);
        }
        if (LDY_StarPieceManager.Instance != null) LDY_StarPieceManager.Instance.transform.root.gameObject.SetActive(false);

        SceneManager.LoadScene(destinationSceneName);
    }

    // 화면이 열린 상태 -> screenUV 주변 구멍만 남기고 좁아짐(fillInDuration) -> 그 구멍을 holdDuration만큼
    // 유지(그 사이에 플레이어가 튀었다 떨어짐) -> 구멍이 마저 메워짐(closeDuration, 완전히 검게).
    private IEnumerator PlayIrisRoutine(Vector2 screenUV)
    {
        if (irisOverlay == null)
        {
            Debug.LogWarning("[LDY_PlayerDeathSequence] irisOverlay(RawImage)가 연결되어 있지 않아 아이리스 없이 진행합니다.", this);
            yield return StartCoroutine(RiseThenFall());
            if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);
            yield break;
        }

        irisOverlay.gameObject.SetActive(true);
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SfxId.Iris);
        }

        Paint(screenUV, openRadius, aspect);
        yield return AnimateRadius(screenUV, openRadius, startRadius, fillInDuration, aspect);

        yield return StartCoroutine(RiseThenFall());

        float remainingHold = holdDuration - riseDuration - fallDuration;
        if (remainingHold > 0f) yield return new WaitForSecondsRealtime(remainingHold);

        yield return AnimateRadius(screenUV, startRadius, 0f, closeDuration, aspect);
    }

    private IEnumerator RiseThenFall()
    {
        if (playerImage == null) yield break;

        Vector2 startPos = GetCurrentPos();
        Vector2 risePos = startPos + Vector2.up * riseHeight;
        Vector2 fallPos = startPos + Vector2.down * fallDistance;

        float t = 0f;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            SetPos(Vector2.Lerp(startPos, risePos, EaseOutQuad(Mathf.Clamp01(t / riseDuration))));
            yield return null;
        }
        SetPos(risePos);

        t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            SetPos(Vector2.Lerp(risePos, fallPos, EaseInQuad(Mathf.Clamp01(t / fallDuration))));
            yield return null;
        }
        SetPos(fallPos);
    }

    // UI 오브젝트(RectTransform)면 anchoredPosition을, 월드 스프라이트면 localPosition을 쓴다 - UI에 position(월드
    // 좌표)을 직접 넣으면 Canvas 스케일 때문에 anchoredPosition이 극단적인 값으로 튀는 문제가 있다
    // (LDY_StarPieceManager에서 겪었던 것과 동일한 버그라 반드시 구분해서 다뤄야 한다).
    private Vector2 GetCurrentPos()
    {
        if (playerImage == null) return Vector2.zero;
        RectTransform rt = playerImage as RectTransform;
        return rt != null ? rt.anchoredPosition : (Vector2)playerImage.localPosition;
    }

    private void SetPos(Vector2 pos)
    {
        if (playerImage == null) return;
        RectTransform rt = playerImage as RectTransform;
        if (rt != null) rt.anchoredPosition = pos;
        else playerImage.localPosition = new Vector3(pos.x, pos.y, playerImage.localPosition.z);
    }

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInQuad(float t) => t * t;

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }

    // ----------------- 아이리스 텍스처 그리기 (LDY_SceneTransition과 동일한 방식: 셰이더 없이 순수 C#으로
    // 원형 구멍이 뚫린 텍스처를 직접 그림 - 매 프레임이 아니라 초당 updatesPerSecond 만큼만 다시 그려 가볍게 동작) -----------------

    private IEnumerator AnimateRadius(Vector2 center, float fromRadius, float toRadius, float animDuration, float aspect)
    {
        if (animDuration <= 0f)
        {
            Paint(center, toRadius, aspect);
            yield break;
        }

        float t = 0f;
        float updateInterval = 1f / Mathf.Max(updatesPerSecond, 1f);
        float sinceLastUpdate = 0f;

        while (t < animDuration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            sinceLastUpdate += dt;

            if (sinceLastUpdate >= updateInterval)
            {
                sinceLastUpdate = 0f;
                float radius = Mathf.Lerp(fromRadius, toRadius, Mathf.Clamp01(t / animDuration));
                Paint(center, radius, aspect);
            }

            yield return null;
        }

        Paint(center, toRadius, aspect);
    }

    private void Paint(Vector2 center, float radius, float aspect)
    {
        Color32 baseColor = irisColor;

        for (int y = 0; y < textureSize; y++)
        {
            float v = y / (float)(textureSize - 1);
            float dy = v - center.y;

            for (int x = 0; x < textureSize; x++)
            {
                float u = x / (float)(textureSize - 1);
                float dx = (u - center.x) * aspect;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01((dist - radius) / Mathf.Max(edgeSoftness, 0.0001f));
                pixels[y * textureSize + x] = new Color32(baseColor.r, baseColor.g, baseColor.b, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }
}

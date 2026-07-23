using UnityEngine;
using UnityEngine.UI;

// 노드 하나를 나타내는 UI 버튼. 사각 패널은 없고 아이콘(별) + 뒤에서 번지는 글로우 + 현재 위치를
// 감싸는 링(테두리 빛)만 보여줌. Background는 화면에는 안 보이고 클릭 판정 영역으로만 쓰임
// 색은 타입이 아니라 "상태"로만 결정 (팔레트 3톤 유지: 잠김=무채색, 진행중=밝은 중립색+골드 링, 클리어=골드)
public class LDY_MapNodeView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage; // 현재 위치를 감싸는 골드 링 (겉테두리 빛)
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cornerStarImage;

    [Header("Type Icons (모양으로만 구분, 색은 사용하지 않음)")]
    [SerializeField] private Sprite startIcon;
    [SerializeField] private Sprite battleIcon;
    [SerializeField] private Sprite eventIcon;
    [SerializeField] private Sprite shopIcon;
    [SerializeField] private Sprite bossIcon;

    [Header("아이콘 크기 (부모 대비 채우는 비율)")]
    [Range(0.5f, 1f)] [SerializeField] private float iconFillRatio = 0.9f;

    public RectTransform RectTransform => (RectTransform)transform;
    public int NodeIndex { get; private set; }

    private LDY_MapManager manager;
    private LDY_MapNode nodeData;
    private LDY_MapTheme theme;

    private bool isPulsing;
    private bool isRingActive;
    private float phaseOffset;
    private Color pulseBaseColor;

    public void Initialize(LDY_MapManager manager, LDY_MapNode node, int index, LDY_MapTheme theme)
    {
        this.manager = manager;
        this.theme = theme;
        nodeData = node;
        NodeIndex = index;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        RectTransform.anchoredPosition = node.position;
        button.onClick.AddListener(HandleClick);

        // 별을 크게: 아이콘이 부모 박스를 거의 꽉 채우도록 인셋을 넓힘
        if (iconImage != null)
        {
            RectTransform iconRt = iconImage.rectTransform;
            float margin = (1f - iconFillRatio) * 0.5f;
            iconRt.anchorMin = new Vector2(margin, margin);
            iconRt.anchorMax = new Vector2(1f - margin, 1f - margin);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
        }

        // Background는 투명한 클릭 판정 영역으로만 사용 (사각 패널 없음)
        if (backgroundImage != null) backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        if (cornerStarImage != null) cornerStarImage.gameObject.SetActive(false);

        if (borderImage != null && borderImage.sprite == null)
            borderImage.sprite = LDY_ProceduralSprite.Ring;

        if (glowImage != null && glowImage.sprite == null)
            glowImage.sprite = LDY_ProceduralSprite.SoftGlow;

        Refresh();
    }

    public void Refresh()
    {
        if (nodeData == null || theme == null) return;

        if (iconImage != null) iconImage.sprite = GetIcon(nodeData.type);

        bool isCurrent = nodeData.isUnlocked && !nodeData.isCleared;
        bool glowActive = nodeData.isCleared || isCurrent;
        bool ringActive = isCurrent; // "겉테두리가 빛나는" 표시는 지금 갈 수 있는 노드에만

        Color iconColor;
        float iconAlpha;

        if (nodeData.isCleared || isCurrent)
        {
            // 아이콘 자체가 이미 타입별로 색이 다르게 그려져 있으므로 흰색(무색 틴트)으로 원래 색 그대로 보여줌
            iconColor = Color.white;
            iconAlpha = 1f; // 갈 수 있는 곳(완료 포함) = 불투명 100
        }
        else
        {
            iconColor = theme.textLocked;
            iconAlpha = theme.lockedOpacity; // 못 가는 곳 = 반투명
        }

        if (iconImage != null)
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconAlpha);

        if (borderImage != null)
            borderImage.gameObject.SetActive(ringActive);

        SetPulse(glowActive, ringActive);

        button.interactable = isCurrent;
    }

    private void SetPulse(bool glowActive, bool ringActive)
    {
        isPulsing = glowActive || ringActive;
        isRingActive = ringActive;

        if (isPulsing)
        {
            float intensity = theme.glowHdrIntensity;
            Color typeColor = theme.GetTypeGlowColor(nodeData.type);
            pulseBaseColor = new Color(typeColor.r * intensity, typeColor.g * intensity, typeColor.b * intensity, 1f);
        }
        else
        {
            if (glowImage != null) glowImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void Update()
    {
        if (!isPulsing || theme == null) return;

        float wave = (Mathf.Sin(Time.time * theme.glowPulseSpeed + phaseOffset) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(theme.glowMinAlpha, theme.glowMaxAlpha, wave);

        if (glowImage != null)
        {
            Color gc = pulseBaseColor;
            gc.a = alpha;
            glowImage.color = gc;
        }

        if (isRingActive && borderImage != null)
        {
            Color rc = pulseBaseColor;
            rc.a = alpha;
            borderImage.color = rc;
        }
    }

    private void HandleClick()
    {
        manager.OnNodeClicked(NodeIndex);
    }

    private Sprite GetIcon(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Start: return startIcon;
            case LDY_NodeType.Battle: return battleIcon;
            case LDY_NodeType.Event: return eventIcon;
            case LDY_NodeType.Shop: return shopIcon;
            case LDY_NodeType.Boss: return bossIcon;
            default: return null;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Setting_B : MonoBehaviour
{
    // static이 아니면 인스턴스마다 자기만의 instance 필드를 가져서 아래 중복 방지 로직이 항상 무력화된다 -
    // 그 결과 씬을 오갈 때마다 이 설정창(및 DontDestroyOnLoad된 _panel)이 계속 쌓여서, 오래된 중복
    // 인스턴스가 파괴된 _panel을 들고 있다가 ESC를 누르면 MissingReferenceException이 났다.
    public static Setting_B instance;
    [Header("설정 패널")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _soundPanel;

    [Header("애니메이션 설정")]
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip _clickSound;

    private bool _isOpen = false;
    private bool _isAnimating = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        instance = this;
        GameObject root = transform.root.gameObject;
        // DontDestroyOnLoad는 "최상위(루트)" 오브젝트에만 걸 수 있다 - 이 스크립트가 다른 오브젝트의
        // 자식에 붙어있으면 gameObject 그대로는 예외가 나서(그 뒤 초기화가 전부 씹힘) transform.root를 쓴다.
        DontDestroyOnLoad(root);

        // _panel/_soundPanel은 EscManager 프리팹 안에 없고, 그 씬 자체의 공용 UI Canvas 밑 깊숙이(다른
        // 스토리/튜토리얼 UI와 같이) 꽂혀있었다 - 그래서 transform.root를 그냥 영속시키면 그 씬의 다른
        // UI까지 통째로 안 죽게 되어버린다. 대신 EscManager 전용 Canvas를 하나 만들어서 패널들을 여기로
        // 옮겨 붙인 뒤, 이 Canvas만(EscManager와 함께) 영속시킨다 - 씬의 나머지 UI와는 완전히 분리됨.
        Canvas ownCanvas = root.GetComponentInChildren<Canvas>();
        if (ownCanvas == null)
        {
            GameObject canvasGO = new GameObject("Setting_B Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGO.transform.SetParent(root.transform, false);
            ownCanvas = canvasGO.GetComponent<Canvas>();
            ownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 2000;
        }

        if (_panel != null) _panel.transform.SetParent(ownCanvas.transform, false);
        if (_soundPanel != null) _soundPanel.transform.SetParent(ownCanvas.transform, false);

        // 게임 시작 시 설정 창 닫기
        if (_panel != null)
        {
            _panel.transform.localScale = Vector3.zero;
            _panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettingPanel();
        }
    }

    // 설정 버튼과 연결
    public void SettingPanel()
    {
        PlayClickSound();
        OpenSettingPanel();
    }

    // 닫기 버튼과 연결
    public void SettingPanelClose()
    {
        PlayClickSound();
        CloseSettingPanel();
    }

    // ESC 키로 열기/닫기
    private void ToggleSettingPanel()
    {
        if (_isAnimating)
            return;

        PlayClickSound();

        if (_isOpen)
        {
            CloseSettingPanel();
        }
        else
        {
            OpenSettingPanel();
        }
    }

    private void OpenSettingPanel()
    {
        // 이미 열려 있거나 애니메이션 중이면 실행하지 않음
        if (_isOpen || _isAnimating)
            return;

        if (_panel == null)
        {
            Debug.LogWarning("[Setting_B] _panel이 비어있어서(이 씬에 연결 안 됨) 설정창을 열 수 없습니다.");
            return;
        }

        _isOpen = true;
        _isAnimating = true;

        Time.timeScale = 0;
        _panel.transform.DOKill();
        _panel.SetActive(true);
        _panel.transform.localScale = Vector3.zero;

        _panel.transform
            .DOScale(Vector3.one, _duration)
            .SetEase(_openEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _isAnimating = false;
            });
    }

    private void CloseSettingPanel()
    {
        // 이미 닫혀 있거나 애니메이션 중이면 실행하지 않음
        if (!_isOpen || _isAnimating)
            return;

        if (_panel == null) return;

        _isOpen = false;
        _isAnimating = true;

        Time.timeScale = 1;
        _panel.transform.DOKill();

        _panel.transform
            .DOScale(Vector3.zero, _duration)
            .SetEase(_closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _panel.SetActive(false);
                _isAnimating = false;
            });
    }

    public void OpenSoundPanel()
    {
        if (_soundPanel == null) return;

        _soundPanel.SetActive(true);
        _soundPanel.transform.localScale = Vector3.zero;

        _soundPanel.transform
            .DOScale(Vector3.one, _duration)
            .SetEase(_openEase)
            .SetUpdate(true);
            
    }

    public void CloseSoundPanel()
    {
        if (_soundPanel == null) return;

        _soundPanel.transform.DOKill();

        _soundPanel.transform
            .DOScale(Vector3.zero, _duration)
            .SetEase(_closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _soundPanel.SetActive(false);
            });
    }

    private void PlayClickSound()
    {
        if (_clickSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(_clickSound);
        }
    }

    private void OnDestroy()
    {
        if (_panel != null)
        {
            _panel.transform.DOKill();
        }
    }
}
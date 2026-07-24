using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Setting_B : MonoBehaviour
{
    [Header("설정 패널")]
    [SerializeField] private GameObject _panel;

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
        // 게임 시작 시 설정 창 닫기
        _panel.transform.localScale = Vector3.zero;
        _panel.SetActive(false);
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

        _isOpen = true;
        _isAnimating = true;

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

        _isOpen = false;
        _isAnimating = true;

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
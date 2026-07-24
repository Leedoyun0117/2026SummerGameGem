using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class Title_Button : MonoBehaviour
{
    public GameObject _setPanel;

    [Header("애니메이션 설정")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InOutBack;

    [Header("사운드 설정")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private bool checker = false;      // 패널이 열려있는지 여부
    private bool isAnimating = false;  // 애니메이션 진행 중인지 여부 (중복 클릭 방지)

    private void Awake()
    {
        // AudioSource가 인스펙터에서 연결 안 되어 있으면 자동으로 붙여서 사용
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    private void PlayClickSound()
    {
        if (clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void GameStart()
    {
        PlayClickSound();
        SceneManager.LoadScene("HTY_Story");
    }

    public void GameSet()
    {
        PlayClickSound();
        SettingPanel();
    }
    public void GameSetClose()
    {
        PlayClickSound();
        SettingPanelClose();
    }

    public void GameExit()
    {
        PlayClickSound();
        Application.Quit();
    }

    private void SettingPanel()
    {
        // 이미 열려있거나, 애니메이션 진행 중이면 무시
        if (checker || isAnimating) return;

        isAnimating = true;
        checker = true;

        _setPanel.transform.DOKill();
        _setPanel.SetActive(true);
        _setPanel.transform.localScale = Vector3.zero;

        _setPanel.transform.DOScale(Vector3.one, duration)
            .SetEase(ease)
            .OnComplete(() => isAnimating = false);
    }

    private void SettingPanelClose()
    {
        // 이미 닫혀있거나, 애니메이션 진행 중이면 무시
        if (!checker || isAnimating) return;

        isAnimating = true;
        checker = false;

        _setPanel.transform.DOKill();
        _setPanel.transform.DOScale(Vector3.zero, duration)
            .SetEase(closeEase)
            .OnComplete(() =>
            {
                _setPanel.SetActive(false);
                isAnimating = false;
            });
    }

}

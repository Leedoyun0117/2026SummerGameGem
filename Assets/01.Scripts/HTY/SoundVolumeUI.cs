using UnityEngine;
using UnityEngine.UI;

public class SoundVolumeUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager가 존재하지 않습니다.");
            return;
        }

        InitializeSliders();

        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void OnDisable()
    {
        bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
    }

    private void InitializeSliders()
    {
        bgmSlider.minValue = 0f;
        bgmSlider.maxValue = 1f;
        bgmSlider.wholeNumbers = false;

        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;
        sfxSlider.wholeNumbers = false;

        bgmSlider.SetValueWithoutNotify(
            SoundManager.Instance.BGMVolume
        );

        sfxSlider.SetValueWithoutNotify(
            SoundManager.Instance.SFXVolume
        );
    }

    private void OnBGMVolumeChanged(float volume)
    {
        SoundManager.Instance.SetBGMVolume(volume);
    }

    private void OnSFXVolumeChanged(float volume)
    {
        SoundManager.Instance.SetSFXVolume(volume);
    }
}
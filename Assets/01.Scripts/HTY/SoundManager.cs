using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public float BGMVolume => bgmSource.volume;
    public float SFXVolume => sfxSource.volume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolume();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("재생할 BGM이 없습니다.");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("재생할 효과음이 없습니다.");
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void SetBGMVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);

        bgmSource.volume = clampedVolume;

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, clampedVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);

        sfxSource.volume = clampedVolume;

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, clampedVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolume()
    {
        float savedBGMVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        bgmSource.volume = savedBGMVolume;
        sfxSource.volume = savedSFXVolume;
    }
}
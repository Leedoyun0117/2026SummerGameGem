using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public float BGMVolume
    {
        get
        {
            if (bgmSource == null)
                return 1f;

            return bgmSource.volume;
        }
    }

    public float SFXVolume
    {
        get
        {
            if (sfxSource == null)
                return 1f;

            return sfxSource.volume;
        }
    }

    private void Awake()
    {
        // 이미 SoundManager가 존재하면 새로 만들어진 것은 삭제
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 변경되어도 SoundManager 유지
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
        {
            Debug.LogError("SoundManager: BGM AudioSource가 연결되지 않았습니다.", this);
        }

        if (sfxSource == null)
        {
            Debug.LogError("SoundManager: SFX AudioSource가 연결되지 않았습니다.", this);
        }

        LoadVolume();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager: 재생할 BGM이 없습니다.");
            return;
        }

        if (bgmSource == null)
        {
            Debug.LogError("SoundManager: BGM AudioSource가 없습니다.");
            return;
        }

        // 이미 같은 BGM이 재생 중이면 처음부터 다시 재생하지 않음
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager: 재생할 효과음이 없습니다.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogError("SoundManager: SFX AudioSource가 없습니다.");
            return;
        }

        float clampedVolumeScale = Mathf.Clamp01(volumeScale);

        // 여러 효과음을 겹쳐서 재생할 수 있음
        sfxSource.PlayOneShot(clip, clampedVolumeScale);
    }

    public void StopAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource == null)
            return;

        float clampedVolume = Mathf.Clamp01(volume);

        bgmSource.volume = clampedVolume;

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, clampedVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource == null)
            return;

        float clampedVolume = Mathf.Clamp01(volume);

        sfxSource.volume = clampedVolume;

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, clampedVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolume()
    {
        float savedBGMVolume =
            PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);

        float savedSFXVolume =
            PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        if (bgmSource != null)
        {
            bgmSource.volume = savedBGMVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = savedSFXVolume;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
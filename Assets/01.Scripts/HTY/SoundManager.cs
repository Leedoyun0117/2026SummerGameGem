using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Serializable]
    private struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
    }

    [Header("SFX Library")]
    [SerializeField] private List<SfxEntry> sfxEntries = new List<SfxEntry>();

    private readonly Dictionary<SfxId, AudioClip> sfxLibrary = new Dictionary<SfxId, AudioClip>();

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

        BuildSfxLibrary();

        LoadVolume();
    }

    public void PlayBGM(AudioClip clip)
    {
        Debug.Log(clip.ToString());
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

        Debug.Log("Instance = " + Instance.gameObject.name);
        Debug.Log("This = " + gameObject.name);
        Debug.Log("AudioSource = " + bgmSource.gameObject.name);
        Debug.Log("Before = " + (bgmSource.clip == null ? "NULL" : bgmSource.clip.name));

        bgmSource.clip = clip;

        Debug.Log("After = " + bgmSource.clip.name);

        bgmSource.Play();

        Debug.Log("Playing = " + bgmSource.isPlaying);
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

    /// <summary>
    /// SfxId로 등록된 효과음을 재생한다. Sfx Entries에 클립이 등록되어 있어야 한다.
    /// </summary>
    public void PlaySFX(SfxId id, float volumeScale = 1f)
    {
        if (!sfxLibrary.TryGetValue(id, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"SoundManager: '{id}'에 등록된 효과음 클립이 없습니다.");
            return;
        }

        PlaySFX(clip, volumeScale);
    }

    private void BuildSfxLibrary()
    {
        sfxLibrary.Clear();

        foreach (SfxEntry entry in sfxEntries)
        {
            if (entry.clip == null)
                continue;

            if (!sfxLibrary.TryAdd(entry.id, entry.clip))
            {
                Debug.LogWarning($"SoundManager: '{entry.id}' 효과음이 Sfx Entries에 중복 등록되어 있습니다.", this);
            }
        }
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
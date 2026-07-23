using UnityEngine;

public class SceneBGMPlayer : MonoBehaviour
{
    [Header("Scene BGM")]
    [SerializeField] private AudioClip bgmClip;

    private void Start()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager가 존재하지 않습니다.");
            return;
        }

        SoundManager.Instance.PlayBGM(bgmClip);
    }
}
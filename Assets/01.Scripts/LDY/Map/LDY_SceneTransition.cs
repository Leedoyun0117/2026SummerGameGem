using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 씬 전환 연출: 클릭한 지점(별)만 남기고 나머지 화면을 검게 가린 뒤, 그 구멍을 duration(기본 1.2초)에 걸쳐
// 완전히 닫고 나서 다음 씬으로 이동. 셰이더 없이 순수 C#으로 원형 구멍이 뚫린 텍스처를 직접 그려서 구현
// (LDY_ProceduralSprite와 같은 방식 - 매 프레임이 아니라 초당 updatesPerSecond 만큼만 다시 그려 가볍게 동작)
public class LDY_SceneTransition : MonoBehaviour
{
    public static LDY_SceneTransition Instance { get; private set; }

    [SerializeField] private RawImage overlay;
    [SerializeField] private int textureSize = 128;
    [SerializeField] private float startRadius = 0.16f;
    [SerializeField] private float edgeSoftness = 0.015f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float updatesPerSecond = 24f;

    private Texture2D texture;
    private Color32[] pixels;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        pixels = new Color32[textureSize * textureSize];

        if (overlay != null)
        {
            overlay.texture = texture;
            overlay.gameObject.SetActive(false);
        }
    }

    // screenUV: 구멍의 중심이 될 화면상 위치 (0~1, 좌하단 기준)
    public void PlayIrisCloseThenLoad(Vector2 screenUV, string sceneName)
    {
        if (isPlaying) return;
        StartCoroutine(PlayRoutine(screenUV, sceneName));
    }

    private IEnumerator PlayRoutine(Vector2 screenUV, string sceneName)
    {
        isPlaying = true;

        if (overlay == null)
        {
            SceneManager.LoadScene(sceneName);
            isPlaying = false;
            yield break;
        }

        overlay.gameObject.SetActive(true);
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        Paint(screenUV, startRadius, aspect);

        float t = 0f;
        float updateInterval = 1f / Mathf.Max(updatesPerSecond, 1f);
        float sinceLastUpdate = 0f;

        while (t < duration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            sinceLastUpdate += dt;

            if (sinceLastUpdate >= updateInterval)
            {
                sinceLastUpdate = 0f;
                float radius = Mathf.Lerp(startRadius, 0f, Mathf.Clamp01(t / duration));
                Paint(screenUV, radius, aspect);
            }

            yield return null;
        }

        Paint(screenUV, 0f, aspect);
        SceneManager.LoadScene(sceneName);
        isPlaying = false;
    }

    private void Paint(Vector2 center, float radius, float aspect)
    {
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
                pixels[y * textureSize + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeOut : MonoBehaviour
{
    [SerializeField] private Image _fadeImage;

    [SerializeField] private float _fadeOutSpeed = 0.05f;
    [SerializeField] private float _fadeOutValue = 0.05f;

    private void Awake()
    {
        Color color = _fadeImage.color;
        color.a = 0f;
        _fadeImage.color = color;
    }

    public void Fade_Out()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        Color color = _fadeImage.color;
        color.a = 0f;
        _fadeImage.color = color;

        while (_fadeImage.color.a < 1f)
        {
            color = _fadeImage.color;
            color.a += _fadeOutValue;
            color.a = Mathf.Clamp01(color.a);

            _fadeImage.color = color;

            yield return new WaitForSecondsRealtime(_fadeOutSpeed);
        }

        color = _fadeImage.color;
        color.a = 1f;
        _fadeImage.color = color;

        if (color.a ==1f)
        {
            SceneManager.LoadScene("HTY_Tutorial");
        }
    }
}
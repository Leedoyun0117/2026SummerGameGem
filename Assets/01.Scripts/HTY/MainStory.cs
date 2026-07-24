using System.Collections;
using TMPro;
using UnityEngine;

public class MainStory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _storyBox;
    [SerializeField] private string[] _storyText;
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private FadeOut _fadeOut;

    

    private void Start()
    {
        StartCoroutine(WaitTime());
    }

    private IEnumerator WaitTime()
    {
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < _storyText.Length; i++)
        {
            yield return StartCoroutine(TypeText(_storyText[i]));//DoText대용 코르틴
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(1f);
        if(_fadeOut!=null)
        {
            _fadeOut.Fade_Out();
        }
    }

    private IEnumerator TypeText(string text)
    {
        _storyBox.text = text;
        _storyBox.maxVisibleCharacters = 0;

        for (int i = 0; i <= text.Length; i++)
        {
            _storyBox.maxVisibleCharacters = i;
            yield return new WaitForSeconds(_typingSpeed);
        }
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class MainStory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _storyBox;
    [SerializeField] private string[] _storyText;
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private float _outTextSpeed = 1;
    [SerializeField] private FadeOut _fadeOut;
    [SerializeField] private Tutorial_Text _next;


    

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
            yield return new WaitForSeconds(_outTextSpeed);
        }
        yield return new WaitForSeconds(1f);

        if(_fadeOut!=null)//페이드아웃 전용
        {
            _fadeOut.Fade_Out();
        }
        else if(_next!=null)
        {
            _next.TextStart();
            _storyBox.SetText("");
        }
        else
        {
            _storyBox.SetText("");
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
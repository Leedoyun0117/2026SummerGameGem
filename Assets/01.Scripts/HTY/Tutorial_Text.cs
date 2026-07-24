using TMPro;
using UnityEngine;
using System.Collections;

public class Tutorial_Text : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _storyBox;
    [SerializeField] private string[] _storyText;
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private float _outTextSpeed = 1;
    [SerializeField] private FadeOut _fadeOut;

    public GameObject _panel1;

    public void TextStart()
    {
        StartCoroutine(WaitTime());
    }

    private IEnumerator WaitTime()
    {
        yield return new WaitForSeconds(1.5f);
        for (int i = 0; i < _storyText.Length; i++)
        {
            if(i>=0&&i<=2)
            {
                _panel1.SetActive(true);
            }
            else
            {
                _panel1.SetActive(false);
            }
            yield return StartCoroutine(TypeText(_storyText[i]));//DoText대용 코르틴
            yield return new WaitForSeconds(_outTextSpeed);
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

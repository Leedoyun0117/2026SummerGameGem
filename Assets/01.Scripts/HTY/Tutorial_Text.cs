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

    [SerializeField] private GameObject _nextTutorial;

    public GameObject _panel1;
    public GameObject _panel2;

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
                _panel2.SetActive(false);
            }
            else if(i>2&&i<=4)
            {
                _panel2.SetActive(true);
                _panel1.SetActive(false);
            }
            else
            {
                _panel1.SetActive(false);
                _panel2.SetActive(false);
            }
            yield return StartCoroutine(TypeText(_storyText[i]));//DoText대용 코르틴
            yield return new WaitForSeconds(_outTextSpeed);
        }
        _nextTutorial.SetActive(true);
        gameObject.SetActive(false);
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

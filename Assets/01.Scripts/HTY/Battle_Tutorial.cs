using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Battle_Tutorial : MonoBehaviour
{
    [SerializeField] private string[] _sent;
    [SerializeField] private TextMeshProUGUI _storyBox;

    [SerializeField] private float _typingInterval = 0.05f;

    private int _page;
    private bool _isTyping;
    private Coroutine _typingCoroutine;

    private void OnEnable()
    {
        Time.timeScale = 0f;

        _page = 0;

        if (_sent == null || _sent.Length == 0)
        {
            FinishTutorial();
            return;
        }

        ShowCurrentPage();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        // 글자가 출력 중이라면 즉시 전체 문장 표시
        if (_isTyping)
        {
            CompleteTyping();
            return;
        }

        // 다음 문장이 남아 있다면 페이지 이동
        if (_page < _sent.Length - 1)
        {
            _page++;
            ShowCurrentPage();
        }
        else
        {
            FinishTutorial();
        }
    }

    private void ShowCurrentPage()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        _typingCoroutine = StartCoroutine(TypeText(_sent[_page]));
    }

    private IEnumerator TypeText(string text)
    {
        _isTyping = true;

        _storyBox.text = text;
        _storyBox.maxVisibleCharacters = 0;

        for (int i = 0; i <= text.Length; i++)
        {
            _storyBox.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(_typingInterval);
        }

        _storyBox.maxVisibleCharacters = text.Length;

        _isTyping = false;
        _typingCoroutine = null;
    }

    private void CompleteTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _storyBox.text = _sent[_page];
        _storyBox.maxVisibleCharacters = _sent[_page].Length;

        _isTyping = false;
    }

    private void FinishTutorial()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
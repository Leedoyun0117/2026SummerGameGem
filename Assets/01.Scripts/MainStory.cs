using System.Collections;
using TMPro;
using UnityEngine;

public class MainStory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _storyBox;
    [SerializeField] private string[] _storyText;

    private void Start()
    {
        StartCoroutine(WaitTime());
    }

    private IEnumerator WaitTime()
    {
        for(int i=0;i<_storyText.Length;i++)
        {
            _storyBox.text= _storyText[i];
            yield return new WaitForSeconds(3f);
        }
    }
}

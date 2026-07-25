using UnityEngine;
using UnityEngine.SceneManagement;

public class Ending : MonoBehaviour
{
    [SerializeField] private float speed = 300f;   // UI라서 픽셀 단위
    [SerializeField] private string nextSceneName;
    [SerializeField] private float maxY;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rect.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        if (rect.anchoredPosition.y >= maxY)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
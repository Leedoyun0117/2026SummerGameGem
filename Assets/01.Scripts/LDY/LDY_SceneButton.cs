using UnityEngine;
using UnityEngine.SceneManagement;

// 버튼 OnClick에 LoadScene()을 연결하면 sceneName에 지정한 씬으로 전환됨.
// 씬 이름은 Build Settings에 등록되어 있어야 함.
public class LDY_SceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LDY_SceneButton] sceneName이 비어 있습니다.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}

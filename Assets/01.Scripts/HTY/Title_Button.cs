using UnityEngine;
using UnityEngine.SceneManagement;

public class Title_Button : MonoBehaviour
{
    public GameObject _setPanel;
    public void GameStart()
    {
        SceneManager.LoadScene("HTY_Story");
    }
    public void GameSet()
    {
        _setPanel.SetActive(true);
    }
    public void GameExit()
    {
        Application.Quit();
    }
}

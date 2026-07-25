using UnityEngine;

// 나가기(Exit) 버튼과 연결. 빌드된 게임에서는 Application.Quit()으로 실제 종료되고,
// 에디터에서는 Application.Quit()이 아무 효과가 없어서 대신 Play 모드를 꺼서 같은 효과를 낸다.
public class Exit_B : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

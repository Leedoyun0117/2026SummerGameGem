using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TimeSkip : MonoBehaviour
{
    public Image _BB;

    void Update()
    {
        if(Keyboard.current.spaceKey.isPressed)
        {
            Time.timeScale = 3;
            _BB.color = Color.white;
        }
        else
        {
            Time.timeScale = 1;
            _BB.color = Color.gray;
        }
    }
}

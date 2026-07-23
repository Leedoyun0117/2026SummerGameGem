using UnityEngine;

public class MoneyManager : MonoBehaviour
{ 
    public static MoneyManager instance;
    public int _money {  get; private set; }


    private void Awake()
    {
        instance = this;
    }

    public void MoneyUP(int value)
    {
        _money += value;
    }
}

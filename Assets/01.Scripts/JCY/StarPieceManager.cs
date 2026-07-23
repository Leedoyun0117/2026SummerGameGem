using TMPro;
using UnityEngine;

public class StarPieceManager : MonoBehaviour
{ 
    public static StarPieceManager instance;
    public int _starPiece {  get; private set; }
    [SerializeField] private TextMeshProUGUI _starUI;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StarPieceUP(int value)
    {
        _starPiece += value;
        _starUI.text = _starPiece.ToString();
    }

    public void StarPieceDown(int value)
    {
        _starPiece -= value;
        _starUI.text = _starPiece.ToString();
    }
}

using TMPro;
using UnityEngine;

public class JCY_ShopManager : MonoBehaviour
{
    public static JCY_ShopManager Instance;
    public GameObject[] _items { get; private set; }
    public GameObject[] _displayPoint { get; private set; }
    public TextMeshProUGUI _displayCost { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayItems()
    {
        for( int i = 0; i < _displayPoint.Length; i++ )
        {
            int index = Random.Range(0 , _items.Length);
            Instantiate(_items[index], _displayPoint[i].transform.position, Quaternion.identity);
            _displayCost[i].text = _items[index].
        }
    }
}

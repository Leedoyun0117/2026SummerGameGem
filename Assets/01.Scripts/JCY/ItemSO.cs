using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "SO/Item")]
public class ItemSO : ScriptableObject
{
   
    public string itemName;

    [Header("status")]
    public int health;
    public float time;

}

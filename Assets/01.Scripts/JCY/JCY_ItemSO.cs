using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "SO/Item")]
public class JCY_ItemSO : ScriptableObject
{
   
    public string itemName;
    public Sprite itemImage;

    [Header("status")]
    public int cost;
    public int health;
    public float time;

   


}

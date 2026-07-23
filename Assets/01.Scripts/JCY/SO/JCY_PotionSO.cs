using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "JCY_SO/Item")]
public class JCY_PotionSO : ScriptableObject
{
   
    public string itemName;
    public Sprite itemImage;

    [Header("status")]
    public int cost;
    public int health;
    public float time;

   


}

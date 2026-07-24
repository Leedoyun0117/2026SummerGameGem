using UnityEngine;

[CreateAssetMenu(fileName = "JCY_ItemSO", menuName = "JCY_SO/JCY_ItemSO")]
public class JCY_ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int cost;
    public int maxHealthUP;
    public int curHealthUP;
    public string Description;
    public int backStarPiece;
    public float maxTimeUP;
}

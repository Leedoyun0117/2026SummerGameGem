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

    [Header("효과 디스패치 (JCY_ItemManager.UseItem에서 이 값으로 분기)")]
    public JCY_ItemEffectType effectType = JCY_ItemEffectType.None;
    [Tooltip("아이템마다 의미가 다른 범용 보조 수치 (예: 무기 데미지 증가량, 인벤토리/포션 한도 증가량)")]
    public int effectAmount;
}

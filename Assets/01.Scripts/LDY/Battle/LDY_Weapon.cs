// 무기 UI의 3칸에 들어가는 무기 하나. 이름 + 공격 범위 모양만 갖는 최소 데이터.
// 나중에 데미지/아이콘 등을 추가하고 싶으면 여기에 필드만 늘리면 된다.
[System.Serializable]
public class LDY_Weapon
{
    public string weaponName;
    public LDY_WeaponAttackShape shape;
}

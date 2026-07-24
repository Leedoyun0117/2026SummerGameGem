using UnityEngine;

// 무기 UI의 3칸에 들어가는 무기 하나. 이름 + 공격 범위 모양 + 설명 텍스트를 갖는다.
// 나중에 데미지/아이콘 등을 추가하고 싶으면 여기에 필드만 늘리면 된다.
[System.Serializable]
public class LDY_Weapon
{
    public string weaponName;
    public LDY_WeaponAttackShape shape;
    [TextArea(2, 4)] public string description;

    [Header("발사 이펙트 (발사 위치 -> 맞은 적까지 이어지는 레이저 등) - 비워두면 기존처럼 맞자마자 즉사")]
    public GameObject hitEffectPrefab;
    [Tooltip("이 이펙트가 재생되는 동안 적이 죽지 않고 버틴다. 발사 이펙트가 없으면 사용되지 않는다.")]
    public float hitEffectDuration = 0.6f;

    [Header("관통(Piercing) - 켜면 대상 칸을 앞->뒤 순서대로 하나씩 처리한다 (예: 활 - 1->2->3->4 순서로 뚫음)")]
    public bool isPiercing;
    [Tooltip("맞은 적의 위치에서 터지는 효과(레이저와는 별개). 예: 활에 맞았을 때 터지는 효과")]
    public GameObject impactEffectPrefab;
    [Tooltip("관통 도중 반사(WeaponReflector)를 만나면 거기서 멈추고 이 효과가 적 위치 -> 발사 위치로 나간다(반사 연출). 비워두면 데미지만 들어가고 이펙트는 없음")]
    public GameObject reflectEffectPrefab;
}

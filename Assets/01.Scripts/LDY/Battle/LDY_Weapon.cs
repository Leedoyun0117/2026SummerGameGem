using UnityEngine;

// 무기 UI의 3칸에 들어가는 무기 하나. 이름 + 공격 범위 모양 + 설명 텍스트를 갖는다.
// 나중에 데미지/아이콘 등을 추가하고 싶으면 여기에 필드만 늘리면 된다.
[System.Serializable]
public class LDY_Weapon
{
    public string weaponName;
    public LDY_WeaponAttackShape shape;
    [TextArea(2, 4)] public string description;

    [Header("보스 전투 전용 데미지 - 사수자리의 활/태양빛 촛불/가시물고기의 뼛조각 아이템이 이 값을 올려준다")]
    public int damage = 10;
    [Tooltip("isForged가 true면(데미지 보너스를 하나라도 얻었으면) description 대신 이 텍스트가 표시됨. 비워두면 description을 계속 씀")]
    [TextArea(2, 4)] public string forgedDescription;
    // 무기 데미지 보너스를 하나라도 적용받았는지 - 씬 시작 시(LDY_WeaponUIController/KTH_BossWeaponUIController)
    // JCY_RunProgress의 보너스 값을 damage에 더하면서 같이 세팅된다. 저장되는 값이 아니라 런타임에만 쓰는 표시.
    [System.NonSerialized] public bool isForged;

    [Header("발사 이펙트 (발사 위치 -> 맞은 적까지 이어지는 레이저 등) - 비워두면 기존처럼 맞자마자 즉사")]
    public GameObject hitEffectPrefab;
    [Tooltip("이 이펙트가 재생되는 동안 적이 죽지 않고 버틴다. 발사 이펙트가 없으면 사용되지 않는다.")]
    public float hitEffectDuration = 0.6f;
    [Tooltip("비관통 무기(예: 검/망치)에서 범위 전체에 맞춰 한 번 띄우는 이펙트의 크기 배율. 이펙트 프리팹 자체가 작게 보이면 여기서 키우면 된다.")]
    public float hitEffectScale = 3f;
    [Tooltip("체크: 이펙트가 보드 중심(0,0)에서 범위 각도로 회전만 해서 나감 - 부채꼴/슬래시처럼 중심에서 뻗어나가는 효과용(예: 검).\n체크 해제: 이펙트가 실제 공격 범위(칸들) 위치에 그대로 생성됨 - 그 자리에서 터지는 효과용(예: 망치).")]
    public bool hitEffectFromOrigin = true;

    [Header("관통(Piercing) - 켜면 대상 칸을 앞->뒤 순서대로 하나씩 처리한다 (예: 활 - 1->2->3->4 순서로 뚫음)")]
    public bool isPiercing;
    [Tooltip("맞은 적의 위치에서 터지는 효과(레이저와는 별개). 예: 활에 맞았을 때 터지는 효과")]
    public GameObject impactEffectPrefab;
    [Tooltip("관통 도중 반사(WeaponReflector)를 만나면 거기서 멈추고 이 효과가 적 위치 -> 발사 위치로 나간다(반사 연출). 비워두면 데미지만 들어가고 이펙트는 없음")]
    public GameObject reflectEffectPrefab;

    [Tooltip("비관통 무기(예: 망치)에서 면역(반사) 적을 만나면, 폭발이 나(플레이어)에게 옮겨가기 전에 이 이펙트가 먼저 그 적 위치에서 재생된다 - '흡수하는' 연출용(예: LDY_AbsorbEffect)")]
    public GameObject absorbEffectPrefab;

    [Header("피격 리액션 - 맞은 적이 빨개지면서 흔들림 (무기마다 강도를 다르게 조절 가능)")]
    public float hitShakeIntensity = 0.15f;
    public float hitShakeDuration = 0.25f;
    public Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("전기 지지직 효과 - 켜면 맞은 적 몸 위에서 주황~노랑 사이 색 선들이 깜빡인다 (예: 검)")]
    public bool hitCrackleEffect;
    public Color crackleColorA = new Color(1f, 0.5f, 0f, 1f);
    public Color crackleColorB = new Color(1f, 0.95f, 0.2f, 1f);
}

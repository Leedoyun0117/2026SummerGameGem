/// <summary>
/// SoundManager의 SFX 라이브러리에서 사용하는 효과음 식별자.
/// 새 효과음을 추가할 때는 이 enum에 값을 추가하고,
/// SoundManager 인스펙터의 Sfx Entries 리스트에 클립을 등록하면 된다.
/// </summary>
public enum SfxId
{
    Death,              // 죽는 소리
    Explosion,          // 폭발
    ExplosionAbsorb,    // 폭발 흡수
    SwordEnergy,        // 검기 소리
    LaserFire,          // 레이저 발사
    Hit,                // 체력 깎이는 소리(피격 소리)
    CountdownStart,     // 3-2-1-START! 소리
    MapRotate,          // 맵을 돌리는 소리
    CoinGain,           // 코인 들어오는 소리
    CoinDrop,           // 코인 떨어지는 소리
    FragmentFire,       // 파편 발사하는 소리
    WeaponEquip,        // 무기 장착 소리
    Typing,             // 타자 소리
    ModeChange,         // 모드 변경 소리
    StarMove,           // 별 이동 소리
    Iris,               // 아이리스 소리
    Slide,              // 슬라이드 소리
}

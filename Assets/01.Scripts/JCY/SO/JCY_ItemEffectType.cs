// 상점 아이템 20종의 고유 효과를 구분하는 값. JCY_ItemManager가 이 값으로 switch해서
// 실제 효과를 적용한다(JCY_ItemSO는 데이터만 들고 있고 로직은 여기서 분기됨).
public enum JCY_ItemEffectType
{
    None = -1,
    AlienCactus = 0,
    BlackHoleShards = 1,
    BowSagittarius = 2,
    CandleOfTheSun = 3,
    Diamond = 4,
    Feather = 5,
    GravitationalFieldApple = 6,
    HickeyHicerScroll = 7,
    HickeyHickeyBag = 8,
    HitchhikerGuide = 9,
    Magnetite = 10,
    MeteoriteFragments = 11,
    OrionArmor = 12,
    RingOfSaturn = 13,
    SpinefishBoneShard = 14,
    SpringWaterAquarius = 15,
    StarlightLantern = 16,
    TinyAsteroid = 17,
    UniverseSecrets = 18,
    Watermelon = 19,
}

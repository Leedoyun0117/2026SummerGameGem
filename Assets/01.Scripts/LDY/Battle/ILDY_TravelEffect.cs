// 목표 지점까지 즉시가 아니라 시간을 두고 날아가는(자라나는) 이펙트가 구현하는 선택적 인터페이스.
// LDY_AttackTargetController가 이 값을 읽어서 "실제로 도착하는 시점"에 데미지 등을 적용할 수 있게 한다.
public interface ILDY_TravelEffect
{
    float TravelDuration { get; }
}

// 공격 범위 전체(부채꼴)에 맞춰 재생되는 이펙트 프리팹이 구현하는 선택적 인터페이스.
// LDY_AttackTargetController가 이펙트를 보드 중심(0,0)에 Instantiate하고 공격 방향으로 회전시킨 직후,
// 이 범위(안쪽/바깥쪽 반지름, 부채꼴 각도)를 넘겨준다. 파티클 시스템의 Shape 모듈(Arc/Radius 등)을
// 이 값에 맞게 직접 설정하는 스크립트를 만들어서 이 인터페이스를 구현하면 된다.
public interface ILDY_RangeEffect
{
    void SetRange(float innerRadius, float outerRadius, float arcAngleDegrees);
}

using UnityEngine;

// 적중/반사 이펙트 프리팹이 구현하는 최소 인터페이스. LDY_AttackTargetController가 이펙트를 Instantiate한
// 직후 TargetPosition에 상대방(적 또는 플레이어) 좌표를 넣어주기 위해 쓴다.
public interface ILDY_EffectTarget
{
    Vector3 TargetPosition { get; set; }
}

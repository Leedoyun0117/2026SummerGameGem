// 무기 공격 범위 모양. 보드를 "4개 링(행) x 12칸(열)" 격자로 보고 앵커 위치 기준 상대 좌표로 정의한다.
public enum LDY_WeaponAttackShape
{
    Vertical1x4,   // 세로 1x4: 같은 칸(열)에서 안쪽~바깥쪽 링 4개를 전부 관통
    Square2x2,     // 사각형 2x2: 인접한 링 2개 x 인접한 칸 2개
    Horizontal4x1  // 가로 4x1: 같은 링(행)에서 인접한 칸 4개
}

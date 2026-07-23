// 링의 타일 배치 형태. Circle/Square는 "닫힌 루프", Line은 오리가미킹의 일자형 줄(위아래 또는 좌우로 미는 패널).
// 어떤 모양이든 ShiftOccupants/Rotate 로직 자체는 동일하게 동작한다(그냥 순서가 있는 슬롯 배열일 뿐).
public enum LDY_RingShape
{
    Circle,
    Square,
    Line
}

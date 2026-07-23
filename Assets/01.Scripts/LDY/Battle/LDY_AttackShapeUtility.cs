using System.Collections.Generic;
using UnityEngine;

// 무기 공격 범위 모양을, 앵커 칸을 기준으로 한 (링 오프셋, 세그먼트 오프셋) 목록으로 변환해준다.
// Vector2Int.x = 링 오프셋(안쪽 -> 바깥쪽 방향), Vector2Int.y = 세그먼트(칸) 오프셋(원형이라 순환됨).
public static class LDY_AttackShapeUtility
{
    public static List<Vector2Int> GetOffsets(LDY_WeaponAttackShape shape)
    {
        switch (shape)
        {
            case LDY_WeaponAttackShape.Vertical1x4:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                };
            case LDY_WeaponAttackShape.Square2x2:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1),
                };
            case LDY_WeaponAttackShape.Horizontal4x1:
                return new List<Vector2Int>
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3),
                };
            default:
                return new List<Vector2Int>();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

// 슬롯(RingSlot)들을 순서대로 들고 있는 회전 단위(원형 전투판 하나).
// 슬롯의 worldPosition은 생성 시 한 번만 계산되어 고정되고, 회전은 오직 occupant 참조의 배열 인덱스 shift로만 이뤄진다.
// Transform.RotateAround 등으로 오브젝트 자체를 궤도로 굴리는 방식은 절대 쓰지 않는다.
public class BattleRing
{
    public string ringId;
    public List<RingSlot> slots;

    public int SlotCount => slots.Count;

    // 핵심 생성자: 중심점(center)/반지름(radius)/슬롯 개수(segmentCount)를 받아
    // 극좌표로 각 슬롯의 worldPosition을 계산해 고정한다.
    // angle = i * (360 / segmentCount) (도) -> 라디안 변환, position = center + (cos, sin, 0) * radius
    public BattleRing(string ringId, Vector3 center, float radius, int segmentCount)
        : this(ringId, ComputePolarPositions(center, radius, segmentCount))
    {
    }

    // 원형이 아닌 배치(사각형 프레임, 수동 앵커 등)를 쓰고 싶을 때를 위한 보조 생성자.
    // 좌표는 호출하는 쪽에서 미리 계산해서 순서대로 넘겨주면 됨.
    public BattleRing(string ringId, List<Vector3> orderedPositions)
    {
        this.ringId = ringId;
        slots = new List<RingSlot>(orderedPositions.Count);
        for (int i = 0; i < orderedPositions.Count; i++)
        {
            slots.Add(new RingSlot(i, orderedPositions[i]));
        }
    }

    private static List<Vector3> ComputePolarPositions(Vector3 center, float radius, int segmentCount)
    {
        var positions = new List<Vector3>(Mathf.Max(segmentCount, 0));
        for (int i = 0; i < segmentCount; i++)
        {
            float angleDegrees = i * (360f / segmentCount);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0f) * radius;
            positions.Add(center + offset);
        }
        return positions;
    }

    public int Wrap(int i)
    {
        int count = SlotCount;
        return count == 0 ? 0 : ((i % count) + count) % count;
    }

    public RingSlot GetSlot(int index) => slots[Wrap(index)];

    public void PlaceOccupant(int slotIndex, GameObject occupant)
    {
        slots[Wrap(slotIndex)].occupant = occupant;
    }

    public void ClearOccupant(GameObject occupant)
    {
        foreach (RingSlot slot in slots)
        {
            if (slot.occupant == occupant) slot.occupant = null;
        }
    }

    /// <summary>
    /// 배열을 direction * steps 칸 만큼 shift한다. (direction: +1 시계 방향, -1 반시계 방향)
    /// 실제 GameObject는 여기서 이동시키지 않고, 각 슬롯의 occupant 참조만 재배정한다(모듈로 연산으로 순환 처리).
    /// 반환값은 occupant가 새로 바뀐(= 새 위치로 이동 애니메이션이 필요한) 슬롯 목록.
    /// </summary>
    public List<RingSlot> ShiftOccupants(int direction, int steps = 1)
    {
        return ShiftOccupantsInList(slots, direction, steps);
    }

    /// <summary>
    /// ShiftOccupants와 같은 로직이지만, 이 BattleRing 소유의 slots가 아니라 외부에서 조합한 임의의
    /// RingSlot 목록에 대해서도 동작한다. 예: 여러 개의 다른 링(BattleRing)에 걸쳐 있는 슬롯들을 하나로
    /// 이어붙인 "지름선"을 미는 경우 - 오리가미킹에서 링과 링 사이로 적이 넘어가는 연출에 사용.
    /// </summary>
    public static List<RingSlot> ShiftOccupantsInList(List<RingSlot> slots, int direction, int steps = 1)
    {
        var changed = new List<RingSlot>();
        int count = slots.Count;
        if (count == 0 || steps == 0) return changed;

        // 이번 shift 전 상태를 스냅샷으로 떠서, 덮어쓰기 도중에 원본이 오염되지 않게 한다.
        GameObject[] snapshot = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            snapshot[i] = slots[i].occupant;
        }

        for (int i = 0; i < count; i++)
        {
            int sourceIndex = ((i - direction * steps) % count + count) % count;
            GameObject newOccupant = snapshot[sourceIndex];
            if (slots[i].occupant != newOccupant)
            {
                slots[i].occupant = newOccupant;
                changed.Add(slots[i]);
            }
        }

        return changed;
    }

    /// <summary>
    /// predicate를 만족하는 occupant가 링을 따라 원형으로 몇 칸 연속되는지 검사해서
    /// 가장 긴 연속 구간의 occupant 목록을 돌려준다. 콤보 공격 판정(예: 적 N마리 이상 일렬 정렬)에 사용.
    /// </summary>
    public List<GameObject> GetLongestAlignedRun(Func<GameObject, bool> predicate)
    {
        var best = new List<GameObject>();
        var current = new List<GameObject>();
        int count = SlotCount;
        if (count == 0) return best;

        // 원형 배열이라 이어붙은 구간(예: 마지막 칸 -> 첫 칸)도 잡아야 하므로 두 바퀴를 돈다.
        for (int i = 0; i < count * 2; i++)
        {
            RingSlot slot = slots[i % count];
            if (slot.occupant != null && predicate(slot.occupant))
            {
                current.Add(slot.occupant);
                if (current.Count > best.Count) best = new List<GameObject>(current);
                if (current.Count >= count) break; // 링 전체가 정렬된 경우
            }
            else
            {
                current.Clear();
            }
        }

        return best;
    }

    public bool IsAligned(Func<GameObject, bool> predicate, int requiredRun)
    {
        return GetLongestAlignedRun(predicate).Count >= requiredRun;
    }
}

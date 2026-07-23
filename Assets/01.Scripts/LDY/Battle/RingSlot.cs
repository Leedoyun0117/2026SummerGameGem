using UnityEngine;

// 회전 링을 구성하는 하나의 슬롯. worldPosition은 BattleRing 생성 시 한 번만 계산되어 이후 절대 바뀌지 않는다.
// 회전 시에는 이 worldPosition은 그대로 두고, occupant(GameObject) 참조만 다른 슬롯으로 재배정된다.
[System.Serializable]
public class RingSlot
{
    public int index; // 디버깅/에디터 표시용 (배열 내 자기 위치). 회전 로직 자체는 배열 인덱스만으로 동작하므로 필수는 아님
    public Vector3 worldPosition;
    public GameObject occupant;

    public RingSlot(int index, Vector3 worldPosition)
    {
        this.index = index;
        this.worldPosition = worldPosition;
        occupant = null;
    }

    public bool IsEmpty => occupant == null;
}

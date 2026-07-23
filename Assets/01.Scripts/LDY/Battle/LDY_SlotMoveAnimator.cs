using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// "바뀐 슬롯들의 occupant를 현재 위치 -> 새 슬롯 위치로 Lerp 이동시킨다"는 연출을 여러 컨트롤러
// (LDY_RingController, LDY_RadialLineController)가 똑같이 쓰기 때문에 공용 유틸리티로 뺐다.
public static class LDY_SlotMoveAnimator
{
    public static List<(Transform t, Vector3 from, Vector3 to)> BuildMovers(List<RingSlot> changedSlots)
    {
        var movers = new List<(Transform t, Vector3 from, Vector3 to)>();
        foreach (RingSlot slot in changedSlots)
        {
            if (slot.occupant == null) continue;
            movers.Add((slot.occupant.transform, slot.occupant.transform.position, slot.worldPosition));
        }
        return movers;
    }

    public static IEnumerator Animate(List<(Transform t, Vector3 from, Vector3 to)> movers, float duration, AnimationCurve ease)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float e = ease.Evaluate(Mathf.Clamp01(elapsed / duration));
            foreach (var m in movers)
            {
                m.t.position = Vector3.LerpUnclamped(m.from, m.to, e);
            }
            yield return null;
        }

        foreach (var m in movers)
        {
            m.t.position = m.to; // 오차 누적 방지용 스냅
        }
    }
}

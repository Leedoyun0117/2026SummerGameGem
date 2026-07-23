using System.Collections.Generic;
using UnityEngine;

// 전투 씬에 존재하는 모든 링(LDY_RingController)을 등록해두는 창구.
// 각 링은 완전히 독립적으로 회전하지만, 콤보 판정처럼 "회전이 끝날 때마다 보드 전체를 훑어야 하는" 로직은 여기서 처리한다.
public class LDY_BattleBoardManager : MonoBehaviour
{
    public static LDY_BattleBoardManager Instance { get; private set; }

    [Header("등록된 링 (비어있으면 자식에서 자동으로 찾음)")]
    [SerializeField] private List<LDY_RingController> rings = new List<LDY_RingController>();

    [Header("지름선 컨트롤러 (비어있으면 자식에서 자동으로 찾음, 없어도 무방)")]
    [SerializeField] private LDY_RadialLineController radialLine;

    [Header("콤보 판정 기준")]
    [SerializeField] private int requiredAlignedCount = 3;
    [SerializeField] private string enemyTag = "Enemy";

    public IReadOnlyList<LDY_RingController> Rings => rings;
    public LDY_RadialLineController RadialLine => radialLine;

    private void Awake()
    {
        Instance = this;

        if (rings.Count == 0)
        {
            rings.AddRange(GetComponentsInChildren<LDY_RingController>());
        }

        if (radialLine == null)
        {
            radialLine = GetComponentInChildren<LDY_RadialLineController>();
        }

        foreach (LDY_RingController ring in rings)
        {
            ring.OnRotationComplete += HandleRingRotationComplete;
        }
    }

    private void OnDestroy()
    {
        foreach (LDY_RingController ring in rings)
        {
            if (ring != null) ring.OnRotationComplete -= HandleRingRotationComplete;
        }
    }

    // 링 하나가 회전을 마칠 때마다 호출된다. 여기서 콤보 공격 트리거를 걸면 된다.
    private void HandleRingRotationComplete(LDY_RingController ring)
    {
        if (ring.IsEnemyAlignedByTag(requiredAlignedCount, enemyTag))
        {
            Debug.Log($"[LDY_BattleBoardManager] '{ring.name}' 링에서 적 {requiredAlignedCount}마리 이상 정렬됨 -> 콤보 공격 가능");
            // TODO: 실제 콤보 공격/연출 트리거는 여기에 연결
        }
    }

    public LDY_RingController GetRing(string ringId)
    {
        return rings.Find(r => r != null && r.Ring != null && r.Ring.ringId == ringId);
    }
}

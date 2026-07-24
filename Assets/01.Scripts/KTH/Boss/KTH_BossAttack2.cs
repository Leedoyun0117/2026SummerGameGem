using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KTH_BossAttack2 : KTH_BossAttack
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackParamName = "isAttack";

    [Header("Object Spawn Settings")]
    [SerializeField] private GameObject attackPrefab;
    [SerializeField] private Transform spawnPoint; // 소환 시작 위치 (없으면 보스 위치)

    [Header("Attack Settings")]
    [SerializeField] private int spawnCount = 1; // 패널에 던질 개수

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected override IEnumerator AttackRoutine(Action onAttackFinished)
    {
        Debug.Log($"⚡ [보스 공격] 공격 시작! (소환 개수: {spawnCount})");

        // 1. 애니메이션 재생
        if (animator != null)
        {
            animator.SetBool(attackParamName, true);
        }

        // 2. 패널 슬롯 월드 좌표 탐색 및 소환
        if (attackPrefab != null)
        {
            List<Vector3> targetPositions = GetRandomPanelWorldPositions(spawnCount);
            Vector3 startPos = (spawnPoint != null) ? spawnPoint.position : transform.position;

            foreach (Vector3 targetPos in targetPositions)
            {
                // 보스 위치에서 공격 오브젝트 생성
                GameObject attackObj = Instantiate(attackPrefab, startPos, Quaternion.identity);

                // DOTween으로 선택된 패널의 월드 위치로 던지는 연출
                attackObj.transform.localScale = Vector3.zero;
                Sequence seq = DOTween.Sequence();
                seq.Join(attackObj.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad));
                seq.Join(attackObj.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
            }

            Debug.Log($"🔮 [보스 공격] 패널 {targetPositions.Count}곳에 공격 오브젝트 투척 완료!");
        }

        yield return new WaitForSeconds(0.5f);

        // 3. 애니메이션 파라미터 리셋
        if (animator != null)
        {
            animator.SetBool(attackParamName, false);
        }

        // 4. 공격 완료 알림
        onAttackFinished?.Invoke();
    }

    /// <summary>
    /// 🔥 씬 내 패널(슬롯)들의 worldPosition 좌표를 수집하여 반환합니다.
    /// </summary>
    private List<Vector3> GetRandomPanelWorldPositions(int count)
    {
        List<Vector3> availableWorldPositions = new List<Vector3>();

        // 1순위: LDY_RingController 및 Slot 탐색 (PlayerMovement와 동일한 기준)
        LDY_RingController[] ringControllers = FindObjectsByType<LDY_RingController>(FindObjectsSortMode.None);

        foreach (var ringController in ringControllers)
        {
            if (ringController != null && ringController.Ring != null)
            {
                for (int i = 0; i < ringController.Ring.SlotCount; i++)
                {
                    RingSlot slot = ringController.Ring.GetSlot(i);
                    if (slot != null)
                    {
                        // slot.worldPosition 사용
                        availableWorldPositions.Add(slot.worldPosition);
                    }
                }
            }
        }

        // 2순위: 링 컨트롤러에서 구하지 못했을 경우 씬의 KTH_Tile(타일) 위치 직접 수집
        if (availableWorldPositions.Count == 0)
        {
            KTH_Tile[] tiles = FindObjectsByType<KTH_Tile>(FindObjectsSortMode.None);
            foreach (var tile in tiles)
            {
                availableWorldPositions.Add(tile.transform.position);
            }
        }

        List<Vector3> selectedPositions = new List<Vector3>();

        // 타일/패널을 아예 못 찾았을 때는 기본 보스 위치 사용
        if (availableWorldPositions.Count == 0)
        {
            for (int i = 0; i < count; i++)
            {
                selectedPositions.Add(transform.position);
            }
            return selectedPositions;
        }

        // 개수만큼 랜덤 선택 (중복 허용)
        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableWorldPositions.Count);
            selectedPositions.Add(availableWorldPositions[randomIndex]);
        }

        return selectedPositions;
    }
}
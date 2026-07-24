using UnityEngine;

// 튜토리얼 전용 Battle_T(BattleBoard/BTUI/Tuto_Canvas/BattleUICanvas 포함) 오브젝트에 붙여서 사용.
// 튜토리얼이 끝나는 버튼 OnClick에 DestroyBattleTutorial()을 연결하면 이 오브젝트와, 튜토리얼용으로
// 같이 생성된 LDY_MapManager(DontDestroyOnLoad 싱글턴)까지 함께 파괴한다.
// MapManager를 안 지우면 실제 맵 씬으로 넘어갔을 때 이 튜토리얼용 더미 인스턴스가 이미 Instance를
// 차지하고 있어서, 진짜 MapManager가 중복으로 판정되어 자기 자신을 파괴해버린다(노드 정보가 텅 빈 채로 남음).
public class TutorialBattleCleanup : MonoBehaviour
{
    public void DestroyBattleTutorial()
    {
        Destroy(gameObject);

        if (LDY_MapManager.Instance != null)
        {
            Destroy(LDY_MapManager.Instance.gameObject);
        }
    }
}

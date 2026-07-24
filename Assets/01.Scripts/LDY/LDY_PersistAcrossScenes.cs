using UnityEngine;

// 이 오브젝트(와 그 자식들)를 씬이 바뀌어도(맵 <-> 전투 등) 파괴되지 않게 만든다.
// 체력 UI, 재화 UI처럼 여러 씬에 걸쳐 계속 떠 있어야 하는 UI 캔버스 루트에 붙이면 된다.
// (KTH_PlayerHealth/LDY_StarPieceManager처럼 이미 자체적으로 DontDestroyOnLoad + 싱글턴 중복 방지를
// 하는 데이터 스크립트가 캔버스에 붙어 있다면 이 컴포넌트는 따로 안 붙여도 된다 - 순수 UI만 있는
// 캔버스(예: 체력 하트 이미지, 텍스트만 있고 로직 스크립트는 없는 경우)를 위한 것.)
// 씬을 다시 로드했을 때 이미 같은 이름의 오브젝트가 떠 있으면(중복 방지) 새로 생긴 쪽을 파괴한다.
public class LDY_PersistAcrossScenes : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<string> PersistedNames = new System.Collections.Generic.HashSet<string>();

    private void Awake()
    {
        if (PersistedNames.Contains(gameObject.name))
        {
            Destroy(transform.root.gameObject);
            return;
        }

        PersistedNames.Add(gameObject.name);
        // DontDestroyOnLoad는 "최상위(루트)" 오브젝트에만 걸 수 있다 - 이 컴포넌트를 자식에 붙여도
        // 안전하게 동작하도록 transform.root를 쓴다(권장은 캔버스 루트에 직접 붙이는 것).
        DontDestroyOnLoad(transform.root.gameObject);
    }
}

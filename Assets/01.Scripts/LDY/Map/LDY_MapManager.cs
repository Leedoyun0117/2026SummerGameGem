using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LDY_MapNodeUnityEvent : UnityEvent<LDY_MapNode> { }

public class LDY_MapManager : MonoBehaviour
{
    public static LDY_MapManager Instance { get; private set; }

    [Header("별자리 노드 (좌표/타입)")]
    [SerializeField] private Vector2[] nodePositions;
    [SerializeField] private LDY_NodeType[] nodeTypes;

    [Header("노드 연결 (분기 가능: 한 노드가 여러 연결을 가질 수 있음)")]
    [SerializeField] private LDY_NodeConnection[] connections;

    [Header("씬 전환용 씬 이름")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string bossSceneName = "BossScene";

    [Header("Shop / Event 노드 진입 시 호출되는 이벤트")]
    public LDY_MapNodeUnityEvent onShopNodeSelected;
    public LDY_MapNodeUnityEvent onEventNodeSelected;

    [Header("노드 상태가 바뀔 때마다 호출 (UI 갱신용)")]
    public UnityEvent onMapChanged;

    // 지금 플레이어가 진입해서 진행 중인 노드 (전투/상점/이벤트가 끝나면 CompleteActiveNode로 완료 처리)
    [SerializeField] private int activeNodeIndex = -1;
    public int ActiveNodeIndex => activeNodeIndex;

    public List<LDY_MapNode> Nodes { get; private set; } = new List<LDY_MapNode>();
    public LDY_NodeConnection[] Connections => connections;

    // 전투(Battle) 노드에 들어간 횟수. 이 매니저는 DontDestroyOnLoad라서 Map -> Battle 씬 전환에도
    // 값이 유지된다 - 전투 씬에서 이 값을 읽어 적 숫자 등 난이도를 올리는 데 쓰면 된다(예: LDY_BattleDifficultyManager).
    public int BattleEntryCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildNodes();
    }

    private void OnValidate()
    {
        if (nodeTypes != null && nodePositions != null && nodeTypes.Length != nodePositions.Length)
        {
            Debug.LogWarning($"[LDY_MapManager] nodePositions({nodePositions.Length})와 nodeTypes({nodeTypes.Length}) 길이가 다릅니다.", this);
        }
    }

    private void BuildNodes()
    {
        Nodes.Clear();
        activeNodeIndex = -1;

        for (int i = 0; i < nodePositions.Length; i++)
        {
            LDY_NodeType type = (nodeTypes != null && i < nodeTypes.Length) ? nodeTypes[i] : LDY_NodeType.Battle;
            Nodes.Add(new LDY_MapNode(nodePositions[i], type));
        }

        if (connections != null)
        {
            foreach (LDY_NodeConnection c in connections)
            {
                if (!IsValidIndex(c.fromIndex) || !IsValidIndex(c.toIndex)) continue;
                Nodes[c.fromIndex].nextIndices.Add(c.toIndex);
            }
        }

        if (Nodes.Count == 0) return;

        int startIndex = Nodes.FindIndex(n => n.type == LDY_NodeType.Start);
        if (startIndex < 0) startIndex = 0;

        Nodes[startIndex].isUnlocked = true;

        if (Nodes[startIndex].type == LDY_NodeType.Start)
            CompleteNode(startIndex);
    }

    // screenUV: 클릭한 노드의 화면상 위치(0~1). 씬 전환 아이리스 연출의 중심점으로 씀
    public void OnNodeClicked(int index, Vector2 screenUV)
    {
        if (!IsValidIndex(index)) return;

        LDY_MapNode node = Nodes[index];
        if (!node.isUnlocked || node.isCleared) return;

        activeNodeIndex = index;

        switch (node.type)
        {
            case LDY_NodeType.Battle:
                BattleEntryCount++;
                RequestSceneLoad(battleSceneName, screenUV);
                break;
            case LDY_NodeType.Boss:
                RequestSceneLoad(bossSceneName, screenUV);
                break;
            case LDY_NodeType.Shop:
                onShopNodeSelected?.Invoke(node);
                break;
            case LDY_NodeType.Event:
                onEventNodeSelected?.Invoke(node);
                break;
            case LDY_NodeType.Start:
                CompleteNode(index);
                break;
        }
    }

    // 전투 승리, 상점/이벤트 종료 시점에 마지막으로 진입했던 노드를 완료 처리 (가장 흔히 쓰는 진입점)
    public void CompleteActiveNode()
    {
        if (activeNodeIndex >= 0) CompleteNode(activeNodeIndex);
    }

    // 특정 노드를 클리어 처리하고, 그 노드와 연결된 다음 노드들(분기면 여러 개)을 모두 unlock
    public void CompleteNode(int index)
    {
        if (!IsValidIndex(index)) return;

        Nodes[index].isCleared = true;

        foreach (int next in Nodes[index].nextIndices)
        {
            if (IsValidIndex(next)) Nodes[next].isUnlocked = true;
        }

        if (activeNodeIndex == index) activeNodeIndex = -1;

        onMapChanged?.Invoke();
    }

    private bool IsValidIndex(int index) => index >= 0 && index < Nodes.Count;

    // 씬 전환 연출(LDY_SceneTransition)이 씬에 있으면 그걸 거쳐서, 없으면 곧바로 씬을 로드
    private void RequestSceneLoad(string sceneName, Vector2 screenUV)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (LDY_SceneTransition.Instance != null)
            LDY_SceneTransition.Instance.PlayIrisCloseThenLoad(screenUV, sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}

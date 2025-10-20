using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    [Header("依赖组件")]
    public DialogueDataLoader dataLoader;

    [Header("当前状态")]
    [SerializeField] private int _currentNodeID = 0;
    [SerializeField] private DialogueNode _currentNode;
    [SerializeField] private DialogueState _state = DialogueState.Idle;

    [Header("场景管理")]
    public string currentSceneName;
    public bool autoLoadSceneDialogue = true;

    [Header("游戏状态")]
    public GameState gameState = new GameState();

    [Header("对话设置")]
    public float textDisplaySpeed = 0.05f;
    public bool autoContinue = false;
    public float autoContinueDelay = 2f;

    [Header("调试选项")]
    public bool debugMode = true;

    // 对话状态枚举
    public enum DialogueState
    {
        Idle,
        Playing,
        WaitingForInput,
        BranchSelection,
        Ended
    }

    [Header("对话事件")]
    public UnityEvent<DialogueNode> OnDialogueStart;
    public UnityEvent<DialogueNode> OnDialogueNodeChange;
    public UnityEvent<DialogueNode> OnDialogueContentUpdate;
    public UnityEvent OnDialogueEnd;
    public UnityEvent<List<DialogueNode>> OnBranchOptionsShow;
    public UnityEvent<string> OnConditionCheck;
    public UnityEvent<string> OnEffectApplied;
    public UnityEvent<string> OnSceneDialogueTriggered; // 新增：场景对话触发事件

    // 历史记录和状态
    private List<int> _dialogueHistory = new List<int>();
    private Coroutine _currentDialogueCoroutine;
    private HashSet<string> _triggeredSceneDialogue = new HashSet<string>();

    void Start()
    {
        // 获取当前场景名
        currentSceneName = SceneManager.GetActiveScene().name;

        // 确保有数据加载器
        if (dataLoader == null)
        {
            dataLoader = GetComponent<DialogueDataLoader>();
        }

        if (dataLoader != null)
        {
            // 注册数据加载完成事件
            dataLoader.OnDataLoaded += OnDataLoaded;
        }

        // 初始化游戏状态
        InitializeGameState();

        if (debugMode)
        {
            Debug.Log($"DialogueManager初始化完成，当前场景: {currentSceneName}");
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 初始化游戏状态
    /// </summary>
    private void InitializeGameState()
    {
        gameState.health = 100;
        gameState.money = 10;
        gameState.maidInParty = true;
        gameState.visitedNodes = new HashSet<int>();
        gameState.items = new Dictionary<string, int>();
        gameState.flags = new Dictionary<string, bool>();
        gameState.currentScene = currentSceneName;
    }

    /// <summary>
    /// 数据加载完成回调
    /// </summary>
    private void OnDataLoaded()
    {
        if (debugMode)
        {
            Debug.Log("对话数据加载完成");
        }

        // 自动加载场景开始对话
        if (autoLoadSceneDialogue)
        {
            LoadSceneStartDialogue();
        }
    }

    /// <summary>
    /// 加载场景开始对话 (#S)
    /// </summary>
    public void LoadSceneStartDialogue()
    {
        string sceneKey = $"{currentSceneName}_#S";

        // 检查是否已经触发过
        if (_triggeredSceneDialogue.Contains(sceneKey))
        {
            if (debugMode)
            {
                Debug.Log($"场景开始对话已触发过: {sceneKey}");
            }
            return;
        }

        // 查找当前场景的开始对话节点
        List<DialogueNode> sceneStartNodes = FindSceneDialogueNodes("#S");

        if (sceneStartNodes.Count > 0)
        {
            _triggeredSceneDialogue.Add(sceneKey);

            if (debugMode)
            {
                Debug.Log($"找到场景开始对话，共 {sceneStartNodes.Count} 个节点");
            }

            // 触发第一个开始节点
            StartDialogue(sceneStartNodes[0].ID);

            OnSceneDialogueTriggered?.Invoke("SceneStart");
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"未找到场景开始对话 (#S) 对于场景: {currentSceneName}");
            }
        }
    }

    /// <summary>
    /// 触发场景结束对话 (#E)
    /// </summary>
    public void TriggerSceneEndDialogue()
    {
        string sceneKey = $"{currentSceneName}_#E";

        // 检查是否已经触发过
        if (_triggeredSceneDialogue.Contains(sceneKey))
        {
            if (debugMode)
            {
                Debug.Log($"场景结束对话已触发过: {sceneKey}");
            }
            return;
        }

        // 查找当前场景的结束对话节点
        List<DialogueNode> sceneEndNodes = FindSceneDialogueNodes("#E");

        if (sceneEndNodes.Count > 0)
        {
            _triggeredSceneDialogue.Add(sceneKey);

            if (debugMode)
            {
                Debug.Log($"找到场景结束对话，共 {sceneEndNodes.Count} 个节点");
            }

            // 如果当前没有对话在进行，直接开始结束对话
            if (_state == DialogueState.Idle || _state == DialogueState.Ended)
            {
                StartDialogue(sceneEndNodes[0].ID);
            }
            else
            {
                // 如果当前有对话，先结束当前对话，然后开始结束对话
                StartCoroutine(StartEndDialogueAfterCurrent(sceneEndNodes[0].ID));
            }

            OnSceneDialogueTriggered?.Invoke("SceneEnd");
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"未找到场景结束对话 (#E) 对于场景: {currentSceneName}");
            }
        }
    }

    /// <summary>
    /// 在当前对话结束后开始结束对话
    /// </summary>
    private IEnumerator StartEndDialogueAfterCurrent(int endNodeID)
    {
        // 等待当前对话结束
        yield return new WaitUntil(() => _state == DialogueState.Ended || _state == DialogueState.Idle);

        // 短暂延迟
        yield return new WaitForSeconds(0.5f);

        // 开始结束对话
        StartDialogue(endNodeID);
    }

    /// <summary>
    /// 查找场景特定位置的对话节点
    /// </summary>
    private List<DialogueNode> FindSceneDialogueNodes(string position)
    {
        List<DialogueNode> result = new List<DialogueNode>();

        if (dataLoader == null || dataLoader.AllNodes == null)
        {
            Debug.LogWarning("数据加载器未初始化");
            return result;
        }

        foreach (var node in dataLoader.AllNodes.Values)
        {
            // 检查关卡匹配和位置匹配
            if (node.Level == currentSceneName && node.Position == position)
            {
                result.Add(node);
            }
        }

        // 按ID排序
        result.Sort((a, b) => a.ID.CompareTo(b.ID));

        return result;
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(int startNodeID = 0)
    {
        if (_state != DialogueState.Idle && _state != DialogueState.Ended)
        {
            Debug.LogWarning("对话正在进行中，无法开始新对话");
            return;
        }

        DialogueNode startNode = dataLoader.GetNode(startNodeID);
        if (startNode == null)
        {
            Debug.LogError($"找不到对话节点: {startNodeID}");
            return;
        }

        _currentNodeID = startNodeID;
        _currentNode = startNode;
        _state = DialogueState.Playing;

        // 清空历史记录
        _dialogueHistory.Clear();

        if (debugMode)
        {
            Debug.Log($"开始对话: {_currentNode.GetDebugInfo()}");
        }

        OnDialogueStart?.Invoke(_currentNode);
        ProcessCurrentNode();
    }

    /// <summary>
    /// 继续对话
    /// </summary>
    public void ContinueDialogue()
    {
        if (_state != DialogueState.WaitingForInput)
        {
            return;
        }

        MoveToNextNode();
    }

    /// <summary>
    /// 处理当前节点
    /// </summary>
    private void ProcessCurrentNode()
    {
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        // 记录历史
        _dialogueHistory.Add(_currentNodeID);
        gameState.visitedNodes.Add(_currentNodeID);

        // 检查条件
        if (!CheckNodeConditions(_currentNode))
        {
            if (debugMode)
            {
                Debug.LogWarning($"节点 {_currentNodeID} 条件不满足，跳过");
            }
            MoveToNextNode();
            return;
        }

        // 应用效果
        ApplyNodeEffects(_currentNode);

        // 触发节点变化事件
        OnDialogueNodeChange?.Invoke(_currentNode);

        // 开始显示文本
        if (_currentDialogueCoroutine != null)
        {
            StopCoroutine(_currentDialogueCoroutine);
        }
        _currentDialogueCoroutine = StartCoroutine(DisplayTextCoroutine());
    }

    /// <summary>
    /// 显示文本的协程
    /// </summary>
    private IEnumerator DisplayTextCoroutine()
    {
        _state = DialogueState.Playing;

        if (textDisplaySpeed <= 0)
        {
            OnDialogueContentUpdate?.Invoke(_currentNode);
            _state = DialogueState.WaitingForInput;
        }
        else
        {
            string fullText = _currentNode.GetFormattedContent();
            for (int i = 0; i <= fullText.Length; i++)
            {
                string partialText = fullText.Substring(0, i);
                DialogueNode tempNode = CreateTempNodeWithText(_currentNode, partialText);
                OnDialogueContentUpdate?.Invoke(tempNode);

                yield return new WaitForSeconds(textDisplaySpeed);
            }

            _state = DialogueState.WaitingForInput;

            if (autoContinue && !_currentNode.HasBranch())
            {
                yield return new WaitForSeconds(autoContinueDelay);
                MoveToNextNode();
            }
        }
    }

    /// <summary>
    /// 创建临时节点（用于逐字显示）
    /// </summary>
    private DialogueNode CreateTempNodeWithText(DialogueNode original, string text)
    {
        DialogueNode tempNode = new DialogueNode(original.TableType);

        // 复制基础字段
        tempNode.ID = original.ID;
        tempNode.Next = original.Next;
        tempNode.Flag = original.Flag;
        tempNode.Level = original.Level;

        // 根据类型设置内容
        switch (original.TableType)
        {
            case DialogueType.Battle:
                tempNode.BattleContent = text;
                tempNode.BattleCharacter = original.BattleCharacter;
                tempNode.BattlePortrait = original.BattlePortrait;
                tempNode.BattlePortraitPos = original.BattlePortraitPos;
                break;
            case DialogueType.Explore:
                tempNode.ExploreContent = text;
                tempNode.ExploreCharacter = original.ExploreCharacter;
                tempNode.ExplorePortrait = original.ExplorePortrait;
                tempNode.ExplorePortraitPos = original.ExplorePortraitPos;
                break;
            case DialogueType.Normal:
                tempNode.NormalContent = text;
                tempNode.NormalCharacter = original.NormalCharacter;
                tempNode.NormalPortrait = original.NormalPortrait;
                tempNode.NormalPortraitPos = original.NormalPortraitPos;
                tempNode.NormalPosition = original.NormalPosition;
                break;
            case DialogueType.Outside:
                tempNode.OutsideContent = text;
                tempNode.OutsideCharacter = original.OutsideCharacter;
                tempNode.OutsidePortrait = original.OutsidePortrait;
                tempNode.OutsidePortraitPos = original.OutsidePortraitPos;
                tempNode.OutsidePosition = original.OutsidePosition;
                break;
            case DialogueType.Dead:
                tempNode.DeadContent = text;
                break;
        }

        return tempNode;
    }

    /// <summary>
    /// 移动到下一个节点
    /// </summary>
    private void MoveToNextNode()
    {
        if (_currentNode == null)
        {
            EndDialogue();
            return;
        }

        // 检查是否是场景结束节点
        if (_currentNode.Position == "#E")
        {
            if (debugMode)
            {
                Debug.Log("到达场景结束节点，准备结束场景");
            }
            EndDialogue();
            OnSceneEndReached?.Invoke();
            return;
        }

        // 检查分支
        if (_currentNode.HasBranch())
        {
            ShowBranchOptions();
            return;
        }

        int[] nextIDs = _currentNode.GetNextIDs();
        if (nextIDs.Length == 0)
        {
            EndDialogue();
            return;
        }

        // 默认选择第一个分支
        int nextID = nextIDs[0];
        MoveToNode(nextID);
    }

    /// <summary>
    /// 移动到指定节点
    /// </summary>
    private void MoveToNode(int nodeID)
    {
        if (nodeID == -1) // E表示结束
        {
            EndDialogue();
            return;
        }

        DialogueNode nextNode = dataLoader.GetNode(nodeID);
        if (nextNode == null)
        {
            Debug.LogError($"找不到对话节点: {nodeID}");
            EndDialogue();
            return;
        }

        _currentNodeID = nodeID;
        _currentNode = nextNode;

        ProcessCurrentNode();
    }

    /// <summary>
    /// 显示分支选项
    /// </summary>
    private void ShowBranchOptions()
    {
        _state = DialogueState.BranchSelection;

        int[] nextIDs = _currentNode.GetNextIDs();
        List<DialogueNode> branchNodes = new List<DialogueNode>();

        foreach (int nextID in nextIDs)
        {
            DialogueNode branchNode = dataLoader.GetNode(nextID);
            if (branchNode != null)
            {
                branchNodes.Add(branchNode);
            }
        }

        if (debugMode)
        {
            Debug.Log($"显示分支选项，共 {branchNodes.Count} 个选项");
        }

        OnBranchOptionsShow?.Invoke(branchNodes);
    }

    /// <summary>
    /// 选择分支
    /// </summary>
    public void SelectBranch(int branchIndex)
    {
        if (_state != DialogueState.BranchSelection)
        {
            Debug.LogWarning("当前不在分支选择状态");
            return;
        }

        int[] nextIDs = _currentNode.GetNextIDs();
        if (branchIndex < 0 || branchIndex >= nextIDs.Length)
        {
            Debug.LogError($"无效的分支索引: {branchIndex}");
            return;
        }

        int selectedNodeID = nextIDs[branchIndex];

        if (debugMode)
        {
            Debug.Log($"选择分支: {branchIndex} -> 节点 {selectedNodeID}");
        }

        MoveToNode(selectedNodeID);
    }

    /// <summary>
    /// 检查节点条件
    /// </summary>
    private bool CheckNodeConditions(DialogueNode node)
    {
        string condition = node.Condition;
        if (string.IsNullOrEmpty(condition))
        {
            return true;
        }

        if (debugMode)
        {
            Debug.Log($"检查条件: {condition}");
        }

        OnConditionCheck?.Invoke(condition);

        string[] conditions = condition.Split('&');

        foreach (string cond in conditions)
        {
            if (!CheckSingleCondition(cond.Trim()))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查单个条件
    /// </summary>
    private bool CheckSingleCondition(string condition)
    {
        if (condition.Contains("Maid"))
        {
            if (condition == "Maid&1")
            {
                return gameState.maidInParty;
            }
            else if (condition == "Maid&0")
            {
                return !gameState.maidInParty;
            }
        }
        else if (condition.Contains("blood"))
        {
            if (condition.Contains("<"))
            {
                string[] parts = condition.Split('<');
                if (parts.Length == 2 && int.TryParse(parts[1], out int requiredHealth))
                {
                    return gameState.health < requiredHealth;
                }
            }
            else if (condition.Contains(">"))
            {
                string[] parts = condition.Split('>');
                if (parts.Length == 2 && int.TryParse(parts[1], out int requiredHealth))
                {
                    return gameState.health > requiredHealth;
                }
            }
        }
        else if (condition.Contains("@"))
        {
            string[] parts = condition.Split('@');
            if (parts.Length == 2 && int.TryParse(parts[0], out int nodeID))
            {
                return gameState.visitedNodes.Contains(nodeID);
            }
        }

        return true;
    }

    /// <summary>
    /// 应用节点效果
    /// </summary>
    private void ApplyNodeEffects(DialogueNode node)
    {
        if (node.TableType == DialogueType.Explore && !string.IsNullOrEmpty(node.ExploreEffect))
        {
            ApplyExploreEffect(node.ExploreEffect);
        }
    }

    /// <summary>
    /// 应用探索效果
    /// </summary>
    private void ApplyExploreEffect(string effect)
    {
        if (debugMode)
        {
            Debug.Log($"应用效果: {effect}");
        }

        OnEffectApplied?.Invoke(effect);

        if (effect.Contains("money++"))
        {
            string amountStr = effect.Replace("money++", "");
            if (int.TryParse(amountStr, out int amount))
            {
                gameState.money += amount;
            }
        }
        else if (effect.Contains("money+-"))
        {
            string amountStr = effect.Replace("money+-", "");
            if (int.TryParse(amountStr, out int amount))
            {
                gameState.money -= amount;
            }
        }
        else if (effect.Contains("blood+a++"))
        {
            string amountStr = effect.Replace("blood+a++", "");
            if (int.TryParse(amountStr, out int amount))
            {
                gameState.health += amount;
            }
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    private void EndDialogue()
    {
        _state = DialogueState.Ended;
        _currentNode = null;

        if (debugMode)
        {
            Debug.Log("对话结束");
        }

        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        if (_state == DialogueState.WaitingForInput)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                ContinueDialogue();
            }
        }
    }

    /// <summary>
    /// 跳过当前文本显示
    /// </summary>
    public void SkipTextDisplay()
    {
        if (_currentDialogueCoroutine != null)
        {
            StopCoroutine(_currentDialogueCoroutine);
        }

        OnDialogueContentUpdate?.Invoke(_currentNode);
        _state = DialogueState.WaitingForInput;
    }

    /// <summary>
    /// 跳转到指定节点
    /// </summary>
    public void JumpToNode(int nodeID)
    {
        if (_state == DialogueState.Idle)
        {
            StartDialogue(nodeID);
        }
        else
        {
            MoveToNode(nodeID);
        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public DialogueState GetCurrentState()
    {
        return _state;
    }

    /// <summary>
    /// 获取当前节点
    /// </summary>
    public DialogueNode GetCurrentNode()
    {
        return _currentNode;
    }

    /// <summary>
    /// 获取对话历史
    /// </summary>
    public List<int> GetDialogueHistory()
    {
        return new List<int>(_dialogueHistory);
    }

    /// <summary>
    /// 强制结束对话
    /// </summary>
    public void ForceEndDialogue()
    {
        if (_currentDialogueCoroutine != null)
        {
            StopCoroutine(_currentDialogueCoroutine);
        }

        EndDialogue();
    }

    /// <summary>
    /// 设置游戏状态标志
    /// </summary>
    public void SetGameFlag(string flag, bool value)
    {
        if (gameState.flags.ContainsKey(flag))
        {
            gameState.flags[flag] = value;
        }
        else
        {
            gameState.flags.Add(flag, value);
        }
    }

    /// <summary>
    /// 获取游戏状态标志
    /// </summary>
    public bool GetGameFlag(string flag)
    {
        return gameState.flags.ContainsKey(flag) && gameState.flags[flag];
    }

    /// <summary>
    /// 设置角色在队伍状态
    /// </summary>
    public void SetMaidInParty(bool inParty)
    {
        gameState.maidInParty = inParty;
    }

    /// <summary>
    /// 设置血量
    /// </summary>
    public void SetHealth(int health)
    {
        gameState.health = health;
    }

    /// <summary>
    /// 设置金钱
    /// </summary>
    public void SetMoney(int money)
    {
        gameState.money = money;
    }

    /// <summary>
    /// 场景切换时调用
    /// </summary>
    public void OnSceneChanged(string newSceneName)
    {
        currentSceneName = newSceneName;
        gameState.currentScene = newSceneName;
        _triggeredSceneDialogue.Clear(); // 清除触发记录，新场景可以重新触发

        if (debugMode)
        {
            Debug.Log($"场景切换至: {newSceneName}");
        }

        // 自动加载新场景的开始对话
        if (autoLoadSceneDialogue)
        {
            LoadSceneStartDialogue();
        }
    }

    // 新增事件：场景结束到达
    public UnityEvent OnSceneEndReached;
}

/// <summary>
/// 游戏状态类（更新版）
/// </summary>
[System.Serializable]
public class GameState
{
    public int health;
    public int money;
    public bool maidInParty;
    public string currentScene;
    public HashSet<int> visitedNodes;
    public Dictionary<string, int> items;
    public Dictionary<string, bool> flags;
}
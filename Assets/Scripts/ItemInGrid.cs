using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class ItemInGrid : MonoBehaviour
{
    [Tooltip("矩形占用区域的两个对角坐标")]
    public Vector2Int cornerA = Vector2Int.zero;
    public Vector2Int cornerB = Vector2Int.zero;

    [Tooltip("是否是单格物件，如果勾选，只占用cornerA")]
    public bool isSingleCell = false;

    public List<GameGrid> occupiedGrids = new List<GameGrid>();
    public bool isInterable = false;
    public bool isBattleInterable = false; // 是否在战斗状态下可交互
    public SpriteRenderer sr;

    // 新增部分
    private UnitController[] players;   // 所有玩家
    public float transparentAlpha = 0.75f;
    private float normalAlpha = 1f;
    private bool isTransparent = false;

    [Header("透明度检测 Layer")]
    public LayerMask blockingLayer;  // Item 所在 Layer

    [Header("对话设置")]
    public UnityEngine.TextAsset dialogueFile; // 关联的对话文件
    public bool triggerDialogueOnInteract = true; // 是否触发对话
    public TMP_FontAsset customFont;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // 找到所有 UnitController 实例
        players = FindObjectsOfType<UnitController>();

        // ===== 原始初始化逻辑 =====
        if (isSingleCell)
            Occupy(cornerA, cornerA);
        else
            Occupy(cornerA, cornerB);

        if (sr != null && occupiedGrids.Count > 0)
        {
            float sum = 0f;
            foreach (var grid in occupiedGrids)
            {
                sum += -(grid.gridPos.x + grid.gridPos.y);
            }
            int average = Mathf.RoundToInt(sum / occupiedGrids.Count);
            sr.sortingOrder = average + 2;
            if (transform.childCount > 0)
                transform.GetComponent<SpriteRenderer>().sortingOrder = average + 3;
        }

        if(isSingleCell)
        {
            transform.GetComponent<SpriteRenderer>().sortingOrder = -(cornerA.x+cornerA.y)+2;
        }
        if (isInterable)
        {
            foreach (var grid in occupiedGrids)
            {
                grid.isInterable = true;
                grid.canDialogue = true;

                // 设置战斗时的交互格子颜色
                grid.SetColor(new Color(0.3f, 0.6f, 1f, 0.8f));

                Debug.Log($"物品 {gameObject.name} 初始化：设置格子 {grid.gridPos} 为可交互");
            }
        }
    }

    void Update()
    {
        if (sr == null || players == null || players.Length == 0) return;

        bool blockingAny = false;

        foreach (var player in players)
        {
            if (player == null) continue;

            // 获取玩家在屏幕空间的矩形（用 Transform 或 SpriteRenderer.bounds）
            Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

            // 获取 Item 的屏幕矩形
            Bounds itemBounds = sr.bounds;
            Vector3 itemMin = Camera.main.WorldToScreenPoint(itemBounds.min);
            Vector3 itemMax = Camera.main.WorldToScreenPoint(itemBounds.max);

            Rect itemRect = new Rect(itemMin.x, itemMin.y, itemMax.x - itemMin.x, itemMax.y - itemMin.y);

            // 判断玩家是否在 Item 的屏幕矩形内
            if (itemRect.Contains(new Vector2(playerScreenPos.x, playerScreenPos.y)))
            {
                blockingAny = true;
                break;
            }
        }

        // 切换透明状态
        if (blockingAny && !isTransparent)
        {
            SetAlpha(transparentAlpha);
            isTransparent = true;
        }
        else if (!blockingAny && isTransparent)
        {
            SetAlpha(normalAlpha);
            isTransparent = false;
        }
    }

    void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    public void Occupy(Vector2Int a, Vector2Int b)
    {
        ClearOccupied();
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GameObject tileObj = IsoGrid2D.instance.GetTile(x, y);
                if (tileObj != null)
                {
                    GameGrid gridComp = tileObj.GetComponent<GameGrid>();
                    gridComp.isOccupied = true;
                    gridComp.ocuupiedItem = this;

                    occupiedGrids.Add(gridComp);
                }
            }
        }
    }

    public void ClearOccupied()
    {
        foreach (var grid in occupiedGrids)
        {
            grid.isOccupied = false;
            grid.isInterable = false;
        }
        occupiedGrids.Clear();
    }

    private void OnDestroy()
    {
        ClearOccupied();
    }

    public void SetCanInteract(Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GameObject tileObj = IsoGrid2D.instance.GetTile(x, y);
                if (tileObj != null)
                {
                    GameGrid gridComp = tileObj.GetComponent<GameGrid>();
                    gridComp.SetColor(new Color(0.3f, 0.6f, 1f, 0.8f));
                    gridComp.canDialogue = true;
                }
            }
        }
    }

    // 检查是否处于战斗状态
    private bool IsInBattleState()
    {
        // 方法1: 检查TurnManager的战斗状态
        TurnManager turnManager = FindObjectOfType<TurnManager>();
        if (turnManager != null)
        {
            return turnManager.phase != TurnPhase.Exploration;
        }

        // 方法2: 检查ExplorationManager（修复了静态方法调用问题）
        ExplorationManager explorationManager = FindObjectOfType<ExplorationManager>();
        if (explorationManager != null)
        {
            // 使用实例方法而不是静态方法
            return !ExplorationManager.IsInExploration();
        }

        // 方法3: 检查是否有敌人存在
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        if (enemies != null && enemies.Length > 0)
        {
            return true;
        }

        // 默认返回非战斗状态
        return false;
    }

    public virtual void Interact()
    {
        if (!isInterable)
        {
            Debug.LogWarning($"物品 {name} 不可交互，isInterable = {isInterable}");
            return;
        }

        // 新增：检查战斗状态
        bool inBattle = IsInBattleState();
        if (inBattle && !isBattleInterable)
        {
            Debug.LogWarning($"战斗状态下无法与物品 {name} 交互，isBattleInterable = {isBattleInterable}");

            // 可选：显示战斗状态下无法交互的提示
            ShowCannotInteractInBattleMessage();
            return;
        }

        Debug.Log($"与物品交互: {gameObject.name} (战斗状态: {inBattle})");

        // 触发对话
        if (triggerDialogueOnInteract && dialogueFile != null)
        {
            TriggerDialogue();
        }

        // 可以在这里添加其他交互逻辑
        // 例如：获得物品、触发事件等
    }

    // 显示战斗状态下无法交互的提示
    private void ShowCannotInteractInBattleMessage()
    {
        // 方法1: 使用Debug.Log显示消息
        Debug.Log("战斗状态下无法与此物品交互！");

        // 方法2: 如果你有UI提示系统，可以在这里调用
        // UIManager.Instance.ShowMessage("战斗状态下无法交互！");

        // 方法3: 在屏幕上显示临时文本
        StartCoroutine(ShowTemporaryMessage("Cannot interact in BattleMode!", 2f));
    }

    // 显示临时消息的协程
    private System.Collections.IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        // 这里可以创建或显示一个UI文本
        // 例如：创建一个临时的TextMeshPro对象
        GameObject messageObj = new GameObject("TempMessage");
        messageObj.transform.position = transform.position + Vector3.up * 2f;

        TextMeshPro tmp = messageObj.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = 3;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        messageObj.GetComponent<MeshRenderer>().sortingLayerName = "JumpText";

        // 尝试设置字体
        if (customFont != null)
        {
            tmp.font = customFont;
        }

        // 添加淡出效果
        float elapsed = 0f;
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            messageObj.transform.position += Vector3.up * Time.deltaTime * 0.5f;
            yield return null;
        }

        Destroy(messageObj);
    }

    private void TriggerDialogue()
    {
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.battleDialogDataFile = dialogueFile;
            dialogueSystem.StartNewDialogue();
            Debug.Log($"触发对话: {dialogueFile.name}");
        }
        else
        {
            Debug.LogWarning("未找到对话系统！");
        }
    }

    // 新增：在Inspector中快速设置战斗交互属性的方法
    [ContextMenu("设置为战斗可交互")]
    public void SetBattleInterable()
    {
        isBattleInterable = true;
        Debug.Log($"物品 {name} 已设置为战斗可交互");
    }

    [ContextMenu("设置为战斗不可交互")]
    public void SetNotBattleInterable()
    {
        isBattleInterable = false;
        Debug.Log($"物品 {name} 已设置为战斗不可交互");
    }

    // 新增：获取当前交互状态的调试信息
    public string GetInteractStatus()
    {
        bool inBattle = IsInBattleState();
        bool canInteract = isInterable && (!inBattle || (inBattle && isBattleInterable));

        return $"物品: {name}\n" +
               $"可交互: {isInterable}\n" +
               $"战斗可交互: {isBattleInterable}\n" +
               $"当前状态: {(inBattle ? "战斗" : "探索")}\n" +
               $"能否交互: {canInteract}";
    }
}
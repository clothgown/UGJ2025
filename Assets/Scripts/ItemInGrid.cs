using System.Collections.Generic;
using UnityEngine;

public class ItemInGrid : MonoBehaviour
{
    [Tooltip("矩形占用区域的两个对角坐标")]
    public Vector2Int cornerA = Vector2Int.zero;
    public Vector2Int cornerB = Vector2Int.zero;

    [Tooltip("是否是单格物件，如果勾选，只占用cornerA")]
    public bool isSingleCell = false;

    private List<GameGrid> occupiedGrids = new List<GameGrid>();
    public bool isInterable = false;
    public bool isBattleInterable = true;
    public SpriteRenderer sr;

    // 新增部分
    private UnitController[] players;   // 所有玩家
    public float transparentAlpha = 0.75f;
    private float normalAlpha = 1f;
    private bool isTransparent = false;

    [Header("透明度检测 Layer")]
    public LayerMask blockingLayer;  // Item 所在 Layer

    [Header("对话设置")]
    public TextAsset dialogueFile; // 关联的对话文件
    public bool triggerDialogueOnInteract = true; // 是否触发对话

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
    public virtual void Interact()
    {
        if (!isInterable)
        {
            Debug.LogWarning($"物品 {name} 不可交互，isInterable = {isInterable}");
            return;
        }

        Debug.Log($"与物品交互: {gameObject.name}");

        // 触发对话
        if (triggerDialogueOnInteract && dialogueFile != null)
        {
            TriggerDialogue();
        }

        // 可以在这里添加其他交互逻辑
        // 例如：获得物品、触发事件等
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
}

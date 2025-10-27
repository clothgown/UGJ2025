using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;

    [Header("探索设置")]
    public bool isExplorationMode = false;

    [Header("UI引用")]
    public GameObject cardUIHolder; // 卡牌UI的父对象
    public GameObject healthBarsHolder; // 血条UI的父对象
    public HorizontalCardHolder playerCardHolder; // 卡牌持有器
    public GameObject shengyubushu; //剩余步数
    public GameObject xiayihuihe; //管你是什么反正别显示
    public GameObject cardaction; //管你是什么反正别显示
    public List<GameObject> stelse; //为什么要藏的东西这么多

    private List<ItemInGrid> explorationOnlyItems = new List<ItemInGrid>(); // 只在探索模式下可用的物品

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 只收集那些专门为探索模式设置的物品
        // 不干涉战斗时就可交互的物品
        FindExplorationOnlyItems();
    }

    private void Update()
    {

    }
    // 开始探索模式
    public void StartExploration()
    {
        isExplorationMode = true;
        Debug.Log("进入探索模式");

            
        // 隐藏战斗UI
        HideBattleUI();

        // 设置无限行动点
        SetInfiniteActionPoints();

        // 收回所有卡片到noselect状态
        ResetAllCardsToNoSelect();

        // 启用探索专用物品
        EnableExplorationOnlyItems();
        if (IsoGrid2D.instance != null)
        {
            IsoGrid2D.instance.isWaitingForGridClick = false;
            IsoGrid2D.instance.ClearHighlight();
        }

        // 强制设置控制器
        if (TurnManager.instance != null && TurnManager.instance.currentController != null)
        {
            IsoGrid2D.instance.controller = TurnManager.instance.currentController.gameObject;
        }

        UnitController[] unitControllers = FindObjectsOfType<UnitController>();
        foreach (UnitController unitController in unitControllers)
        {
            if (unitController.transform.name == "Player")
            {
                Debug.Log(1);
                unitController.Move();
            }
        }

    }

    // 结束探索模式
    public void EndExploration()
    {
        isExplorationMode = false;
        Debug.Log("结束探索模式");

        // 显示战斗UI
        ShowBattleUI();

        // 恢复正常行动点
        RestoreNormalActionPoints();

        // 禁用探索专用物品
        DisableExplorationOnlyItems();
    }

    // 只查找专门为探索模式设置的物品
    private void FindExplorationOnlyItems()
    {
        ItemInGrid[] allItems = FindObjectsOfType<ItemInGrid>();
        foreach (ItemInGrid item in allItems)
        {
            // 只管理那些明确标记为探索专用的物品
            // 例如：宝箱、隐藏道具等，而不是战斗时就可交互的Exit
            if (item.isInterable && ShouldBeExplorationOnly(item))
            {
                explorationOnlyItems.Add(item);
                // 初始时禁用交互（只在探索模式启用）
                item.isInterable = false;

                // 清除之前设置的可交互格子颜色
                ClearInteractableGrids(item);
            }
        }
        Debug.Log($"找到 {explorationOnlyItems.Count} 个探索专用物品");
    }

    // 判断物品是否应该只在探索模式下可用
    private void InitializeExplorationItems()
    {
        ItemInGrid[] allItems = FindObjectsOfType<ItemInGrid>();
        foreach (ItemInGrid item in allItems)
        {
            // 只管理那些明确标记为探索专用的物品
            // 不要影响战斗时可交互的物品（如Exit）
            if (item.isInterable && ShouldBeExplorationOnly(item))
            {
                explorationOnlyItems.Add(item);
                // 初始时只在非探索模式下禁用
                if (!isExplorationMode)
                {
                    item.isInterable = false;
                    ClearInteractableGrids(item);
                }
            }
            else if (item.isInterable)
            {
                // 战斗时可交互物品（如Exit）保持启用状态
                Debug.Log($"保持战斗可交互物品: {item.gameObject.name}");
            }
        }
        Debug.Log($"找到 {explorationOnlyItems.Count} 个探索专用物品");
    }

    private bool ShouldBeExplorationOnly(ItemInGrid item)
    {
        // Exit 物品在任何模式下都应该可用
        if (item is Exit)
        {
            Debug.Log($"跳过Exit物品: {item.gameObject.name}");
            return false;
        }

        // 其他逻辑根据你的需求调整
        // 例如：return item.CompareTag("ExplorationOnly");
        return false; // 暂时让所有物品在战斗时都可用
    }
    private void EnableExplorationOnlyItems()
    {
        foreach (ItemInGrid item in explorationOnlyItems)
        {
            if (item != null)
            {
                item.isInterable = true;
                // 设置可交互格子（显示蓝色高亮）
                item.SetCanInteract(item.cornerA, item.isSingleCell ? item.cornerA : item.cornerB);
            }
        }
    }

    // 禁用探索专用物品
    private void DisableExplorationOnlyItems()
    {
        foreach (ItemInGrid item in explorationOnlyItems)
        {
            if (item != null)
            {
                item.isInterable = false;
                // 清除可交互格子颜色
                ClearInteractableGrids(item);
            }
        }
    }

    // 清除可交互格子颜色
    private void ClearInteractableGrids(ItemInGrid item)
    {
        Vector2Int a = item.cornerA;
        Vector2Int b = item.isSingleCell ? item.cornerA : item.cornerB;

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
                    gridComp.SetColor(Color.white); // 恢复默认颜色
                    gridComp.canDialogue = false;
                }
            }
        }
    }

    // 隐藏战斗UI
    private void HideBattleUI()
    {
        // 隐藏卡牌UI
        if (cardUIHolder != null)
            cardUIHolder.SetActive(false);

        // 隐藏血条
        if (healthBarsHolder != null)
            healthBarsHolder.SetActive(false);

        // 隐藏行动点显示
        if (TurnManager.instance != null && TurnManager.instance.actionPointText != null)
            TurnManager.instance.actionPointText.gameObject.SetActive(false);

        if (shengyubushu != null)
            shengyubushu.SetActive(false);

        if (xiayihuihe != null)
            xiayihuihe.SetActive(false);

        if (cardaction != null)
            cardaction.SetActive(false);

        foreach (GameObject cardElement in stelse)
        {
            if (cardElement != null)
            {
                cardElement.SetActive(false);
            }
        }
    }

    // 显示战斗UI
    private void ShowBattleUI()
    {
        // 显示卡牌UI
        if (cardUIHolder != null)
            cardUIHolder.SetActive(true);

        // 显示血条
        if (healthBarsHolder != null)
            healthBarsHolder.SetActive(true);

        // 显示行动点显示
        if (TurnManager.instance != null && TurnManager.instance.actionPointText != null)
            TurnManager.instance.actionPointText.gameObject.SetActive(true);

        if (shengyubushu != null)
            shengyubushu.SetActive(true);

        if (xiayihuihe != null)
            xiayihuihe.SetActive(true);

        if (cardaction != null)
            cardaction.SetActive(true);

        foreach (GameObject cardElement in stelse)
        {
            if (cardElement != null)
            {
                cardElement.SetActive(true);
            }
        }
    }

    // 设置无限行动点
    private void SetInfiniteActionPoints()
    {
        UnitController[] players = FindObjectsOfType<UnitController>();
        foreach (UnitController player in players)
        {
            if (player != null && !player.IsDead())
            {
                // 设置一个很大的行动点数，模拟无限
                player.SetActionPoint(999);
                player.maxActionPoints = 999;
            }
        }
    }

    // 恢复正常行动点
    private void RestoreNormalActionPoints()
    {
        UnitController[] players = FindObjectsOfType<UnitController>();
        foreach (UnitController player in players)
        {
            if (player != null && !player.IsDead())
            {
                // 恢复原来的最大行动点
                player.maxActionPoints = 3;
                player.RecoverActionPoint();
            }
        }
    }

    // 收回所有卡片到noselect状态
    private void ResetAllCardsToNoSelect()
    {
        if (TurnManager.instance != null && TurnManager.instance.playerCards != null)
        {
            foreach (GameObject card in TurnManager.instance.playerCards)
            {
                if (card != null)
                {
                    CardFocusState focusState = card.GetComponent<CardFocusState>();
                    if (focusState != null)
                    {
                        focusState.isFocused = false;
                    }

                    // 隐藏UILogo（选中状态显示的内容）
                    Transform uiLogo = card.transform.Find("UIlogo");
                    if (uiLogo != null)
                        uiLogo.gameObject.SetActive(false);
                }
            }
        }
    }

    // 检查是否在探索模式
    public static bool IsInExploration()
    {
        return Instance != null && Instance.isExplorationMode;
    }

    // 探索结束并返回
    public void EndExplorationAndReturn()
    {
        EndExploration();

        // 给予战斗奖励
        if (CollectionManager.instance != null)
            CollectionManager.instance.AddCoin(5);
        if (PanelManager.instance != null)
            PanelManager.instance.ShowEndPanel();

        Debug.Log("探索结束，返回地图");
    }

}
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public enum TurnPhase { PlayerTurn, EnemyTurn }

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public UnitController currentController;
    public TurnPhase phase = TurnPhase.PlayerTurn;
    public int turnIndex = 1;

    [SerializeField] private HorizontalCardHolder playerCardHolder;
    public TextMeshProUGUI actionPointText; // 全局行动点文本

    [Header("角色与UI卡片对应")]
    public UnitController[] unitControllers; // 每个角色
    public List<GameObject> playerCards; // 每个角色对应的UI卡片（顺序一一对应）

    [Header("卡片颜色设置")]
    public Color deadCardColor = new Color(0.3f, 0.3f, 0.3f, 0.7f); // 死亡卡片颜色
    public Color aliveCardColor = Color.white; // 存活卡片颜色

    private int focusedIndex = -1; // 当前选中卡片索引
    private float selectedXPosition = 840f; // 选中卡片的X位置
    private float noselectXPosition = 890f; // 未选中卡片的X位置
    private int previousActionPoints = -1; // 记录上一次的行动点数值

    public EnemyUnit[] enemies ;
    public bool isWin = false;
    
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        unitControllers = FindObjectsOfType<UnitController>();

        // 为所有单位订阅死亡事件
        foreach (var unit in unitControllers)
        {
            HealthSystem healthSystem = unit.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.OnDeath += OnUnitDeath;
            }
        }

        // 初始化卡片行动点显示
        InitializeCardActionPoints();

        StartCoroutine(RunTurnLoop());
        
        enemies = FindObjectsOfType<EnemyUnit>();
    }

    private void Update()
    {
        
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        if (enemies.Length == 0)
        {
            isWin = true;
        }
        
    }
    // 初始化卡片行动点显示
    private void InitializeCardActionPoints()
    {
        if (playerCards == null || unitControllers == null) return;

        for (int i = 0; i < Mathf.Min(playerCards.Count, unitControllers.Length); i++)
        {
            if (playerCards[i] != null && unitControllers[i] != null)
            {
                UpdateCardActionPointUI(unitControllers[i]);
            }
        }
    }

    public void ChangePlayer(UnitController player)
    {
        if (player == null || player.IsDead() || !player.isActive)
        {
            Debug.Log($"玩家 {player?.name} 已死亡或不可用，尝试切换下一个");
            FindNextAlivePlayer();
            return;
        }

        // 原有切换逻辑...
        IsoGrid2D.instance.ClearHighlight();
        currentController = player;
        IsoGrid2D.instance.controller = player.gameObject;
        IsoGrid2D.instance.currentPlayerGrid = player.startGrid.GetComponent<GameGrid>();

        // 更新全局行动点显示
        UpdateActionPointUI(player.actionPoints);

        CameraMove.instance.ChangeFollow(player.gameObject);
        player.Move();
        PlayerSwitchManager.instance.currentUnitController = player;

        var psm = PlayerSwitchManager.instance;
        if (psm != null)
        {
            psm.currentUnitController = player;
            int slotIndex = psm.allSlots.FindIndex(s => s.unit == player);
            if (slotIndex >= 0)
                psm.currentIndex = slotIndex;
            else
                Debug.LogWarning("玩家未在 PlayerSwitchManager 的 slots 中找到！");
        }

        // 更新卡片 UI 状态
        UpdateCardSelectionUI(player);
    }
    private void OnUnitDeath(UnitController deadUnit)
    {
        Debug.Log($"检测到单位死亡: {deadUnit.name}");

        // 更新死亡角色的卡片
        UpdateDeadUnitCard(deadUnit);

        // 如果当前控制的角色死亡，切换到下一个存活角色
        if (currentController == deadUnit)
        {
            FindNextAlivePlayer();
        }

        // 检查游戏是否结束（所有玩家死亡）
        CheckGameOver();
    }
    private void UpdateDeadUnitCard(UnitController deadUnit)
    {
        int deadIndex = System.Array.IndexOf(unitControllers, deadUnit);
        if (deadIndex >= 0 && deadIndex < playerCards.Count)
        {
            GameObject card = playerCards[deadIndex];
            if (card != null)
            {
                // 改变卡片颜色为灰色
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.color = deadCardColor;
                }

                // 改变卡片上所有文本颜色
                TextMeshProUGUI[] texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    text.color = deadCardColor;
                }

                // 禁用卡片的交互（如果有）
                Button button = card.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }

                Debug.Log($"已更新死亡单位 {deadUnit.name} 的卡片外观");
            }
        }
    }

    private void CheckGameOver()
    {
        bool allDead = true;
        foreach (var unit in unitControllers)
        {
            if (unit != null && !unit.IsDead())
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            Debug.Log("所有玩家都已死亡，游戏结束");
            HandleGameOver();
        }
    }
    private void FindNextAlivePlayer()
    {
        foreach (var unit in unitControllers)
        {
            if (unit != null && !unit.IsDead() && unit.isActive)
            {
                ChangePlayer(unit);
                Debug.Log($"自动切换到存活角色: {unit.name}");
                return;
            }
        }

        // 如果没有存活的玩家
        Debug.Log("没有存活的玩家可以切换");
        HandleGameOver();
    }


    private void HandleGameOver()
    {
        // 显示游戏结束界面
        // 重新开始游戏或返回主菜单
        // Gameover.gameObject.setactive(true);
    }

    public void StartPlayerTurn()
    {
        FindAnyObjectByType<NextTurnButton>().RestoreButton();

        // 只重置存活角色的行动点
        foreach (var unitController in unitControllers)
        {
            if (unitController != null && !unitController.IsDead())
            {
                unitController.RecoverActionPoint();
            }
        }

        // 查找第一个存活的玩家
        UnitController firstAlivePlayer = null;
        foreach (var unit in unitControllers)
        {
            if (unit != null && !unit.IsDead() && unit.isActive)
            {
                firstAlivePlayer = unit;
                break;
            }
        }

        if (firstAlivePlayer != null)
        {
            ChangePlayer(firstAlivePlayer);
            UpdateCardSelectionUI(firstAlivePlayer);
        }
        else
        {
            HandleGameOver();
        }

        // 清空护盾（只对存活玩家）
        foreach (UnitController player in FindObjectsOfType<UnitController>())
        {
            if (!player.IsDead())
            {
                player.shield = 0;
                player.healthSystem.SetShield(0);
            }
        }

        Debug.Log("Player Turn Started!");
    }

    private IEnumerator RunTurnLoop()
    {
        while (true)
        {
            switch (phase)
            {
                case TurnPhase.PlayerTurn:
                    StartPlayerTurn();
                    yield return new WaitUntil(() => phase != TurnPhase.PlayerTurn);
                    break;

                case TurnPhase.EnemyTurn:
                    
                    yield return StartCoroutine(EnemyTurn());
                    phase = TurnPhase.PlayerTurn;
                    turnIndex++;
                    break;
            }
            yield return null;
        }
    }

    public void EndPlayerTurn()
    {
        
        if (IsoGrid2D.instance.isWaitingForGridClick) return;
        phase = TurnPhase.EnemyTurn;

        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        if (enemies.Length == 0)
        {
            Debug.Log("没有敌人了，返回地图场景");
            CollectionManager.instance.AddCoin(5);
            PanelManager.instance.ShowEndPanel();
        }
    }

    private IEnumerator EnemyTurn()
    {
        
        
        foreach (var enemy in enemies)
        {
            
            if (enemy == null) continue;
            if (enemy.isDizziness)
            {
                enemy.Recover();
                continue;
            }

            enemy.ChasePlayer();
            yield return new WaitForSeconds(1.5f);
            SoundManager.Instance.PlaychangeturnAudio();

        }
    }

    // ✅ 重载方法：支持从UnitController调用
    public void UpdateActionPointUI(UnitController controller)
    {
        if (controller == null) return;

        // 更新全局行动点显示
        UpdateActionPointUI(controller.actionPoints);

        // 同时更新卡片上的行动点显示
        UpdateCardActionPointUI(controller);
    }

    // ✅ 保留之前的行动点UI更新（只在数值改变时产生缩放效果）
    public void UpdateActionPointUI(int points)
    {
        if (actionPointText == null) return;

        // 检查数值是否真的改变了
        bool pointsChanged = points != previousActionPoints;

        // 更新文本内容
        actionPointText.text = points.ToString();

        // 停止之前的动画
        actionPointText.DOKill();
        actionPointText.transform.DOKill();

        // 只有在数值改变时才执行缩放动画
        if (pointsChanged)
        {
            // 缩放提示动画
            actionPointText.transform.DOScale(1.4f, 0.12f).OnComplete(() =>
            {
                actionPointText.transform.DOScale(1f, 0.12f);
            });
        }

        // 颜色和透明度效果（无论数值是否改变都执行）
        if (points == 1)
        {
            actionPointText.color = Color.red;
            actionPointText.DOFade(0.3f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else if (points <= 0)
        {
            // 灰色，停止闪烁
            actionPointText.DOKill();
            actionPointText.color = Color.gray;
            actionPointText.alpha = 1f;
        }
        else
        {
            // 普通显示
            actionPointText.DOKill();
            actionPointText.color = Color.black;
            actionPointText.alpha = 1f;
        }

        // 更新记录的上一次数值
        previousActionPoints = points;
    }

    // ✅ 更新卡片上的行动点显示
    public void UpdateCardActionPointUI(UnitController controller)
    {
        if (controller == null || playerCards == null || playerCards.Count == 0)
            return;

        int index = System.Array.IndexOf(unitControllers, controller);
        if (index < 0 || index >= playerCards.Count) return;

        var card = playerCards[index];

        // 在卡片上查找行动点文本组件
        TextMeshProUGUI tmp = FindActionPointTextInCard(card);
        if (tmp == null)
        {
            Debug.LogWarning($"在卡片 {card.name} 上找不到行动点文本组件");
            return;
        }

        tmp.text = controller.actionPoints.ToString();

        tmp.transform.DOKill();
        tmp.transform.localScale = Vector3.one;
        tmp.transform.DOScale(1.3f, 0.15f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);

        tmp.DOKill();
        if (controller.actionPoints == 1)
        {
            tmp.DOColor(Color.red, 0.3f).SetLoops(-1, LoopType.Yoyo);
        }
        else if (controller.actionPoints <= 0)
        {
            tmp.color = Color.gray;
        }
        else
        {
            tmp.color = Color.black;
        }
    }

    // 在卡片中查找行动点文本组件
    private TextMeshProUGUI FindActionPointTextInCard(GameObject card)
    {
        // 按照路径查找：先找到UILogo，然后找到其子对象ActionPointText
        Transform uiLogoTransform = card.transform.Find("UIlogo");
        if (uiLogoTransform != null)
        {
            Transform actionPointTextTransform = uiLogoTransform.Find("ActionPointText");
            if (actionPointTextTransform != null)
            {
                return actionPointTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // 如果没找到，尝试直接在整个卡片中查找（包括非激活）
        TextMeshProUGUI tmp = card.GetComponentInChildren<TextMeshProUGUI>(true);
        return tmp;
    }

    // ✅ 控制卡片选中/未选中动画（使用selected和noselect，并控制位置）
    public void UpdateCardSelectionUI(UnitController selectedController)
    {
        if (playerCards == null || playerCards.Count == 0) return;

        int selectedIndex = System.Array.IndexOf(unitControllers, selectedController);
        if (selectedIndex < 0) return;

        focusedIndex = selectedIndex;

        for (int i = 0; i < playerCards.Count; i++)
        {
            GameObject card = playerCards[i];
            if (card == null) continue;

            bool isSelected = (i == focusedIndex);

            // 添加状态组件（可用于外部判断）
            CardFocusState state = card.GetComponent<CardFocusState>();
            if (state == null) state = card.AddComponent<CardFocusState>();
            state.isFocused = isSelected;

            // 控制卡片位置
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float targetX = isSelected ? selectedXPosition : noselectXPosition;

                // 使用DOTween平滑移动位置
                rectTransform.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutBack);
            }

            // ✅ 控制UILogo的显示/隐藏
            Transform uiLogoTransform = card.transform.Find("UIlogo");
            if (uiLogoTransform != null)
            {
                uiLogoTransform.gameObject.SetActive(isSelected);
            }
            else
            {
                // 如果直接找不到，尝试在子对象中查找
                uiLogoTransform = card.transform.Find("UIlogo");
                if (uiLogoTransform != null)
                {
                    uiLogoTransform.gameObject.SetActive(isSelected);
                }
            }

            // 获取DOTween动画
            DOTweenAnimation[] tweens = card.GetComponents<DOTweenAnimation>();
            if (tweens == null || tweens.Length == 0) continue;

            foreach (var tween in tweens)
            {
                // ✅ 只播放你指定的动画
                //if (isSelected && tween.id == "selected")
                //{
                //    tween.DORestart();
                //}
                //else if (!isSelected && tween.id == "noselect")
                //{
                //    tween.DORestart();
                //}
                //else
                //{
                //    // 停止其他动画
                //    tween.DOPause();
                //}

            }
        }
    }
    private void OnDestroy()
    {
        foreach (var unit in unitControllers)
        {
            if (unit != null)
            {
                HealthSystem healthSystem = unit.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.OnDeath -= OnUnitDeath;
                }
            }
        }
    }
}

// ✅ 状态组件，用来记录卡片当前是否选中
public class CardFocusState : MonoBehaviour
{
    public bool isFocused = false;
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public enum TurnPhase { PlayerTurn, EnemyTurn ,Exploration }

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public UnitController currentController;
    public TurnPhase phase = TurnPhase.PlayerTurn;
    private bool isCheckingForWin = false;
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

    public static System.Action<int> OnTurnStart;
    public static System.Action<int> OnTurnEnd;

    public GameObject Gameover;
    [Header("关键角色设置")]
    public UnitController[] criticalCharacters; // 关键角色列表
    public bool gameOverOnCriticalDeath = true; // 关键角色死亡是否导致游戏结束

    [Header("探索模式")]
    public ExplorationManager explorationManager;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        unitControllers = FindObjectsOfType<UnitController>();
        

        // 确保ExplorationManager引用
        if (explorationManager == null)
        {
            explorationManager = FindObjectOfType<ExplorationManager>();
            if (explorationManager == null)
            {
                Debug.LogError("未找到ExplorationManager！请确保场景中有ExplorationManager组件");
            }
            else
            {
                Debug.Log($"找到ExplorationManager: {explorationManager.gameObject.name}");
            }
        }
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

        enemies = FindObjectsOfType<EnemyUnit>();
        if (phase == TurnPhase.PlayerTurn && enemies.Length == 0 && !isWin && !isCheckingForWin)
        {
            isCheckingForWin = true;
            Debug.Log($"检测到所有敌人被消灭，准备进入探索模式");
            StartCoroutine(HandleVictory());
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

    public void OnCriticalCharacterDeath(UnitController deadCharacter)
    {
        if (!gameOverOnCriticalDeath) return;

        Debug.Log($"关键角色 {deadCharacter.characterName} 死亡，触发游戏结束");

        // 停止所有协程
        StopAllCoroutines();

        // 禁用所有输入
        IsoGrid2D.instance.isWaitingForGridClick = true;

        // 显示游戏结束界面
    }
    public void HandleGameOver()
    {
        Gameover.gameObject.SetActive(true);
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
            if (ExplorationManager.IsInExploration() || phase == TurnPhase.Exploration)
            {
                yield return null;
                continue;
            }

            switch (phase)
            {
                case TurnPhase.PlayerTurn:
                    OnTurnStart?.Invoke(turnIndex);
                    StartPlayerTurn();
                    yield return new WaitUntil(() => phase != TurnPhase.PlayerTurn);
                    Debug.Log($"玩家回合结束，进入 {phase}");
                    break;

                case TurnPhase.EnemyTurn:
                    // 进入敌人回合前再次检查
                    enemies = FindObjectsOfType<EnemyUnit>();
                    if (enemies.Length == 0 && !isWin)
                    {
                        StartCoroutine(HandleVictory());
                        yield break; // 退出敌人回合
                    }

                    yield return StartCoroutine(EnemyTurn());

                    // 敌人回合结束后检查
                    enemies = FindObjectsOfType<EnemyUnit>();
                    if (enemies.Length == 0 && !isWin)
                    {
                        StartCoroutine(HandleVictory());
                        yield break;
                    }

                    OnTurnEnd?.Invoke(turnIndex);
                    phase = TurnPhase.PlayerTurn;
                    turnIndex++;
                    break;
            }
            yield return null;
        }
    }




    private IEnumerator HandleVictory()
    {
        isWin = true;
        Debug.Log("战斗胜利，准备进入探索模式");

        // 确保当前是玩家回合
        phase = TurnPhase.Exploration;

        // 清空高亮等战斗状态
        IsoGrid2D.instance.ClearHighlight();
        IsoGrid2D.instance.isWaitingForGridClick = true;

        // 稍微延迟一帧确保状态切换完成
        yield return null;

        // 启动探索模式
        if (explorationManager != null)
        {
            Debug.Log("调用 ExplorationManager.StartExploration()");
            explorationManager.StartExploration();
        }
        else
        {
            Debug.LogError("ExplorationManager 未找到！");
        }

        isCheckingForWin = false;
    }

    public void EndPlayerTurn()
    {
        if (IsoGrid2D.instance.isWaitingForGridClick) return;
        if (isWin) return; // 如果已经胜利，不执行敌人回合

        // 检查是否还有敌人
        enemies = FindObjectsOfType<EnemyUnit>();
        if (enemies.Length == 0 && !isWin)
        {
            StartCoroutine(HandleVictory());
            return;
        }

        phase = TurnPhase.EnemyTurn;
        Debug.Log($"进入敌人回合，敌人数量: {enemies.Length}");
    }

    private IEnumerator StartExplorationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"开始探索模式，ExplorationManager: {explorationManager != null}");

        // 进入探索模式
        if (explorationManager != null)
        {
            explorationManager.StartExploration();
            Debug.Log("探索模式已成功启动");
        }
        else
        {
            Debug.LogError("ExplorationManager 未设置！请检查场景中的ExplorationManager");
        }
    }

    private IEnumerator EnemyTurn()
    {
        // 再次检查敌人数组
        enemies = FindObjectsOfType<EnemyUnit>();
        if (enemies.Length == 0)
        {
            Debug.Log("敌人回合开始但无敌人，直接胜利");
            yield break;
        }

        bool hasEnemyActed = false;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.isDizziness)
            {
                Debug.Log($"敌人 {enemy.name} 处于眩晕状态，跳过行动");
                enemy.Recover();
                continue;
            }

            Debug.Log($"敌人 {enemy.name} 开始行动");
            enemy.ChasePlayer();
            hasEnemyActed = true;
            yield return new WaitForSeconds(1.5f);
            SoundManager.Instance.PlaychangeturnAudio();
        }

        // 如果没有敌人行动，立即结束回合
        if (!hasEnemyActed)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("敌人回合结束");
    }

    // ✅ 重载方法：支持从UnitController调用
    public void UpdateActionPointUI(UnitController controller)
    {
        if (controller == null) return;

        if (ExplorationManager.IsInExploration())
            return;
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

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;

public enum TurnPhase { PlayerTurn, EnemyTurn }

[System.Serializable]
public class PlayerUIControl
{
    public GameObject uiObject; // UI控件对象
    public UnitController associatedPlayer; // 关联的玩家
    [HideInInspector] public DOTweenAnimation[] animations; // 该对象上的所有DOTween动画
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public UnitController currentController;
    public TurnPhase phase = TurnPhase.PlayerTurn;
    public int turnIndex = 1;
    public EnemyUnit enemyUnit;

    [SerializeField] private HorizontalCardHolder playerCardHolder; // 玩家手牌管理器

    public TextMeshProUGUI actionPointText; // 当前显示的行动点数字
    public UnitController[] unitControllers;

    // UI动画控制相关
    public List<PlayerUIControl> playerUIControls = new List<PlayerUIControl>();

    private int previousActionPoints = -1; // 记录上一次的行动点数值

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        unitControllers = FindObjectsOfType<UnitController>();

        // 初始化UI控件的动画组件
        InitializeUIAnimations();

        StartCoroutine(RunTurnLoop());
    }

    // 初始化UI动画组件
    private void InitializeUIAnimations()
    {
        foreach (var control in playerUIControls)
        {
            if (control.uiObject != null)
            {
                control.animations = control.uiObject.GetComponents<DOTweenAnimation>();
            }
        }
    }

    // ✅ 更新 UI（只在数值改变时产生缩放效果）
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

    public void ChangePlayer(UnitController player)
    {
        if (player.isActive == false) return;

        IsoGrid2D.instance.ClearHighlight();
        currentController = player;
        IsoGrid2D.instance.controller = currentController.gameObject;
        IsoGrid2D.instance.currentPlayerGrid = currentController.startGrid.GetComponent<GameGrid>();

        // ✅ 改为调用 UI 更新函数
        UpdateActionPointUI(currentController.actionPoints);

        CameraMove.instance.ChangeFollow(player.gameObject);
        currentController.Move();
        PlayerSwitchManager.instance.currentUnitController = currentController;

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

        // 更新UI控件动画
        UpdateUIAnimations();
    }

    // 更新UI控件动画
    private void UpdateUIAnimations()
    {
        if (currentController == null) return;

        foreach (var control in playerUIControls)
        {
            if (control.uiObject == null) continue;

            // 检查是否是当前玩家
            bool isCurrentPlayer = control.associatedPlayer == currentController;

            if (isCurrentPlayer)
            {
                // 播放当前玩家的selected动画
                PlayAnimation(control, "selected");
            }
            else
            {
                // 检查x位置是否为890
                RectTransform rectTransform = control.uiObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    bool isAtTargetPosition = Mathf.Approximately(rectTransform.anchoredPosition.x, 890f);

                    if (!isAtTargetPosition)
                    {
                        // 如果不在目标位置，播放noselect动画
                        PlayAnimation(control, "noselect");
                    }
                    else
                    {
                        // 如果在目标位置，停止所有动画
                        StopAllAnimations(control);
                    }
                }
            }
        }
    }

    private void PlayAnimation(PlayerUIControl control, string animationId)
    {
        if (control.animations == null) return;

        foreach (var animation in control.animations)
        {
            if (animation != null && animation.id == animationId)
            {
                // 停止动画再重新播放，确保每次都能触发
                animation.DORestart();
                animation.DOPlay();
            }
        }
    }

    private void StopAllAnimations(PlayerUIControl control)
    {
        if (control.animations == null) return;

        foreach (var animation in control.animations)
        {
            if (animation != null)
            {
                animation.DOPause();
            }
        }
    }

    // 手动触发UI动画更新
    public void ForceUpdateUIAnimations()
    {
        UpdateUIAnimations();
    }

    public void StartPlayerTurn()
    {
        FindAnyObjectByType<NextTurnButton>().RestoreButton();

        // 重置每个角色行动点
        foreach (var unitController in unitControllers)
        {
            unitController.RecoverActionPoint();
        }

        if (unitControllers != null && unitControllers.Length > 0)
        {
            ChangePlayer(unitControllers[0]);
            UpdateActionPointUI(unitControllers[0].actionPoints);
        }

        // 清空护盾
        foreach (UnitController player in FindObjectsOfType<UnitController>())
        {
            player.shield = 0;
            player.healthSystem.SetShield(0);
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
        if (IsoGrid2D.instance.isWaitingForGridClick == true) return;
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
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                if (enemy.isDizziness)
                {
                    enemy.Recover();
                    continue;
                }
                enemy.ChasePlayer();
                yield return new WaitForSeconds(1.5f);
            }
        }
        Debug.Log("敌人回合结束");
    }

    // 在Inspector中添加新的UI控件
    [ContextMenu("Add New UI Control")]
    private void AddNewUIControl()
    {
        playerUIControls.Add(new PlayerUIControl());
    }
}
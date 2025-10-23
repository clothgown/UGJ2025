using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TurnPhase { PlayerTurn, EnemyTurn }


public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public UnitController currentController;
    public TurnPhase phase = TurnPhase.PlayerTurn;
    public int turnIndex = 1;
    public EnemyUnit enemyUnit;

    [SerializeField] private HorizontalCardHolder playerCardHolder; // 玩家手牌管理器


    public int actionPoints;          // 当前行动点
    public TextMeshProUGUI actionPointText;

    public UnitController[] unitControllers;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }


    public void ChangePlayer(UnitController player)
    {
        if (player.isActive == false) return;
        IsoGrid2D.instance.ClearHighlight();
        currentController = player;
        IsoGrid2D.instance.controller = currentController.gameObject;
        IsoGrid2D.instance.currentPlayerGrid = currentController.startGrid.GetComponent<GameGrid>();
        actionPointText.text = "[" + currentController.name + "]" + "Action Point: " + currentController.actionPoints;
        CameraMove.instance.ChangeFollow(player.gameObject);
        currentController.Move();
        PlayerSwitchManager.instance.currentUnitController = currentController;
        var psm = PlayerSwitchManager.instance;
        if (psm != null)
        {
            psm.currentUnitController = player;

            // 找到 player 对应的 slot index
            int slotIndex = psm.allSlots.FindIndex(s => s.unit == player);
            if (slotIndex >= 0)
            {
                psm.currentIndex = slotIndex;
            }
            else
            {
                Debug.LogWarning("玩家未在 PlayerSwitchManager 的 slots 中找到！");
            }
        }
    }
    public void StartPlayerTurn()
    {
        FindAnyObjectByType<NextTurnButton>().RestoreButton();
        // 回合开始时重置行动点
        foreach (var unitController in unitControllers) 
        {
            unitController.RecoverActionPoint();
            //unitController.RecoverState();
        }
        actionPointText.text = "[" + unitControllers[0].name +"]"+"Action Point: " + unitControllers[0].actionPoints;
        ChangePlayer(unitControllers[0]);
        // --- 回合开始时抽一张卡 ---
        //if (playerCardHolder != null)
        //    playerCardHolder.StartCoroutine(playerCardHolder.DrawNewCard());
        UnitController[] players = FindObjectsOfType<UnitController>();
        foreach(UnitController player in players)
        {
            player.shield = 0;
            player.healthSystem.SetShield(0);
        }
        Debug.Log("Player Turn Started!");
    }

    private void Start()
    {
        unitControllers = FindObjectsOfType<UnitController>();
        StartCoroutine(RunTurnLoop());
    }


    private IEnumerator RunTurnLoop()
    {
        while (true)
        {
            switch (phase)
            {
                case TurnPhase.PlayerTurn:
                    Debug.Log("玩家回合开始");
                    StartPlayerTurn();
                    // 等待玩家结束回合按钮
                    yield return new WaitUntil(() => phase != TurnPhase.PlayerTurn);
                    break;


                case TurnPhase.EnemyTurn:
                    Debug.Log("敌人回合开始");
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
        // --- 检查场上是否还有敌人 ---
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();

        // 如果还有敌人，执行敌人AI逻辑
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                if(enemy.isDizziness == true)
                {
                    enemy.Recover();
                    continue;
                }
                enemy.ChasePlayer();
                yield return new WaitForSeconds(1.5f); // 等待敌人行动完成
            }
        }

        Debug.Log("敌人回合结束");
    }
}
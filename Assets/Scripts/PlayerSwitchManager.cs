using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

[System.Serializable]
public class UnitSlot
{
    public UnitController unit; // 角色
    public Button button;       // 对应按钮
    public bool isActive;       // 是否在场上

    public UnitSlot(UnitController unit, Button button, bool isActive = false)
    {
        this.unit = unit;
        this.button = button;
        this.isActive = isActive;
    }
}

public class PlayerSwitchManager : MonoBehaviour
{
    public static PlayerSwitchManager instance;

    public List<UnitSlot> allSlots = new List<UnitSlot>();

    public UnitController currentUnitController;
    public int currentIndex = 0;
    public Vector2Int currentUnitGrid;

    public EnemyUnit[] enemyUnits;
    public bool isChoosing = false;

    public CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        enemyUnits = FindObjectsOfType<EnemyUnit>();

        // 绑定按钮点击事件
        for (int i = 0; i < allSlots.Count; i++)
        {
            int index = i; // 闭包捕获
            allSlots[i].button.onClick.AddListener(() => ChangePlayer(index));
        }

        // 初始化角色状态：UnitController.isActive 与 UnitSlot.isActive 同步
        for (int i = 0; i < allSlots.Count; i++)
        {
            allSlots[i].unit.isActive = allSlots[i].isActive;
            allSlots[i].unit.gameObject.SetActive(allSlots[i].isActive);
        }

        // 默认选择第一个在场角色
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (allSlots[i].isActive)
            {
                currentUnitController = allSlots[i].unit;
                currentIndex = i;
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = currentUnitController.transform;
                }
                break;
            }
        }

        // 初始化按钮状态（在场按钮可点）
        UpdateButtonStates();
    }

    public void ChangePlayer(int newIndex)
    {
        if (newIndex < 0 || newIndex >= allSlots.Count)
            return;

        var newSlot = allSlots[newIndex];
        var currentSlot = allSlots[currentIndex];

        if (!isChoosing || newSlot.isActive) return;

        currentUnitGrid = currentUnitController.currentGridPos;
        int remainingAP = currentUnitController.actionPoints;

        // 互换在场状态
        currentSlot.isActive = false;
        currentSlot.unit.isActive = false; // 同步 UnitController
        newSlot.isActive = true;
        newSlot.unit.isActive = true;      // 同步 UnitController
        // 下场当前角色
        currentSlot.unit.gameObject.SetActive(false);

        // 上场新角色
        currentUnitController = newSlot.unit;
        currentUnitController.SetActionPoint(remainingAP);
        // 🔺 通知 TurnManager 更新UI显示
if (TurnManager.instance != null)
{
    TurnManager.instance.ChangePlayer(currentUnitController);
}

        int currentListIndex = System.Array.IndexOf(TurnManager.instance.unitControllers, currentSlot.unit);
        if (currentListIndex >= 0)
        {
            TurnManager.instance.unitControllers[currentListIndex] = currentUnitController;
        }


        IsoGrid2D.instance.controller = currentUnitController.gameObject;
        currentIndex = newIndex;

        currentUnitController.startPoint = currentUnitGrid;
        currentUnitController.TeleportToGrid(
            IsoGrid2D.instance
                .GetTile(currentUnitGrid.x, currentUnitGrid.y)
                .GetComponent<GameGrid>()
        );
        currentUnitController.gameObject.SetActive(true);

        if (virtualCamera != null)
            virtualCamera.Follow = currentUnitController.transform;

        isChoosing = false;
        UpdateButtonStates();

        Debug.Log($"Switched: {currentSlot.unit.name} ⇄ {newSlot.unit.name}");
    }

    public void SwitchToNextPlayer()
    {
        int startIndex = currentIndex;

        do
        {
            currentIndex = (currentIndex + 1) % allSlots.Count;

            // 检查玩家是否存活
            if (allSlots[currentIndex].unit != null &&
                !allSlots[currentIndex].unit.IsDead() &&
                allSlots[currentIndex].unit.isActive)
            {
                TurnManager.instance.ChangePlayer(allSlots[currentIndex].unit);
                return;
            }

        } while (currentIndex != startIndex);

        // 如果没有存活的玩家
        Debug.Log("没有存活的玩家可以切换");
    }

    public void StartChooseSwitch()
    {
        isChoosing = true;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        for (int i = 0; i < allSlots.Count; i++)
        {
            if (isChoosing)
            {
                // 切换状态：只有后备角色可点
                allSlots[i].button.interactable = !allSlots[i].isActive;
            }
            else
            {
                // 非切换状态：只有当前在场的角色按钮可点
                allSlots[i].button.interactable = allSlots[i].isActive;
            }
        }
    }

}

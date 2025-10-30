using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Tooltip("门所在的格子坐标（可以有多个）")]
    public Vector2Int[] gameGridsPos;

    private UnitController[] allUnits;

    public GameObject choosePanel;
    private GameObject instantiatedPanel;
    public Canvas canvas;

    private Vector2Int? lastTriggeredGrid = null; // 上一次触发的格子
    private UnitController lastTriggerUnit = null; // 哪个单位触发的

    private void Start()
    {
        StartCoroutine(InitAfterDelay());
    }

    private IEnumerator InitAfterDelay()
    {
        // 等待一帧，确保场景内对象都初始化完毕
        yield return null;

        allUnits = FindObjectsOfType<UnitController>();

        // 初始化门格子的视觉效果
        GameGrid[] allGrids = FindObjectsOfType<GameGrid>();
        foreach (var grid in allGrids)
        {
            foreach (var pos in gameGridsPos)
            {
                if (grid.gridPos == pos)
                {
                    
                    grid.isLeaveGrid = true;
                    grid.normalColor = grid.LeaveColor;
                    grid.UpdateGridAppearance();
                    break;
                }
            }
        }
    }


    void Update()
    {
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            Vector2Int? currentDoorGrid = null;

            // 检查该单位是否在门格子上
            foreach (var gridPos in gameGridsPos)
            {
                if (unit.currentGridPos == gridPos)
                {
                    currentDoorGrid = gridPos;
                    break;
                }
            }

            if (currentDoorGrid.HasValue)
            {
                // 如果踏上不同的门格子，则触发新的UI
                if (currentDoorGrid != lastTriggeredGrid)
                {
                    OnUnitEnterNewDoor(unit, currentDoorGrid.Value);
                }
            }
            else
            {
                // 离开所有门格子
                if (lastTriggerUnit == unit)
                {
                    OnUnitLeaveDoor(unit);
                }
            }
        }
    }

    /// <summary>
    /// 当单位进入新的门格子
    /// </summary>
    private void OnUnitEnterNewDoor(UnitController unit, Vector2Int gridPos)
    {
        lastTriggeredGrid = gridPos;
        lastTriggerUnit = unit;

        Debug.Log($"单位 {unit.name} 到达门格子 {gridPos}，触发UI面板");

        if (instantiatedPanel != null)
        {
            Destroy(instantiatedPanel);
            instantiatedPanel = null;
        }

        if (choosePanel != null && canvas != null)
        {
            instantiatedPanel = Instantiate(choosePanel, canvas.transform);
            instantiatedPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Door: choosePanel 或 canvas 未设置！");
        }
    }

    /// <summary>
    /// 当单位离开门格子
    /// </summary>
    private void OnUnitLeaveDoor(UnitController unit)
    {
        Debug.Log($"单位 {unit.name} 离开门格子，UI面板关闭");
        lastTriggeredGrid = null;
        lastTriggerUnit = null;

        if (instantiatedPanel != null)
        {
            Destroy(instantiatedPanel);
            instantiatedPanel = null;
        }
    }
}

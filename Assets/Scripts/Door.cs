using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Tooltip("门所在的格子坐标（可以有多个）")]
    public Vector2Int[] gameGridsPos;

    private UnitController[] allUnits;
    private bool hasTriggered = false;
    private Vector2Int? lastTriggeredGrid = null; // 记录上一次触发 UI 的格子

    public GameObject choosePanel;
    private GameObject instantiatedPanel; // 生成的面板实例
    public Canvas canvas;

    

    private void Start()
    {
        allUnits = FindObjectsOfType<UnitController>();
        // 找到场景中所有 GameGrid
        GameGrid[] allGrids = FindObjectsOfType<GameGrid>();

        foreach (var grid in allGrids)
        {
            foreach (var pos in gameGridsPos)
            {
                if (grid.gridPos == pos)
                {
                    // 设置为离开格子
                    grid.isLeaveGrid = true;
                    grid.normalColor = grid.LeaveColor;

                    // 立即刷新外观
                    grid.UpdateGridAppearance();

                    break; // 匹配到就跳出内层循环
                }
            }
        }
    }

    void Update()
    {
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            bool isOnDoorGrid = false;
            Vector2Int? currentGrid = null;

            // 检查单位是否在任意门格子上
            foreach (var gridPos in gameGridsPos)
            {
                if (unit.currentGridPos == gridPos)
                {
                    isOnDoorGrid = true;
                    currentGrid = gridPos;
                    break;
                }
            }

            if (isOnDoorGrid)
            {
                // 如果单位在门格子上，且与上一次触发的格子不同
                if (!hasTriggered || (currentGrid != lastTriggeredGrid))
                {
                    OnAnyUnitReachDoor(unit, currentGrid.Value);
                }
            }
            else if (hasTriggered)
            {
                // 单位不在任何门格子上，重置触发状态
                ResetTrigger();
            }
        }
    }

    /// <summary>
    /// 任意单位到达门格子时触发
    /// </summary>
    private void OnAnyUnitReachDoor(UnitController triggeringUnit, Vector2Int gridPos)
    {
        hasTriggered = true;
        lastTriggeredGrid = gridPos; // 记录当前触发格子
        Debug.Log($"单位 {triggeringUnit.name} 到达门格子 {gridPos}，准备触发UI面板");

        // 销毁旧的面板（如果存在）
        if (instantiatedPanel != null)
        {
            Destroy(instantiatedPanel);
            instantiatedPanel = null;
        }

        if (choosePanel != null)
        {
            // 实例化新的 UI 面板
            instantiatedPanel = Instantiate(choosePanel, canvas.transform);
            instantiatedPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Door: choosePanel 未设置！");
        }
    }

    /// <summary>
    /// 允许再次触发门逻辑
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        lastTriggeredGrid = null; // 清空上一次触发的格子
        if (instantiatedPanel != null)
        {
            Destroy(instantiatedPanel); // 销毁面板
            instantiatedPanel = null;
        }
        Debug.Log("门触发状态已重置");
    }
}
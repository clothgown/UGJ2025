using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Tooltip("门所在的格子坐标（可以有多个）")]
    public Vector2Int[] gameGridsPos;

    private UnitController[] allUnits;
    private bool hasTriggered = false;

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
        if (hasTriggered) return; // 防止重复触发

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            foreach (var gridPos in gameGridsPos)
            {
                if (unit.currentGridPos == gridPos)
                {
                    OnAnyUnitReachDoor(unit);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 任意单位到达门格子时触发
    /// </summary>
    private void OnAnyUnitReachDoor(UnitController triggeringUnit)
    {
        hasTriggered = true;
        Debug.Log($"单位 {triggeringUnit.name} 到达门格子，准备触发UI面板");
        // ✅ 调用 UI 面板接口
        Debug.Log(1);

        // （如果 UI 关闭时需要重新允许触发，可调用 ResetTrigger()）
    }

    /// <summary>
    /// 允许再次触发门逻辑
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}

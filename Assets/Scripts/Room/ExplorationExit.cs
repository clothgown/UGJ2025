using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit : ItemInGrid
{
    [Header("探索出口")]
    public string exitName = "出口";
    public bool showConfirmationDialogue = true;

    [Header("交互格子设置")]
    public Vector2Int[] interactableGridPositions; // 可交互的格子坐标数组

    void Start()
    {
        isInterable = true;

        // 使用父类的 Occupy 方法来注册格子
        SetupInteractableGrids();
    }

    void SetupInteractableGrids()
    {
        Debug.Log($"开始为 Exit 物体设置交互格子");

        if (interactableGridPositions == null || interactableGridPositions.Length == 0)
        {
            Debug.LogError($"Exit 物体 {name} 没有设置 interactableGridPositions！");
            return;
        }

        // 如果是单格物体，使用第一个位置
        if (interactableGridPositions.Length == 1)
        {
            isSingleCell = true;
            cornerA = interactableGridPositions[0];
            cornerB = interactableGridPositions[0];
        }
        else
        {
            // 如果是多格物体，计算包围盒
            isSingleCell = false;
            Vector2Int min = interactableGridPositions[0];
            Vector2Int max = interactableGridPositions[0];

            foreach (Vector2Int pos in interactableGridPositions)
            {
                min = Vector2Int.Min(min, pos);
                max = Vector2Int.Max(max, pos);
            }

            cornerA = min;
            cornerB = max;
        }

        // 调用父类的 Occupy 方法
        if (isSingleCell)
            Occupy(cornerA, cornerA);
        else
            Occupy(cornerA, cornerB);

        // 设置交互状态
        foreach (GameGrid grid in occupiedGrids)
        {
            grid.isInterable = true;
            grid.canDialogue = true;
            Debug.Log($" Exit 设置格子 {grid.gridPos} 为可交互");
        }

        Debug.Log($" Exit {name} 成功设置 {occupiedGrids.Count} 个交互格子");
    }

    void OnDrawGizmos()
    {
        if (interactableGridPositions == null) return;

        // 绘制所有可交互格子
        foreach (Vector2Int gridPos in interactableGridPositions)
        {
            Gizmos.color = Color.green;
            Vector3 worldPos = GetWorldPositionFromGrid(gridPos);
            Gizmos.DrawWireCube(worldPos, new Vector3(0.8f, 0.8f, 0.1f));
        }
    }

    Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
    {
        if (IsoGrid2D.instance != null)
        {
            return IsoGrid2D.instance.GridToWorld(gridPos.x, gridPos.y, IsoGrid2D.instance.cellSize);
        }

        // 备用计算方法
        float worldX = (gridPos.x - gridPos.y) * 1f;
        float worldY = (gridPos.x + gridPos.y) * 0.5f;
        return new Vector3(worldX, worldY, 0);
    }

    // 原有的 Interact 方法保持不变
    public override void Interact()
    {
        if (!isInterable) return;

        Debug.Log($"与出口交互: {exitName}");

        // 检查当前模式
        if (ExplorationManager.IsInExploration())
        {
            HandleExplorationExit();
        }
        else
        {
            HandleBattleExit();
        }
    }

    private void HandleExplorationExit()
    {
        Debug.Log($"开始处理探索模式退出: {exitName}");

        if (showConfirmationDialogue && dialogueFile != null)
        {
            base.Interact();
            StartCoroutine(WaitForDialogueEnd());
        }
        else
        {
            EndExploration();
        }
    }

    private void HandleBattleExit()
    {
        Debug.Log($"在战斗中发现 {exitName}，直接退出战斗");

        if (TurnManager.instance != null)
        {
            TurnManager.instance.isWin = false;
        }

        if (PanelManager.instance != null)
        {
            PanelManager.instance.ShowEndPanel();
        }

        Debug.Log("战斗结束，返回地图");
        SceneManager.LoadScene("Map");
    }

    private IEnumerator WaitForDialogueEnd()
    {
        Debug.Log("等待对话结束...");

        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            yield return new WaitUntil(() => !dialogueSystem.isDialoguing);
            Debug.Log("对话结束，继续退出流程");
        }
        else
        {
            Debug.LogWarning("未找到对话系统，直接退出");
            yield return new WaitForSeconds(0.5f);
        }

        EndExploration();
    }

    private void EndExploration()
    {
        Debug.Log($"通过 {exitName} 离开探索区域");

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.EndExplorationAndReturn();
        }
        else
        {
            Debug.LogWarning("ExplorationManager 实例未找到，直接加载场景");
            SceneManager.LoadScene("Map");
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在 IsoGrid2D 的局部坐标系中生成一个独立的附加格子区域，
/// 起点以脚本物体的 transform 位置为准，逻辑坐标保留 startPos。
/// 不修改 IsoGrid2D 的 width / height，只是附加格子。
/// 同时支持玩家走到 triggerPositions 任意格子触发 parent 激活。
/// </summary>
public class GridAddition : MonoBehaviour
{
    [Header("附加区域参数")]
    public Vector2Int startPos = new Vector2Int(12, 0);  // 逻辑坐标起点
    public int areaWidth = 3;                            // 区域宽度
    public int areaHeight = 3;                           // 区域高度

    [Header("触发相关")]
    public List<Vector2Int> triggerPositions = new List<Vector2Int>(); // 玩家触发格子列表
    public GameObject targetParent;                                       // 触发后激活的父物体
    public bool generateOnStart = false;                                   // 是否在 Start 就生成额外格子

    private IsoGrid2D gridSystem;
    public bool triggered = false; // 防止重复触发

    void Start()
    {
        gridSystem = IsoGrid2D.instance;
        if (gridSystem == null)
        {
            Debug.LogError("未找到 IsoGrid2D 实例，请确保场景中存在 IsoGrid2D。");
            return;
        }

        if (targetParent != null)
            targetParent.SetActive(false); // 初始关闭

        if (generateOnStart)
            GenerateExtraArea();
    }

    void Update()
    {
        if (triggered) return;

        // 检查玩家当前位置
        if (gridSystem.currentPlayerGrid != null)
        {
            Vector2Int playerPos = gridSystem.currentPlayerGrid.gridPos;
            foreach (var pos in triggerPositions)
            {
                if (pos == playerPos)
                {
                    triggered = true;
                    ActivateTargetAndGenerate();
                    break;
                }
            }
        }
    }

    [ContextMenu("Generate Extra Area")]
    public void GenerateExtraArea()
    {
        if (gridSystem == null)
            gridSystem = IsoGrid2D.instance;

        List<GameObject> newTiles = new List<GameObject>();

        // 以 IsoGrid2D 的局部空间为基准
        Vector3 startLocalPos = gridSystem.transform.InverseTransformPoint(transform.position);

        float cellSize = gridSystem.cellSize;  // 与 IsoGrid2D 一致

        for (int y = 0; y < areaHeight; y++)
        {
            for (int x = 0; x < areaWidth; x++)
            {
                Vector2Int logicPos = new Vector2Int(startPos.x + x, startPos.y + y);

                // 使用相同的 cellSize 计算偏移
                Vector3 offset = gridSystem.GridToWorld(x, y, cellSize);
                Vector3 localOffset = offset - gridSystem.GridToWorld(0, 0, cellSize);

                GameObject tile = Instantiate(gridSystem.tilePrefab, gridSystem.transform);
                tile.transform.localPosition = startLocalPos + localOffset;
                tile.name = $"ExtraTile_{logicPos.x}_{logicPos.y}";

                GameGrid gridComp = tile.GetComponent<GameGrid>();
                gridComp.gridPos = logicPos;
                gridComp.isOccupied = false;

                GridNode node = new GridNode(gridComp, logicPos);
                IsoGrid2D.instance.extraNodes[logicPos] = node;

                gridSystem.grid.Add(tile);
                newTiles.Add(tile);
            }
        }
        IsoGrid2D.instance.controller.GetComponent<UnitController>().Move();

        Debug.Log($"已生成附加区域 {areaWidth}x{areaHeight}，逻辑起点：{startPos}，cellSize={cellSize}，共 {newTiles.Count} 格。");
    }

    /// <summary>
    /// 激活父物体并生成额外格子
    /// </summary>
    private void ActivateTargetAndGenerate()
    {
        if (targetParent != null)
            targetParent.SetActive(true);


        GenerateExtraArea();


        Debug.Log("触发玩家到达格子，父物体激活并生成额外格子");
    }
}

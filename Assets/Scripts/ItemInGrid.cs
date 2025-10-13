using System.Collections.Generic;
using UnityEngine;

public class ItemInGrid : MonoBehaviour
{
    [Tooltip("矩形占用区域的两个对角坐标")]
    public Vector2Int cornerA = Vector2Int.zero;
    public Vector2Int cornerB = Vector2Int.zero;

    [Tooltip("是否是单格物件，如果勾选，只占用cornerA")]
    public bool isSingleCell = false;

    private List<GameGrid> occupiedGrids = new List<GameGrid>();
    public bool isInterable = false; // 是否可交互
    public SpriteRenderer sr;

    void Start()
    {
        sr = transform.GetComponent<SpriteRenderer>();
        // 占用格子
        if (isSingleCell)
        {
            Occupy(cornerA, cornerA); // 单格物件只占cornerA
        }
        else
        {
            Occupy(cornerA, cornerB);
        }

        if (sr != null && occupiedGrids.Count > 0)
        {
            float sum = 0f;
            foreach (var grid in occupiedGrids)
            {
                sum += -(grid.gridPos.x + grid.gridPos.y); // 按照原来的规则取负
            }

            int average = Mathf.RoundToInt(sum / occupiedGrids.Count);
            sr.sortingOrder = average + 2; // +2 确保显示在格子上方
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sortingOrder = average + 3;
        }

        // 可交互只标记，不改变颜色
        if (isInterable)
        {
            foreach (var grid in occupiedGrids)
            {
                grid.isInterable = true;
            }
        }
    }

    public void Occupy(Vector2Int a, Vector2Int b)
    {
        ClearOccupied();

        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                GameObject tileObj = IsoGrid2D.instance.GetTile(x, y);
                if (tileObj != null)
                {
                    GameGrid gridComp = tileObj.GetComponent<GameGrid>();
                    gridComp.isOccupied = true;
                    occupiedGrids.Add(gridComp);
                }
                else
                {
                    Debug.LogWarning($"没找到 ({x},{y}) 对应的格子，跳过 {gameObject.name}");
                }
            }
        }
    }

    public void ClearOccupied()
    {
        foreach (var grid in occupiedGrids)
        {
            grid.isOccupied = false;
            grid.isInterable = false;  // 清除可交互标记
        }
        occupiedGrids.Clear();
    }

    private void OnDestroy()
    {
        ClearOccupied();
    }
}

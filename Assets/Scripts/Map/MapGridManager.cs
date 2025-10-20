using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轻量级等距格子生成器（基于 IsoGrid2D 的 GridToWorld 公式）
/// 支持按 Y 轴分组到空物体
/// </summary>
public class IsoGridSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Prefab")]
    public GameObject tilePrefab;

    [Header("Generated Tiles")]
    public List<GameObject> tiles = new List<GameObject>();

    // 存储每行空物体
    private Dictionary<int, GameObject> rowParents = new Dictionary<int, GameObject>();

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        ClearGrid();

        if (tilePrefab == null)
        {
            Debug.LogWarning("❌ 未设置 tilePrefab！");
            return;
        }

        rowParents.Clear();

        for (int y = 0; y < height; y++)
        {
            // 为每一行创建空物体
            GameObject rowParent = new GameObject($"Row_{y}");
            rowParent.transform.parent = transform;
            rowParents[y] = rowParent;

            for (int x = 0; x < width; x++)
            {
                Vector3 worldPos = GridToWorld(x, y, cellSize);
                GameObject tile = Instantiate(tilePrefab, rowParent.transform); // 父物体是行空物体
                tile.transform.localPosition = worldPos;
                tile.name = $"Tile_{x}_{y}";
                tiles.Add(tile);
            }
        }
    }

    private Vector3 GridToWorld(int x, int y, float cellSize)
    {
        float worldX = (x - y) * cellSize * 1f;
        float worldY = (x + y) * cellSize * 0.5f;
        return new Vector3(worldX, worldY, 0);
    }

    public void ClearGrid()
    {
        foreach (var tile in tiles)
        {
            if (tile != null) DestroyImmediate(tile);
        }
        tiles.Clear();

        // 删除每行空物体
        foreach (var row in rowParents.Values)
        {
            if (row != null) DestroyImmediate(row);
        }
        rowParents.Clear();
    }
}

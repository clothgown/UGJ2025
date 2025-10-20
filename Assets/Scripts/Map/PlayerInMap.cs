//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class PlayerInMap : MonoBehaviour
//{
//    private MapGridManager mapManager;
//    public MapGrid currentGrid;
//    private List<MapGrid> highlightedGrids = new List<MapGrid>();

//    void Start()
//    {
//        mapManager = FindObjectOfType<MapGridManager>();
//        if (mapManager == null)
//        {
//            Debug.LogError("未找到 MapGridManager！");
//            return;
//        }

//        currentGrid = GetComponentInParent<MapGrid>();
//        if (currentGrid != null)
//        {
//            currentGrid.SetVisited();
//            SetUpperGridsGray();
//            HighlightNeighbors();
//        }
//    }

//    void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

//            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
//            if (hit.collider != null)
//            {
//                MapGrid clickedGrid = hit.collider.GetComponent<MapGrid>();
//                if (clickedGrid != null)
//                {
//                    TryMoveTo(clickedGrid);
                    
//                }
//            }
//        }
//    }

//    private IEnumerator LoadSceneAfterDelay()
//    {
//        yield return new WaitForSeconds(1f); // 等待 delayTime 秒
//        SceneManager.LoadScene("Test");          // 加载场景
//    }

//    void HighlightNeighbors()
//    {
//        ClearHighlights();

//        Vector2Int[] directions = new Vector2Int[]
//        {
//            new Vector2Int(0, -1), // 下
//            new Vector2Int(-1, 0), // 左
//            new Vector2Int(1, 0),  // 右
//            new Vector2Int(0, 1)   // 上
//        };

//        //检查邻居中是否存在坐标 (2,0)
//        MapGrid priorityGrid = null;
//        foreach (var dir in directions)
//        {
//            Vector2Int neighborPos = currentGrid.gridPos + dir;
//            if (neighborPos == new Vector2Int(2, 0))
//            {
//                GameObject neighborObj = mapManager.GetGridObject(neighborPos);
//                if (neighborObj != null)
//                {
//                    priorityGrid = neighborObj.GetComponent<MapGrid>();
//                    break;
//                }
//            }
//        }

//        if (priorityGrid != null)
//        {
//            // 只有 (2,0) 高亮，其他邻居立即灰色
//            foreach (var dir in directions)
//            {
//                Vector2Int neighborPos = currentGrid.gridPos + dir;
//                GameObject neighborObj = mapManager.GetGridObject(neighborPos);
//                if (neighborObj == null) continue;

//                MapGrid grid = neighborObj.GetComponent<MapGrid>();
//                if (grid != null)
//                {
//                    if (grid == priorityGrid)
//                    {
//                        grid.SetHighlight(grid.gridColor);
//                        highlightedGrids.Add(grid);
//                    }
//                    else
//                    {
//                        grid.SetVisited();
//                    }
//                }
//            }
//            return;
//        }

//        foreach (var dir in directions)
//        {
//            Vector2Int neighborPos = currentGrid.gridPos + dir;
//            GameObject neighborObj = mapManager.GetGridObject(neighborPos);
//            if (neighborObj == null) continue;

//            MapGrid grid = neighborObj.GetComponent<MapGrid>();
//            if (grid != null)
//            {
//                // 如果格子已灰色，跳过高亮
//                if (grid.IsGray()) continue;

//                // 你的高亮逻辑
//                grid.SetHighlight(grid.gridColor);
//                highlightedGrids.Add(grid);
//            }
//        }

//    }

//    void ClearHighlights()
//    {
//        foreach (var grid in highlightedGrids)
//        {
//            // 已走过的格子保持灰色，不恢复
//            if (grid != null) continue;
//        }
//        highlightedGrids.Clear();
//    }

//    public void TryMoveTo(MapGrid targetGrid)
//    {
//        if (highlightedGrids.Contains(targetGrid))
//        {
//            transform.SetParent(targetGrid.transform);
//            transform.localPosition = Vector3.zero;

//            currentGrid = targetGrid;
//            currentGrid.SetVisited();
//            SetUpperGridsGray();
//            HighlightNeighbors();
//            ResetFarGrids(); // 保证远离玩家的格子恢复白色

//            if(targetGrid.type == 2)
//            {
//                SceneManager.LoadScene("Store");
//            }
//            if (targetGrid.type == 4)
//            {
//                SceneManager.LoadScene("RoomTest");
//            }
//        }
//    }

//    void SetUpperGridsGray()
//    {
//        foreach (var kvp in mapManager.gridDict)
//        {
//            MapGrid grid = kvp.Value.GetComponent<MapGrid>();
//            if (grid != null && grid.gridPos.y > currentGrid.gridPos.y)
//            {
//                grid.SetVisited();
//            }
//        }
//    }


//    void ResetFarGrids()
//    {
//        Vector2Int[] directions = new Vector2Int[]
//        {
//        new Vector2Int(0, -1), // 下
//        new Vector2Int(-1, 0), // 左
//        new Vector2Int(1, 0),  // 右
//        new Vector2Int(0, 1)   // 上
//        };

//        foreach (var kvp in mapManager.gridDict)
//        {
//            MapGrid grid = kvp.Value.GetComponent<MapGrid>();
//            if (grid == null) continue;

//            // 保留玩家当前格子及上下左右邻居
//            bool keepColor = grid == currentGrid;
//            foreach (var dir in directions)
//            {
//                if (grid.gridPos == currentGrid.gridPos + dir)
//                {
//                    keepColor = true;
//                    break;
//                }
//            }

//            if (!keepColor)
//            {
//                grid.ResetIfFarFromPlayer(); // 不在邻居 → 恢复白色
//            }
//        }
//    }

//}

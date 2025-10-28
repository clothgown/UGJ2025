using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static UnityEditor.PlayerSettings;

public class IsoGrid2D : MonoBehaviour
{
    public static IsoGrid2D instance;
    public int width = 10;          // 地图宽
    public int height = 10;         // 地图高
    public float cellSize = 1f;     // 格子大小
    public GameObject tilePrefab;   // 格子预制体（一个 Sprite，菱形格子）
    public GameGrid currentSelectedGrid = null;
    public GameObject controller;
    // 用一维列表存格子，Inspector 可见
    public List<GameObject> grid = new List<GameObject>();
    public GameGrid currentPlayerGrid = null;

    public bool isWaitingForGridClick = false;
    public Card waitingCard;

    public Dictionary<Vector2Int, GridNode> extraNodes = new Dictionary<Vector2Int, GridNode>();
    public GridState gridStateToChange = GridState.None;
    public bool isExploreScene;
    public string exploringSceneName;

    public bool isFortune;
    public bool isLocate;
    public GameObject column;
    public ItemCard locateCard;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateGrid();  // 提前生成
            
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private void Update()
    {
        

        

        if (FindAnyObjectByType<HorizontalCardHolder>()!=null)
        {
            if (controller.GetComponent<UnitController>().isNextAttackDouble)
            {
                FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToDouble();
            }
            else if (controller.GetComponent<UnitController>().isNextAttackMass)
            {
                FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToMass();
            }
            else
            {
                FindAnyObjectByType<HorizontalCardHolder>().ChangeAllCardToNormal();
            }
        }
        
    }
    void Start()
    {
        controller = FindAnyObjectByType<UnitController>().gameObject;
    }

    public GridNode[,] nodes;

    void GenerateGrid()
    {
        grid.Clear();
        nodes = new GridNode[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 worldPos = GridToWorld(x, y, cellSize);
                GameObject tile = Instantiate(tilePrefab, transform);
                tile.transform.localPosition = worldPos;
                tile.name = $"Tile_{x}_{y}";
                tile.GetComponent<GameGrid>().gridPos = new Vector2Int(x, y);
                grid.Add(tile);

                nodes[x, y] = new GridNode(tile.GetComponent<GameGrid>(), new Vector2Int(x, y));
            }
        }
    }


    // 逻辑坐标 (x,y) -> 世界坐标
    public Vector3 GridToWorld(int x, int y, float cellSize)
    {
        float worldX = (x - y) * cellSize * 1f;
        float worldY = (x + y) * cellSize * 0.5f;
        return new Vector3(worldX, worldY, 0);
    }

    // 根据 (x,y) 拿到格子 GameObject
    public GameObject GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            int index = y * width + x;
            return grid[index];
        }

        //查找扩展节点
        if (extraNodes.TryGetValue(new Vector2Int(x, y), out GridNode node))
            return node.grid.gameObject;

        return null;
    }




    public void HighlightMoveRange(Vector2Int playerPos, int moveRange)
    {
        ClearHighlight();

        // 检查当前模式
        bool isExplorationMode = ExplorationManager.IsInExploration();

        Queue<(Vector2Int pos, int step)> queue = new Queue<(Vector2Int, int)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue((playerPos, 0));
        visited.Add(playerPos);

        while (queue.Count > 0)
        {
            var (pos, step) = queue.Dequeue();
            GameObject tileObj = GetTile(pos.x, pos.y);
            if (tileObj == null) continue;

            GameGrid gridComp = tileObj.GetComponent<GameGrid>();

            // ✅ 根据模式使用不同颜色
            if (step > 0)
            {
                if (isExplorationMode)
                {
                    gridComp.SetColor(gridComp.explorecolor);
                }
                else
                {
                    gridComp.SetColor(gridComp.moveRangeColor);
                }
                gridComp.isInRange = true;
            }

            if (step >= moveRange) continue;

            // 四个方向扩展
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                Vector2Int newPos = pos + dir;
                if (visited.Contains(newPos)) continue;

                GameObject neighborObj = GetTile(newPos.x, newPos.y);
                if (neighborObj == null) continue;

                GameGrid neighbor = neighborObj.GetComponent<GameGrid>();
                if (neighbor.isOccupied) continue;

                queue.Enqueue((newPos, step + 1));
                visited.Add(newPos);
            }
        }

        // ✅ 重要：在战斗模式下也设置可交互物品
        SetInteractableItemsAroundPlayer(playerPos);
            }

    

    public void ResetWaiting()
    {
        isWaitingForGridClick = false;
        waitingCard = null;
        ClearHighlight();
    }

    public void ClearHighlight()
    {
        if (isWaitingForGridClick) return;
        foreach (var tile in grid)
        {
            GameGrid gridComp = tile.GetComponent<GameGrid>();
            gridComp.ResetColor();       // 恢复颜色
            gridComp.isInRange = false;
            gridComp.isAttackTarget = false;
            gridComp.canChangeState = false;
            gridComp.canHeal = false;
            gridComp.canDialogue = false;
        }

    }

    public List<GameGrid> FindPath(Vector2Int start, Vector2Int target)
    {
        // 1️获取 startNode 和 targetNode，支持 extraNodes
        GridNode startNode = GetNodeAt(start);
        GridNode targetNode = GetNodeAt(target);
        if (startNode == null || targetNode == null) return null;

        // 2️初始化 open 和 closed 列表
        List<GridNode> openList = new List<GridNode>();
        HashSet<GridNode> closedList = new HashSet<GridNode>();

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // 3️选取 fCost 最小的节点
            GridNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // 4️找到目标
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // 5️遍历邻居
            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (closedList.Contains(neighbor)) continue;

                // 如果格子被占用且不是目标，跳过
                if (neighbor.grid.isOccupied && neighbor != targetNode) continue;

                if (!neighbor.walkable) continue;

                int newGCost = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return null; // 没有路径
    }

    //辅助方法：根据坐标获取节点
    private GridNode GetNodeAt(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
            return nodes[pos.x, pos.y];

        if (extraNodes.TryGetValue(pos, out GridNode extraNode))
            return extraNode;

        return null; // 不存在
    }

    // 曼哈顿距离
    private int GetDistance(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.position.x - b.position.x) + Mathf.Abs(a.position.y - b.position.y);
    }

    // 回溯路径
    private List<GameGrid> RetracePath(GridNode startNode, GridNode endNode)
    {
        List<GameGrid> path = new List<GameGrid>();
        GridNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.grid);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    // 邻居获取（已支持 extraNodes）
    private List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int np = node.position + dir;

            GridNode neighbor = GetNodeAt(np);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }
        return neighbors;
    }




    public void HighlightSingleTile(Vector2Int pos)
    {
        ClearHighlight(); // 先清空已有高亮（可选，看你需不需要多格同时亮）

        GameObject tile = GetTile(pos.x, pos.y);
        if (tile != null)
        {
            GameGrid gridComp = tile.GetComponent<GameGrid>();
            gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 1f));
            
        }
    }
    public List<GameGrid> GetRangedAttackTiles(Vector2Int playerPos, int attackRange)
    {
        List<GameGrid> tilesInRange = new List<GameGrid>();

        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                Vector2Int targetPos = new Vector2Int(playerPos.x + dx, playerPos.y + dy);
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance == 0 || distance > attackRange) continue;

                GameObject tile = GetTile(targetPos.x, targetPos.y);
                if (tile != null)
                {
                    tilesInRange.Add(tile.GetComponent<GameGrid>());
                }
            }
        }

        return tilesInRange;
    }
    /// <summary>
    /// 高亮玩家攻击范围（敌人与空格子）
    /// </summary>
    /// <param name="playerPos">玩家格子坐标</param>
    /// <param name="attackRange">攻击范围</param>
    /// <returns>范围内是否有敌人</returns>
    public bool HighlightAttackArea(Vector2Int playerPos, int attackRange)
    {
        ClearHighlight();
        HighlightSingleTile(playerPos);
        bool hasEnemy = false;

        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy); // 曼哈顿距离
                if (distance == 0 || distance > attackRange) continue;

                Vector2Int targetPos = playerPos + new Vector2Int(dx, dy);
                GameObject tile = GetTile(targetPos.x, targetPos.y);

                if (tile != null)
                {
                    GameGrid gridComp = tile.GetComponent<GameGrid>();
                    EnemyUnit enemy = tile.GetComponentInChildren<EnemyUnit>();

                    if (enemy != null)
                    {
                        // 敌人 → 高亮不透明红色
                        gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 1f));
                        gridComp.isAttackTarget = true;
                        hasEnemy = true;
                    }
                    else
                    {
                        // 空格子 → 半透明红色
                        gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 0.3f));
                    }
                }
            }
        }

        

        return hasEnemy;
    }

    public void MarkEditableArea(Vector2Int centerPos, int range)
    {
        ClearHighlight();
        HighlightSingleTile(centerPos);

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy); // 曼哈顿距离
                if (distance == 0 || distance > range) continue;

                Vector2Int pos = centerPos + new Vector2Int(dx, dy);
                GameObject tile = GetTile(pos.x, pos.y);
                if (tile == null) continue;

                GameGrid gridComp = tile.GetComponent<GameGrid>();
                if (gridComp == null) continue;


                gridComp.isAttackTarget = true;
                gridComp.canChangeState = true;

                // （可选）稍微改个颜色区分范围
                gridComp.SetColor(new Color(0.5f, 0.8f, 1f, 0.3f));
            }
        }
    }


    /// <summary>
    /// 获取范围内可攻击的格子列表
    /// </summary>
    public List<GameGrid> GetAttackableTiles(Vector2Int playerPos, int attackRange)
    {
        List<GameGrid> tiles = new List<GameGrid>();

        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance == 0 || distance > attackRange) continue;

                Vector2Int targetPos = playerPos + new Vector2Int(dx, dy);
                GameObject tile = GetTile(targetPos.x, targetPos.y);
                if (tile != null)
                    tiles.Add(tile.GetComponent<GameGrid>());
            }
        }

        return tiles;
    }
    /// <summary>
    /// 高亮玩家上下左右直线范围（类似十字攻击）
    /// </summary>
    /// <param name="playerPos">玩家格子坐标</param>
    /// <param name="attackRange">直线范围</param>
    /// <returns>范围内是否有敌人</returns>
    public bool HighlightStraightAttackArea(Vector2Int playerPos, int attackRange)
    {
        ClearHighlight();
        HighlightSingleTile(playerPos); // 高亮玩家自己

        bool hasEnemy = false;

        // 四个方向
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            for (int step = 1; step <= attackRange; step++)
            {
                Vector2Int targetPos = playerPos + dir * step;
                GameObject tile = GetTile(targetPos.x, targetPos.y);

                if (tile == null) break; // 超出地图

                GameGrid gridComp = tile.GetComponent<GameGrid>();
                EnemyUnit enemy = tile.GetComponentInChildren<EnemyUnit>();

                if (enemy != null)
                {
                    // 敌人 → 不透明红色
                    gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 1f));
                    gridComp.isAttackTarget = true;
                    hasEnemy = true;
                }
                else
                {
                    // 空格子 → 半透明红色
                    gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 0.3f));
                }
            }
        }

        return hasEnemy;
    }
    private void SetInteractableItemsAroundPlayer(Vector2Int playerPos)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = playerPos + dir;
            GameObject neighborObj = GetTile(neighborPos.x, neighborPos.y);
            if (neighborObj == null) continue;

            GameGrid gridComp = neighborObj.GetComponent<GameGrid>();
            ItemInGrid item = gridComp.ocuupiedItem;

            if (item != null && item.isInterable)
            {
                // 设置格子为可交互状态
                gridComp.canDialogue = true;
                gridComp.isInterable = true;

                // 设置可交互格子的高亮
                if (item.isSingleCell)
                    item.SetCanInteract(item.cornerA, item.cornerA);
                else
                    item.SetCanInteract(item.cornerA, item.cornerB);

                Debug.Log($"设置可交互物品: {item.gameObject.name} 在格子 {neighborPos}");
            }
        }
    }

    public void CancelPendingCard()
    {
        if (waitingCard != null)
        {
            Debug.Log("取消出卡，卡牌返回手牌。");
            HorizontalCardHolder holder = FindObjectOfType<HorizontalCardHolder>();
            Card cancelledCard = waitingCard;
            ClearHighlight();

            // 动画返回到手牌位置
            cancelledCard.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);

            // 恢复为未打出的状态
            if (!holder.cards.Contains(cancelledCard))
                holder.cards.Add(cancelledCard);
        }
    }

    public void DealMassAttackDamage(float damage)
    {
        foreach (var tileObj in grid)
        {
            if (tileObj == null) continue;

            GameGrid gridComp = tileObj.GetComponent<GameGrid>();
            if (gridComp == null || !gridComp.isAttackTarget) continue;

            // 检查是否有敌人
            EnemyUnit enemy = tileObj.GetComponentInChildren<EnemyUnit>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        // 若存在扩展格子（extraNodes）
        foreach (var extra in extraNodes.Values)
        {
            GameGrid gridComp = extra.grid;
            if (gridComp == null || !gridComp.isAttackTarget) continue;

            EnemyUnit enemy = gridComp.GetComponentInChildren<EnemyUnit>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    public bool HighlightHealArea(Vector2Int playerPos, int range)
    {
        ClearHighlight();

        GameObject playerTile = GetTile(playerPos.x, playerPos.y);
        if (playerTile != null)
        {
            GameGrid gridComp = playerTile.GetComponent<GameGrid>();
            gridComp.SetColor(new Color(0.5f, 1f, 0.5f, 1f));
            gridComp.canHeal = true;

        }

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance == 0 || distance > range) continue;

                Vector2Int pos = playerPos + new Vector2Int(dx, dy);
                GameObject tile = GetTile(pos.x, pos.y);
                if (tile == null) continue;

                GameGrid gridComp = tile.GetComponent<GameGrid>();
                UnitController ally = tile.GetComponentInChildren<UnitController>();
                if (ally != null)
                {
                    gridComp.SetColor(new Color(0.5f, 1f, 0.5f, 1f)); // 绿色高亮
                    gridComp.canHeal = true;
                }

                else
                    gridComp.SetColor(new Color(0.5f, 1f, 0.5f, 0.3f)); // 半透明
            }
        }
        return true;
    }

    public void DealMassHeal(float healAmount)
    {
        foreach (var tileObj in grid)
        {
            if (tileObj == null) continue;

            GameGrid gridComp = tileObj.GetComponent<GameGrid>();
            if (gridComp == null || !gridComp.isAttackTarget) continue;

            // 检查格子上是否有可治疗的玩家单位
            if (gridComp.occupiedPlayer != null)
            {
                UnitController unit = gridComp.occupiedPlayer;
                unit.Heal(healAmount);
            }
        }

        // 若存在扩展格子（extraNodes）
        foreach (var extra in extraNodes.Values)
        {
            GameGrid gridComp = extra.grid;
            if (gridComp == null || !gridComp.isAttackTarget) continue;

            if (gridComp.occupiedPlayer != null)
            {
                UnitController unit = gridComp.occupiedPlayer;
                unit.Heal(healAmount);
            }
        }

        // 出卡后抽卡 + 重置等待状态
        FindAnyObjectByType<HorizontalCardHolder>().DrawCardAndUpdate();
        IsoGrid2D.instance.ResetWaiting();

        Debug.Log($"Mass Heal：为范围内所有角色回复 {healAmount} 点生命值！");
    }

    /// <summary>
    /// 高亮指定范围内的所有单位（玩家与敌人）
    /// </summary>
    /// <param name="centerPos">中心格坐标</param>
    /// <param name="range">范围半径（曼哈顿距离）</param>
    /// <returns>范围内是否存在任何单位</returns>
    public bool HighlightArea(Vector2Int centerPos, int range)
    {
        ClearHighlight(); // 清除旧高亮
        bool hasAnyUnit = false;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance > range) continue; // 超出范围

                Vector2Int pos = centerPos + new Vector2Int(dx, dy);
                GameObject tile = GetTile(pos.x, pos.y);
                if (tile == null) continue;

                GameGrid gridComp = tile.GetComponent<GameGrid>();
                if (gridComp == null) continue;

                // 默认半透明蓝色高亮（空格）
                Color highlightColor = new Color(0.5f, 0.8f, 1f, 0.3f);

                // 检查单位
                UnitController player = tile.GetComponentInChildren<UnitController>();
                EnemyUnit enemy = tile.GetComponentInChildren<EnemyUnit>();

                if (player != null && player != controller.GetComponent<UnitController>())
                {
                    highlightColor = new Color(0.5f, 1f, 0.5f, 1f); // 玩家 → 绿色
                    hasAnyUnit = true;
                }
                else if (enemy != null)
                {
                    highlightColor = new Color(1f, 0.5f, 0.5f, 1f); // 敌人 → 红色
                    hasAnyUnit = true;
                }

                gridComp.SetColor(highlightColor);
                gridComp.isAttackTarget = true;
            }
        }

        // 中心格子特殊高亮（黄色）
        GameObject centerTile = GetTile(centerPos.x, centerPos.y);
        if (centerTile != null)
        {
            GameGrid gridComp = centerTile.GetComponent<GameGrid>();
            gridComp.SetColor(new Color(1f, 1f, 0.4f, 1f));
        }

        return hasAnyUnit;
    }

    /// <summary>
    /// 交换两个单位（玩家或敌人）的位置与父子关系
    /// </summary>
    public void SwapUnitPositions(MonoBehaviour unitA, MonoBehaviour unitB)
    {
        if (unitA == null || unitB == null)
        {
            Debug.LogWarning("SwapUnitPositions：传入的单位为空！");
            return;
        }

        GameObject goA = unitA.gameObject;
        GameObject goB = unitB.gameObject;

        // 获取两个单位所在的格子（父物体）
        GameGrid gridA = goA.GetComponentInParent<GameGrid>();
        GameGrid gridB = goB.GetComponentInParent<GameGrid>();

        if (gridA == null || gridB == null)
        {
            Debug.LogWarning("SwapUnitPositions：无法找到单位所在的格子！");
            return;
        }

        // 保存原始位置
        Vector3 worldPosA = goA.transform.position;
        Vector3 worldPosB = goB.transform.position;

        // 清除旧格子占用状态
        if (unitA is UnitController)
            gridA.occupiedPlayer = null;
        if (unitB is UnitController)
            gridB.occupiedPlayer = null;
        if (unitA is EnemyUnit)
            gridA.isOccupied = false;
        if (unitB is EnemyUnit)
            gridB.isOccupied = false;

        // 交换父对象
        goA.transform.SetParent(gridB.transform);
        goB.transform.SetParent(gridA.transform);

        // 重新绑定新格子引用
        if (unitA is UnitController playerA)
        {
            gridB.occupiedPlayer = null;
            gridB.currentEnemy = null;
            gridB.occupiedPlayer = playerA;
            playerA.startGrid = gridB.gameObject;
            playerA.startPoint = gridB.gridPos;
            playerA.currentGrid = gridB;
            playerA.currentGridPos = gridB.gridPos;
            gridB.isOccupied = true;
            currentPlayerGrid = gridB;
        }
        else if (unitA is EnemyUnit enemyA)
        {
            gridB.occupiedPlayer = null;
            gridB.currentEnemy = null;
            gridB.currentEnemy = enemyA;
            gridB.isOccupied = true;
            enemyA.startPoint = gridB.gridPos;
            enemyA.startGrid = gridB.gameObject;

        }

        if (unitB is UnitController playerB)
        {
            gridA.occupiedPlayer = null;
            gridA.currentEnemy = null;
            gridA.occupiedPlayer = playerB;
            playerB.currentGrid = gridA;
            playerB.startGrid = gridA.gameObject;
            playerB.startPoint = gridA.gridPos;
            playerB.currentGridPos = gridA.gridPos;
            gridA.isOccupied = true;
            currentPlayerGrid = gridA;
        }
        else if (unitB is EnemyUnit enemyB)
        {
            gridA.occupiedPlayer = null;
            gridA.currentEnemy = null;
            gridA.currentEnemy = enemyB;
            gridA.isOccupied = true;
            enemyB.startPoint = gridA.gridPos;
            enemyB.startGrid = gridA.gameObject;
        }

        // 平滑移动动画（使用世界坐标）
        float moveDuration = 0.4f;
        goA.transform.DOMove(worldPosB, moveDuration).SetEase(Ease.OutQuad);
        goB.transform.DOMove(worldPosA, moveDuration).SetEase(Ease.OutQuad);

        Debug.Log($"SwapUnitPositions：{unitA.name} <-> {unitB.name} 交换完成");
    }

    /// <summary>
    /// 获取指定范围内的所有格子，并高亮单位或空格
    /// </summary>
    /// <param name="centerPos">中心格坐标</param>
    /// <param name="range">范围半径（曼哈顿距离）</param>
    /// <returns>范围内的所有 GameObject（格子）</returns>
    public List<GameObject> GetAndHighlightUnitsInRange(Vector2Int centerPos, int range)
    {
        List<GameObject> result = new List<GameObject>();
        ClearHighlight(); // 清除之前的高亮

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance > range) continue;

                Vector2Int pos = centerPos + new Vector2Int(dx, dy);
                GameObject tileObj = GetTile(pos.x, pos.y);
                if (tileObj == null) continue;

                GameGrid gridComp = tileObj.GetComponent<GameGrid>();
                if (gridComp == null) continue;

                // 检查单位
                UnitController player = tileObj.GetComponentInChildren<UnitController>();
                EnemyUnit enemy = tileObj.GetComponentInChildren<EnemyUnit>();

                // 只对有单位的格子进行高亮并加入返回列表
                if ((player != null && player != controller.GetComponent<UnitController>()) || enemy != null)
                {
                    // 深红色高亮
                    gridComp.SetColor(new Color(1f, 0.2f, 0.2f, 1f));
                    gridComp.isAttackTarget = true;

                    result.Add(tileObj);
                }
                else
                {
                    // 空格子仍然可以高亮浅红色（可选）
                    gridComp.SetColor(new Color(1f, 0.5f, 0.5f, 0.3f));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 高亮指定范围内的格子，根据是否被占用显示深色或浅色
    /// </summary>
    /// <param name="centerPos">中心格子坐标</param>
    /// <param name="range">曼哈顿范围</param>
    public void HighlightEditableTiles(Vector2Int centerPos, int range)
    {
        ClearHighlight(); // 先清空已有高亮

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance > range) continue;

                Vector2Int pos = centerPos + new Vector2Int(dx, dy);
                GameObject tileObj = GetTile(pos.x, pos.y);
                if (tileObj == null) continue;

                GameGrid gridComp = tileObj.GetComponent<GameGrid>();
                if (gridComp == null) continue;

                if (!gridComp.isOccupied)
                {
                    gridComp.isAttackTarget = true;
                    // 空格 → 深色高亮
                    gridComp.SetColor(new Color(0.3f, 0.6f, 1f, 1f));
                }
                else
                {
                    // 已占用 → 浅色高亮
                    gridComp.SetColor(new Color(0.6f, 0.8f, 1f, 0.5f));
                }
            }
        }

        // 中心格子单独标记为黄色（可选）
        GameObject centerTile = GetTile(centerPos.x, centerPos.y);
        if (centerTile != null)
        {
            GameGrid centerGrid = centerTile.GetComponent<GameGrid>();
            centerGrid.SetColor(new Color(1f, 1f, 0.4f, 1f));
        }
    }

}

using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum EnemyType
{
    Normal,       // 原本行为
    Passive,      // 第一次被攻击前不行动，攻击半径4，优先血量最低玩家
}

public class EnemyUnit : MonoBehaviour
{
    [Header("敌人类型")]
    public EnemyType enemyType = EnemyType.Normal;

    public Vector2Int startPoint;
    public GameObject startGrid;
    public int moveRange = 3;
    public float moveSpeed = 2f;

    public UnitController targetPlayer;  // 追击的目标玩家
    public HealthSystem healthSystem;
    public float maxHealth;
    public float currentHealth;

    public float attackDamage = 2f;
    public bool isDizziness;

    [Header("Passive Specific")]
    [SerializeField] private bool hasBeenAttacked = false; // Passive敌人是否已被攻击过

    public SpriteRenderer sr;
    [Header("Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Passive Buff Settings")]
    private bool hasTriggeredLifeAbsorb = false; // 是否已触发吸血事件
    private bool damageBoostActive = false;      // 是否正在进行伤害加成
    private void Start()
    {
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        // 初始化敌人的位置
        if (IsoGrid2D.instance.GetTile(startPoint.x, startPoint.y) != null)
        {
            startGrid = IsoGrid2D.instance.GetTile(startPoint.x, startPoint.y);
            var gridComp = startGrid.GetComponent<GameGrid>();

            gridComp.isOccupied = true;
            gridComp.currentEnemy = this;

            transform.SetParent(startGrid.transform);
            transform.localPosition = Vector3.zero;

            //同步敌人 SpriteRenderer 层级
            if (sr != null)
            {
                int sortingOrder = startPoint.x + startPoint.y;
                sr.sortingOrder = -sortingOrder + 2; // +2 确保比格子高
            }
        }

        currentHealth = maxHealth;
        healthSystem.SetMaxHealth(maxHealth);

        // Passive敌人初始不行动
        if (enemyType == EnemyType.Passive)
        {
            hasBeenAttacked = false;
        }
    }

    private void Update()
    {
        // 只有在敌人回合才执行追击逻辑
        if (TurnManager.instance == null || TurnManager.instance.phase != TurnPhase.EnemyTurn)
            return;
    }

    /// <summary>
    /// 选择最近的玩家作为目标 (Normal) 或 优先血量最低的玩家 (Passive)
    /// </summary>
    private void ChooseNearestPlayer()
    {
        UnitController[] players = FindObjectsOfType<UnitController>();
        if (players == null || players.Length == 0) return;

        UnitController selected = null;
        int shortestPath = int.MaxValue;

        if (enemyType == EnemyType.Normal)
        {
            // Normal: 选择最近的玩家
            foreach (var p in players)
            {
                Debug.Log(p);
                List<GameGrid> path = IsoGrid2D.instance.FindPath(startPoint, p.currentGridPos);
                if (path != null && path.Count < shortestPath)
                {
                    shortestPath = path.Count;
                    selected = p;
                    Debug.Log(selected);
                }
            }
        }
        else if (enemyType == EnemyType.Passive)
        {
            // Passive: 优先选择血量最低的玩家（如果多个，选最近的）
            UnitController lowestHealthPlayer = null;
            float lowestHealth = float.MaxValue;

            foreach (var p in players)
            {
                if (p.currentHealth < lowestHealth)
                {
                    lowestHealth = p.currentHealth;
                    lowestHealthPlayer = p;
                }
            }

            if (lowestHealthPlayer != null)
            {
                selected = lowestHealthPlayer;
                shortestPath = IsoGrid2D.instance.FindPath(startPoint, selected.currentGridPos)?.Count ?? int.MaxValue;
            }
        }

        targetPlayer = selected;
    }

    /// <summary>
    /// 敌人追踪玩家
    /// </summary>
    public void ChasePlayer()
    {
        // Passive: 第一次被攻击前不行动
        if (enemyType == EnemyType.Passive && !hasBeenAttacked)
        {
            Debug.Log("Passive enemy is not active yet.");
            return;
        }

        // 选择目标玩家
        ChooseNearestPlayer();
        if (targetPlayer == null) return;

        Vector2Int playerPos = targetPlayer.currentGridPos;
        int dist = Mathf.Abs(playerPos.x - startPoint.x) + Mathf.Abs(playerPos.y - startPoint.y);
        int attackRadius = (enemyType == EnemyType.Passive) ? 4 : 1;

        // ---------------- Normal 敌人逻辑 ----------------
        if (enemyType == EnemyType.Normal)
        {
            // 在攻击范围内直接攻击
            if (dist <= attackRadius)
            {
                AttackPlayer();
                return;
            }

            // 不在攻击范围则尝试移动
            List<GameGrid> path = IsoGrid2D.instance.FindPath(startPoint, playerPos);
            if (path == null || path.Count == 0)
            {
                Debug.Log("Normal enemy: No valid path to player.");
                return;
            }

            int steps = Mathf.Min(moveRange, path.Count - 1);
            List<GameGrid> limitedPath = path.GetRange(0, steps);

            StopAllCoroutines();
            StartCoroutine(FollowPath(limitedPath));
            return;
        }

        // ---------------- Passive 敌人逻辑 ----------------
        if (enemyType == EnemyType.Passive)
        {
            // 1️⃣ 目标在攻击范围内，直接攻击
            
            if (dist <= attackRadius)
            {
                Debug.Log("Passive enemy: target in attack range, attack directly!");
                AttackPlayer();
                return;
            }

            // 2️⃣ 尝试移动到可攻击目标的格子
            List<GameGrid> path = IsoGrid2D.instance.FindPath(startPoint, playerPos);
            List<GameGrid> movePath = new List<GameGrid>();

            if (path != null && path.Count > 0)
            {
                // 目标：停在距离玩家 attackRadius 的格子
                int stopIndex = Mathf.Max(0, path.Count - attackRadius - 1);
                // 限制移动距离不超过 moveRange
                stopIndex = Mathf.Min(stopIndex, moveRange);
                movePath = path.GetRange(0, Mathf.Min(path.Count, stopIndex + 1));
            }
            else
            {
                // 3️⃣ 路径不存在（被包围） → 检查当前位置是否可攻击
                List<GameGrid> attackableTiles = IsoGrid2D.instance.GetAttackableTiles(startPoint, attackRadius);
                bool canAttackFromCurrent = attackableTiles.Exists(tile => tile.gridPos == playerPos);

                if (canAttackFromCurrent)
                {
                    Debug.Log("Passive enemy: can attack from current position, attack directly!");
                    AttackPlayer();
                    return;
                }

                // 4️⃣ 无法攻击 → 尝试移动到靠近玩家的格子
                List<GameGrid> possibleTiles = IsoGrid2D.instance.GetAttackableTiles(playerPos, attackRadius);
                GameGrid closestTile = null;
                int minDistance = int.MaxValue;

                foreach (var tile in possibleTiles)
                {
                    if (tile.isOccupied) continue; // 跳过被占用的格子
                    int distToTile = Mathf.Abs(tile.gridPos.x - startPoint.x) + Mathf.Abs(tile.gridPos.y - startPoint.y);
                    if (distToTile <= moveRange && distToTile < minDistance)
                    {
                        minDistance = distToTile;
                        closestTile = tile;
                    }
                }

                if (closestTile != null)
                {
                    // 找到最近的可移动格子，计算路径
                    path = IsoGrid2D.instance.FindPath(startPoint, closestTile.gridPos);
                    if (path != null && path.Count > 0)
                    {
                        movePath = path.GetRange(0, Mathf.Min(path.Count, moveRange));
                    }
                }
            }

            // 5️⃣ 执行移动并在最后攻击
            StopAllCoroutines();
            if (movePath.Count > 0)
            {
                StartCoroutine(FollowPathThenAttack(movePath));
            }
            else
            {
                Debug.Log("Passive enemy: no valid path or move needed, staying in place.");
                // 如果没有路径，可能被完全包围，保持不动
            }
        }


    }


    public void Move()
    {
        
        IsoGrid2D.instance.HighlightMoveRange(startPoint, moveRange);
    }

    public void MoveToGrid(GameGrid targetGrid)
    {
        string[] nameParts = targetGrid.gameObject.name.Split('_');
        Vector2Int targetPos = new Vector2Int(int.Parse(nameParts[1]), int.Parse(nameParts[2]));

        List<GameGrid> path = IsoGrid2D.instance.FindPath(startPoint, targetPos);
        if (path != null)
        {
            StopAllCoroutines();
            StartCoroutine(FollowPath(path));
            IsoGrid2D.instance.ClearHighlight();
        }
    }

    private IEnumerator FollowPath(List<GameGrid> path)
    {
        CinemachineVirtualCamera virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>();
        if (virtualCamera != null)
            virtualCamera.Follow = this.transform;
        foreach (var grid in path)
        {
            Vector2Int prevPos = startPoint;
            string[] nameParts = grid.name.Split('_');
            Vector2Int nextPos = new Vector2Int(int.Parse(nameParts[1]), int.Parse(nameParts[2]));
            UpdateDirectionSprite(prevPos, nextPos); // 关键行
            // ---- 把目标格子先标记为占用，防止冲突 ----
            grid.isOccupied = true;
            grid.currentEnemy = this;

            Vector3 targetPos = grid.transform.position;

            while ((transform.position - targetPos).sqrMagnitude > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;

            // ---- 释放旧的格子 ----
            if (startGrid != null)
            {
                GameGrid oldGrid = startGrid.GetComponent<GameGrid>();
                oldGrid.isOccupied = false;
                oldGrid.currentEnemy = null;
            }

            // ---- 占用新的格子 ----
            startGrid = grid.gameObject;

            int x = int.Parse(nameParts[1]);
            int y = int.Parse(nameParts[2]);
            startPoint = new Vector2Int(x, y);

            transform.SetParent(grid.transform);
            transform.localPosition = Vector3.zero;
            if (sr != null)
                sr.sortingOrder = grid.GetComponent<GameGrid>().sortingOrder * -1 + 2;
        }

        // 攻击判定
        if (targetPlayer != null)
        {
            Vector2Int playerPos = targetPlayer.currentGridPos;
            int dist = Mathf.Abs(playerPos.x - startPoint.x) + Mathf.Abs(playerPos.y - startPoint.y);
            // Passive: 攻击半径为4
            int attackRadius = (enemyType == EnemyType.Passive) ? 4 : 1;
            if (dist <= attackRadius)
            {
                AttackPlayer();
            }
        }
    }

    private void AttackPlayer()
    {
        if (targetPlayer == null || currentHealth <= 0) return;

        // 面向玩家
        UpdateDirectionSprite(startPoint, targetPlayer.currentGridPos);

        // 攻击动画参数
        float dashDistance = 0.3f; // 冲刺距离
        float dashDuration = 0.15f; // 冲刺时间
        float returnDuration = 0.1f; // 回位时间

        // 计算攻击方向
        Vector3 dir = (targetPlayer.transform.position - transform.position).normalized;

        // 动画执行：先前冲 → 回来 → 造成伤害
        Sequence attackSeq = DOTween.Sequence();

        // 冲刺（攻击前摇）
        attackSeq.Append(transform.DOMove(transform.position + dir * dashDistance, dashDuration).SetEase(Ease.OutQuad));

        // 回位
        attackSeq.Append(transform.DOMove(transform.position, returnDuration).SetEase(Ease.InQuad));

        // 攻击结算（在动画结束后执行伤害）
        attackSeq.OnComplete(() =>
        {
            targetPlayer.TakeDamage(attackDamage);
            Debug.Log("Enemy attacks player!");
        });
    }


    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        FindObjectOfType<CameraShake>().Shake();
        healthSystem.SetHealth(currentHealth);

        if(GetComponent<BattleDialogue>()!=null)
        {
            GetComponent<BattleDialogue>().CheckTrigger();
        }



        // Passive: 第一次被攻击后激活行动
        if (enemyType == EnemyType.Passive && !hasBeenAttacked)
        {
            hasBeenAttacked = true;
            Debug.Log("Passive enemy is now active!");
        }

        // ✅ Passive: 血量第一次低于30%时触发吸血事件
        if (enemyType == EnemyType.Passive && !hasTriggeredLifeAbsorb && currentHealth / maxHealth <= 0.3f)
        {
            hasTriggeredLifeAbsorb = true;
            sr.color = Color.red;
            Debug.Log("Passive enemy triggers life absorption!");

            // 吸收所有我方在场角色生命值
            UnitController[] players = FindObjectsOfType<UnitController>();
            float totalAbsorbed = 0f;
            foreach (var p in players)
            {
                float absorbAmount = Mathf.Min(5f, p.currentHealth - 1f); // 最多吸5，保留1点血
                if (absorbAmount > 0)
                {
                    p.TakeDamage(absorbAmount);
                    totalAbsorbed += absorbAmount;
                }
            }

            // 敌人回满当前吸收量的血
            currentHealth += totalAbsorbed;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            healthSystem.SetHealth(currentHealth);

            // 下一回合开始后，所有造成的伤害 ×1.5
            damageBoostActive = true;
            attackDamage *= 1.5f;
        }

        if (IsoGrid2D.instance.controller.GetComponent<UnitController>().isNextAttackBloodSucking == true)
        {
            IsoGrid2D.instance.controller.GetComponent<UnitController>().Heal(amount);
            IsoGrid2D.instance.controller.GetComponent<UnitController>().RecoverState();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"{name} is dead!");
            Die();
        }
    }


    private void Die()
    {
        if (startGrid != null)
        {
            GameGrid grid = startGrid.GetComponent<GameGrid>();
            grid.isOccupied = false;
            grid.currentEnemy = null;
        }
        Destroy(gameObject);
    }

    public void Dizziness()
    {
        isDizziness = true;
        Color c = Color.blue;
        sr.color = c;
    }

    public void Recover()
    {
        isDizziness = false;
        Color c = Color.white;
        sr.color = c;
    }

    /// <summary>
    /// 敌人被拉向玩家（带快速移动效果）
    /// </summary>
    /// <param name="playerPos">玩家所在格子</param>
    /// <param name="pullRange">最多拉多少格</param>
    public void BePulled(Vector2Int playerPos, int pullRange)
    {
        // 计算方向（单位向量）
        Vector2Int dir = playerPos - startPoint;

        // 如果敌人和玩家在对角线方向（比如 ↖ ↘），直接return，不拉
        if (Mathf.Abs(dir.x) > 0 && Mathf.Abs(dir.y) > 0) return;
        if (dir == Vector2Int.zero) return; // 已经和玩家重叠

        dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));

        // 玩家前一格 = 玩家位置 - 拉的方向
        Vector2Int maxPullPos = playerPos - dir;

        // 实际能拉的目标位置
        Vector2Int targetPos = startPoint;

        for (int step = 1; step <= pullRange; step++)
        {
            Vector2Int nextPos = startPoint + dir * step;

            // 超出地图
            if (nextPos.x < 0 || nextPos.x >= IsoGrid2D.instance.width ||
                nextPos.y < 0 || nextPos.y >= IsoGrid2D.instance.height)
                break;

            // 不能超过玩家前一格
            if (nextPos == playerPos) break;
            if (nextPos == maxPullPos + dir) break;

            GameGrid nextGrid = IsoGrid2D.instance.GetTile(nextPos.x, nextPos.y).GetComponent<GameGrid>();

            if (nextGrid.isOccupied) break; // 前方被挡住就停下

            targetPos = nextPos;
        }

        if (targetPos == startPoint) return; // 没有移动

        StopAllCoroutines();
        StartCoroutine(PullToPosition(targetPos));
    }

    /// <summary>
    /// 协程：平滑拉动敌人
    /// </summary>
    private IEnumerator PullToPosition(Vector2Int targetPos)
    {
        // ---- 释放旧格子 ----
        if (startGrid != null)
        {
            GameGrid oldGrid = startGrid.GetComponent<GameGrid>();
            oldGrid.isOccupied = false;
            oldGrid.currentEnemy = null;
        }

        GameGrid newGrid = IsoGrid2D.instance.GetTile(targetPos.x, targetPos.y).GetComponent<GameGrid>();
        newGrid.isOccupied = true;
        newGrid.currentEnemy = this;

        startPoint = targetPos;
        startGrid = newGrid.gameObject;

        Vector3 targetWorldPos = newGrid.transform.position;
        float speed = moveSpeed * 2f; // 拉扯时快一点，你可以调

        // 平滑移动
        while ((transform.position - targetWorldPos).sqrMagnitude > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetWorldPos;

        // 挂在新格子下
        transform.SetParent(newGrid.transform);
        transform.localPosition = Vector3.zero;

        Debug.Log($"{name} 被拉到 {targetPos}");
    }

    private void UpdateDirectionSprite(Vector2Int from, Vector2Int to)
    {
        Vector2Int dir = to - from;

        if (dir.y < 0) // 向前（地图上y减小）
        {
            sr.sprite = frontSprite;
            sr.flipX = true;
        }
        else if (dir.y > 0) // 向后（地图上y增大）
        {
            sr.sprite = backSprite;
            sr.flipX = false;
        }
        else if (dir.x > 0) // 向右
        {
            sr.sprite = backSprite;
            sr.flipX = true;
        }
        else if (dir.x < 0) // 向左
        {
            sr.sprite = frontSprite;
            sr.flipX = false;
        }
    }

    private IEnumerator FollowPathThenAttack(List<GameGrid> path)
    {
        yield return StartCoroutine(FollowPath(path));

        // 到达后判断是否能攻击
        if (targetPlayer != null)
        {
            Vector2Int playerPos = targetPlayer.currentGridPos;
            int dist = Mathf.Abs(playerPos.x - startPoint.x) + Mathf.Abs(playerPos.y - startPoint.y);
            int attackRadius = (enemyType == EnemyType.Passive) ? 4 : 1;

            if (dist <= attackRadius)
            {
                AttackPlayer();
            }
        }
    }

    /// <summary>
    /// 敌人移动到指定格子（可选攻击到玩家）
    /// </summary>
    /// <param name="targetGrid">目标格子</param>
    /// <param name="onComplete">移动完成回调</param>
    public void MoveToTargetGrid(GameGrid targetGrid, System.Action onComplete = null)
    {
        if (targetGrid == null) return;

        // 1️⃣ 寻路
        List<GameGrid> path = IsoGrid2D.instance.FindPath(startPoint, targetGrid.gridPos);
        if (path == null || path.Count == 0)
        {
            Debug.Log("MoveToTargetGrid: 找不到路径");
            return;
        }

        // 2️⃣ 启动协程移动
        StopAllCoroutines();
        StartCoroutine(FollowPathWithCallback(path, onComplete));
    }

    /// <summary>
    /// 协程：沿路径移动敌人，每格更新占用和Sprite朝向
    /// </summary>
    private IEnumerator FollowPathWithCallback(List<GameGrid> path, System.Action onComplete)
    {
        foreach (var grid in path)
        {
            Vector2Int prevPos = startPoint;
            Vector2Int nextPos = grid.gridPos;

            UpdateDirectionSprite(prevPos, nextPos);

            // ---- 占用新格子 ----
            grid.isOccupied = true;
            grid.currentEnemy = this;

            // ---- 移动世界坐标 ----
            Vector3 targetWorldPos = grid.transform.position;
            while ((transform.position - targetWorldPos).sqrMagnitude > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetWorldPos;

            // ---- 释放旧格子 ----
            if (startGrid != null)
            {
                GameGrid oldGrid = startGrid.GetComponent<GameGrid>();
                oldGrid.isOccupied = false;
                oldGrid.currentEnemy = null;
            }

            // ---- 更新状态 ----
            startGrid = grid.gameObject;
            startPoint = nextPos;

            transform.SetParent(grid.transform);
            transform.localPosition = Vector3.zero;
            if (sr != null)
                sr.sortingOrder = grid.sortingOrder * -1 + 2;
        }

        onComplete?.Invoke();
    }

}
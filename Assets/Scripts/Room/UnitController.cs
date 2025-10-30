using DG.Tweening;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.VFX;

public class UnitController : MonoBehaviour
{
    public Vector2Int startPoint;
    public GameObject startGrid;
    public int moveRange = 3;
    public float moveSpeed = 2f;

    public HealthSystem healthSystem;
    public float maxHealth;
    public float currentHealth;

    [Header("Combat")]

    public int attackRange = 1;    // ������Χ���������� 1 ��
    public float attackDamage = 5f;
    public float meleeMultiplier = 1f;   

    public float rangedMultiplier = 1f;  

    public float dodgeChance = 0f;       

    public Vector2Int currentGridPos; // ��ҵ�ǰ����λ��

    public int maxActionPoints = 3;   // 每回合初始行动点
    public int actionPoints;          // 当前行动点

    public float shield = 0f;      // ����ֵ

    public bool isMoving = false;

    public SpriteRenderer sr;

    public bool isActive = false;

    public bool isNextAttackDizziness = false;
    public bool isNextAttackMultiple = false;
    public int SegmentCount = 0;

    public bool isNextAttackBloodSucking = false;

    public bool isNextAttackFire = false;

    public bool isNextAttackIce = false;
    public bool isNextAttackPull = false;
    public bool isNextAttackDouble = false;
    public bool isNextAttackMass = false;
    public int PullDistance = 0;

    public float healPoint = 0;
    [Header("Sprites")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("VFX")]
    public VisualEffect MoveVFX;
    public VisualEffect RunOutActionPoint;
    
    public VisualEffect XN;
    public VisualEffect Attacked;
    public VisualEffect AttackedByArrow;
    public int attackway;
    public VisualEffect Attack1;
    public VisualEffect Cure;
    public VisualEffect sheild;

    [Header("死亡效果")]
    public Color deadColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    public int attackType = -1;
    public CardData.AttackAttribute attackAttribute;

    [Header("关键角色设置")]
    public bool isCriticalCharacter = false; // 如果这个角色死亡，游戏直接结束
    public string characterName; // 角色名称（用于显示）

    public GameGrid currentGrid;

    public bool isNextAttackChange;
    public GameObject changeTarget;
    public VisualEffect AttackVFXPrefab;

    public enum Who
    {
        Heart,
        Female,
        Insert,
        Male,
    }
    public Who who;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // 订阅死亡事件

            currentHealth = maxHealth;
            healthSystem.SetMaxHealth(maxHealth);
            healthSystem.SetMaxShield(10f);
            healthSystem.SetShield(shield);
            //PlayerSwitchManager.instance.currentUnitController = this;
        }
    }
    private void Start()
    {
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        currentGridPos = startPoint;
        if (IsoGrid2D.instance.GetTile(startPoint.x, startPoint.y) != null)
        {
            startGrid = IsoGrid2D.instance.GetTile(startPoint.x, startPoint.y);
            var gridComp = startGrid.GetComponent<GameGrid>();

            gridComp.isOccupied = true;
            gridComp.occupiedPlayer = this;

            transform.SetParent(startGrid.transform);
            transform.localPosition = Vector3.zero;

            //同步角色排序层级
            if (sr != null)
            {
                int sortingOrder = startPoint.x+ startPoint.y;
                sr.sortingOrder = -sortingOrder + 2; // +2 确保比格子高
                if(RunOutActionPoint!=null)
                {
                    RunOutActionPoint.GetComponent<Renderer>().sortingOrder = sr.sortingOrder;
                }
            }
            IsoGrid2D.instance.currentPlayerGrid = gridComp;
        }
        
        // ✅ 从 AllPlayerState 恢复血量
        AllPlayerState aps = FindAnyObjectByType<AllPlayerState>();
        if (aps != null)
        {
            float savedHealth = aps.GetHealthByName(gameObject.name); // 或者 unit.characterName
            if (savedHealth > 0f) // 找到有效血量
            {
                currentHealth = savedHealth;
                if (healthSystem != null)
                {
                    healthSystem.SetHealth(savedHealth); // 假设 HealthSystem 有 SetHealth 方法
                }
                
            }
        }

        if (TeamManager.instance != null)
        {
            CharacterInfo info = TeamManager.instance.characterInfos.Find(c => c.characterName == this.name);
            if (info != null && info.currentHealth > 0)
            {
                currentHealth = info.currentHealth;
                maxHealth = info.maxHealth;
                if (healthSystem != null)
                {
                    healthSystem.SetMaxHealth(maxHealth);
                    healthSystem.SetHealth(currentHealth);
                }
                Debug.Log($"✅ 已从 TeamManager 恢复 {name} 的血量：{currentHealth}/{maxHealth}");
            }
            else
            {
                Debug.Log(2);
            }
        }
        else
        {
            Debug.Log(1);
        }
    }

    private void Update()
    {
        if (isActive == false) return;
        if (transform.childCount == 0) return; // 防止没子物件时报错
        currentGrid = transform.parent.GetComponent<GameGrid>();
        

    }

    private void OnUnitDeath()
    {
        // 设置单位不可用
        isActive = false;

        // 停止所有移动和攻击
        StopAllCoroutines();
        isMoving = false;

        // 清除格子占用
        if (startGrid != null)
        {
            var gridComp = startGrid.GetComponent<GameGrid>();
            if (gridComp != null)
            {
                gridComp.isOccupied = false;
                gridComp.occupiedPlayer = null;
            }
        }

        // 如果有死亡动画，播放它
        // PlayDeathAnimation();
        DialogueTrigger[] triggers = GetComponents<DialogueTrigger>();
        foreach (DialogueTrigger trigger in triggers)
        {
            if (trigger.triggerType == DialogueTriggerType.AllyDeath)
            {
                trigger.TriggerManually();
            }
        }
        if (isCriticalCharacter)
        {
            HandleCriticalCharacterDeath();
        }
        Debug.Log($"单位 {name} 已死亡，不再可操作");
    }
    private void HandleCriticalCharacterDeath()
    {
        Debug.Log($"关键角色 {characterName} 死亡，游戏结束！");

        // 触发游戏结束事件
        if (TurnManager.instance != null)
        {
            TurnManager.instance.OnCriticalCharacterDeath(this);
            TurnManager.instance.HandleGameOver();
        }

        // 显示游戏结束UI
        
    }
    // 检查单位是否死亡
    public bool IsDead()
    {
        return healthSystem != null && healthSystem.IsDead;
    }

    // 在移动和攻击前检查死亡状态
    public void Move()
    {
        if (FindAnyObjectByType<DialogueSystem>().isDialoguing == true) return;
        if (IsoGrid2D.instance.isWaitingForGridClick) return;
        if(actionPoints == 0) return;
        IsoGrid2D.instance.HighlightMoveRange(currentGridPos, moveRange);
        if (IsDead())
        {
            Debug.Log("单位已死亡，无法移动");
            return;
        }
        if (!ExplorationManager.IsInExploration())
        {
            if (actionPoints <= 0) return;
        }
        else IsoGrid2D.instance.HighlightMoveRange(currentGridPos, moveRange);

    }

    public void MoveToGrid(GameGrid targetGrid)
    {
        // 探索模式下不消耗行动点
        if (!ExplorationManager.IsInExploration())
        {
            if (actionPoints <= 0) return;
            UseActionPoint(1);
        }
        
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

    private System.Collections.IEnumerator FollowPath(List<GameGrid> path)
    {
    isMoving = true;
        // ✅ 开始播放奔跑烟雾
        if (MoveVFX != null)
        {
            MoveVFX.gameObject.SetActive(true);
            MoveVFX.Play();
        }

        if (startGrid != null)
        startGrid.GetComponent<GameGrid>().isOccupied = false;

    foreach (var grid in path)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = grid.transform.position;
            Vector2Int prevPos = startPoint;
            string[] nameParts = grid.name.Split('_');
            Vector2Int nextPos = new Vector2Int(int.Parse(nameParts[1]), int.Parse(nameParts[2]));
            UpdateDirectionSprite(prevPos, nextPos); // 关键行
            float distance = Vector2.Distance(startPos, endPos);
        float travelTime = distance / moveSpeed; // 移动时间
        float elapsed = 0f;

        float jumpHeight = 0.1f; // 跳跃高度，可调
            AudioManager.Instance.PlaySFX("move");
            while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            // XY 方向平滑插值（走格子路径）
            Vector3 basePos = Vector3.Lerp(startPos, endPos, t);

            // 在 Y 上叠加跳跃（抛物线/正弦曲线都行）
            float jumpOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;

                

                transform.position = new Vector3(basePos.x, basePos.y + jumpOffset, basePos.z);

            yield return null;
        }
            
            // 最终落地到格子
            transform.position = endPos;
            

            // 更新格子占用
            if (startGrid != null)
        {
            var oldGrid = startGrid.GetComponent<GameGrid>();
            oldGrid.isOccupied = false;
            oldGrid.occupiedPlayer = null;
        }

        grid.isOccupied = true;
        grid.occupiedPlayer = this;
        startGrid = grid.gameObject;
        
        int x = int.Parse(nameParts[1]);
        int y = int.Parse(nameParts[2]);
        startPoint = new Vector2Int(x, y);
        currentGridPos = startPoint;

        IsoGrid2D.instance.currentPlayerGrid = grid.GetComponent<GameGrid>();

        transform.SetParent(grid.transform);
        transform.localPosition = Vector3.zero;

        if (sr != null)
            {
                sr.sortingOrder = grid.GetComponent<GameGrid>().sortingOrder * -1 + 2;
            }

            if (RunOutActionPoint != null)
            {
                RunOutActionPoint.GetComponent<Renderer>().sortingOrder = sr.sortingOrder;
            }
            if (XN != null)
            {
                XN.GetComponent<Renderer>().sortingOrder = sr.sortingOrder;
            }
        }

    isMoving = false;
        // ✅ 停止播放奔跑烟雾
        if (MoveVFX != null)
        {
            MoveVFX.Stop();
            MoveVFX.gameObject.SetActive(false);
        }

        Move();
    }

   

    public void TakeDamage(float amount)
    {
        if (Random.value < dodgeChance)
        {
            
            Debug.Log($"{name} 闪避了这次攻击！");
            AudioManager.Instance.PlaySFX("dodge");
            return;
        }
        DOTweenAnimation attackedTween = GetComponent<DOTweenAnimation>();
        if (attackedTween != null && attackedTween.id == "Attacked")
        {
            attackedTween.DORestart();
        }

        FindObjectOfType<CameraShake>().Shake();
        if (shield == 0)
        {
            Attacked.gameObject.SetActive(true);

            Attacked.SendEvent("OnPlay");
            if (who == Who.Heart)
            {
                AudioManager.Instance.PlaySFX("hearthurt");
            }
            if (who == Who.Female)
            {
                AudioManager.Instance.PlaySFX("fhurt");
            }

        }

        if (shield > 0)
        {
            if (shield >= amount)
            {
                shield -= amount;
                amount = 0f;
                AudioManager.Instance.PlaySFX("sheild");
            }
            else
            {
                amount -= shield;
                shield = 0f;
                Attacked.gameObject.SetActive(true);

                Attacked.SendEvent("OnPlay");

            }

            // ���»�������ʾ��ǰֵ
            healthSystem.SetShield(shield);
        }

        // ʣ���˺���Ѫ
        if (amount > 0)
        {
            currentHealth -= amount;
            healthSystem.SetHealth(currentHealth);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Debug.Log("Player is dead!");
                // TODO: ��Ϸʧ���߼�
                if (who == Who.Heart)
                {
                    AudioManager.Instance.PlaySFX("hearthurt2");
                }
                if (who == Who.Female)
                {
                    AudioManager.Instance.PlaySFX("fdie");
                }
            }
        }
        DialogueTrigger[] triggers = GetComponents<DialogueTrigger>();
        foreach (DialogueTrigger trigger in triggers)
        {
            if (trigger.triggerType == DialogueTriggerType.AllyHealthBelow)
            {
                // 血量条件在触发器的Update中自动检查
            }
            else if (trigger.triggerType == DialogueTriggerType.CustomEvent &&
                     trigger.customEventName == "OnTakeDamage")
            {
                trigger.TriggerManually();
            }
        }
        UpdateCharacterHealthRecord();
    }

    // ⚡ 在 TakeDamage() 函数末尾添加：
    private void UpdateCharacterHealthRecord()
    {
        if (TeamManager.instance == null) return;

        // 找到对应角色信息
        CharacterInfo info = TeamManager.instance.characterInfos.Find(c => c.characterName == this.name);
        if (info != null)
        {
            info.currentHealth = currentHealth;
            info.maxHealth = maxHealth;
            Debug.Log($"🩸 已同步 {name} 的血量：{currentHealth}/{maxHealth}");
        }
    }

    public void AddShield(float amount)
    {
        shield += amount;
        Debug.Log($"��һ�û��� {amount}����ǰ����ֵ: {shield}");
        healthSystem.SetShield(shield);
        sheild.gameObject.SetActive(true);
        sheild.Play();
        AudioManager.Instance.PlaySFX("shield");
    }

    
    public void Heal(float health)
    {
        Debug.Log(123);
        currentHealth += health;
        if (health > 5)
        {
            Cure.SetFloat(Shader.PropertyToID("size"), 2.5f);
            Cure.SetVector2(Shader.PropertyToID("count"), new Vector2(20,25));
            
        }
        Cure.gameObject.SetActive(true);
        Cure.Play();
        AudioManager.Instance.PlaySFX("heal");
        if (currentHealth>=maxHealth)
        {
            currentHealth=maxHealth;
        }
        healthSystem.SetHealth(currentHealth);
        UpdateCharacterHealthRecord();
    }
    public void Attack(GameGrid targetGrid)
    {
        if (IsDead())
        {
            Debug.Log("单位已死亡，无法攻击");
            return;
        }
        EnemyUnit enemy = targetGrid.GetComponentInChildren<EnemyUnit>();
        if (enemy != null)
        {
            UpdateDirectionSprite(currentGridPos, targetGrid.gridPos);
            Debug.Log($"��ҹ��� {enemy.name}����� {attackDamage} �˺���");
            Attack1.gameObject.SetActive(true);
            Attack1.Play();
            if (attackType == 1)
            {
                AudioManager.Instance.PlaySFX("sword");
            }
            if (attackType == 2 && attackAttribute == CardData.AttackAttribute.None)
            {
                AudioManager.Instance.PlaySFX("arrow");
            }
            if (attackType == 2 && attackAttribute == CardData.AttackAttribute.Fire)
            {
                AudioManager.Instance.PlaySFX("firearrow");
            }
            if (attackType == 2 && attackAttribute == CardData.AttackAttribute.Ice)
            {
                AudioManager.Instance.PlaySFX("icearrow");
            }

            float finalDamage = attackDamage;
            if (isNextAttackFire)
            {
                

                
            }
            if (isNextAttackIce)
            {
                

                
            }
            if (IsoGrid2D.instance.isFortune)
            {
                // 50% 几率造成双倍伤害
                if (Random.value < 0.5f) // Random.value 返回 [0,1) 的浮点数
                {
                    finalDamage *= 2;
                    Debug.Log("幸运触发！造成双倍伤害！");
                }
            }

            Debug.Log($"{enemy.name} 受到 {finalDamage} 点伤害");
            enemy.TakeDamage(finalDamage, this.attackAttribute);
            enemy.TakeDamage(finalDamage);

        }
        
    }
    
    public void TeleportToGrid(GameGrid targetGrid)
    {
        // �ͷ�ԭ���ĸ���
        if (startGrid != null)
        {
            startGrid.GetComponent<GameGrid>().isOccupied = false;
        }

        // ռ���µĸ���
        targetGrid.isOccupied = true;
        startGrid = targetGrid.gameObject;

        // ��������
        string[] nameParts = targetGrid.name.Split('_');
        int x = int.Parse(nameParts[1]);
        int y = int.Parse(nameParts[2]);
        startPoint = new Vector2Int(x, y);
        currentGridPos = startPoint;

        IsoGrid2D.instance.currentPlayerGrid = targetGrid;

        // ���ø��ӹ�ϵ��˲�Ƶ���������
        transform.SetParent(targetGrid.transform);
        transform.localPosition = Vector3.zero;
    }

    private bool hasPlayedRunOutVFX;
    public void UseActionPoint(int usePoint)
    {
        if (TurnManager.instance.currentController == this)
        {
            actionPoints-=usePoint;
            TurnManager.instance.UpdateActionPointUI(this);
        }
        
        Debug.Log($"剩余行动点：{TurnManager.instance.currentController.actionPoints}");
        if (actionPoints <= 0)
        {
            // 半透明
            hasPlayedRunOutVFX = true;
            RunOutActionPoint.gameObject.SetActive(true);
            RunOutActionPoint.Play(); // 直接播放特效
            Debug.Log("🎇 播放行动点耗尽特效！");
           
        }
    }

    public void RecoverActionPoint()
    {
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        actionPoints = maxActionPoints;
        TurnManager.instance.UpdateActionPointUI(this);
        if (RunOutActionPoint != null)
        {
            RunOutActionPoint.Stop();               // 停止播放
            RunOutActionPoint.gameObject.SetActive(false); // 隐藏它（可选）
            Debug.Log("🛑 停止播放耗尽行动点特效");
        }
        hasPlayedRunOutVFX = false;
    }

    public void SetActionPoint(int actionPoint)
    {
        actionPoints = actionPoint;
        TurnManager.instance.UpdateActionPointUI(this);
        
    }
    public void SetNextAttackDouble()
    {
        isNextAttackDouble = true;
        XN.SetBool(Shader.PropertyToID("isxn"), false);
        XN.gameObject.SetActive(true);
        AudioManager.Instance.PlaySFX("double");

    }
    public void SetNextAttackMass()
    {
        isNextAttackMass = true;
        XN.SetBool(Shader.PropertyToID("isxn"), true);
        XN.gameObject.SetActive(true);
        AudioManager.Instance.PlaySFX("mass");

    }
    public void SetNextAttackBloodSuck()
    {
        isNextAttackBloodSucking = true;
        sr.color = Color.cyan;
    }
    public void RecoverState()
    {
        isNextAttackBloodSucking = false;
        isNextAttackDouble = false;
        isNextAttackMass = false;
        if(XN!=null)
        {
            XN.gameObject.SetActive(false);
        }
        sr.color = Color.white;
    }
    public string vectorPropertyName = "哪个攻击"; // 属性名称

    // 修改VFX中的Vector2属性
    public void ChangeVFXVectorProperty(Vector2 newValue)
    {
        if (Attack1 != null)
        {
            // 使用SetVector2方法，传入属性名称和新的Vector2值
            Attack1.SetVector2(vectorPropertyName, newValue);
            Debug.Log($"已设置属性 '{vectorPropertyName}' 为: {newValue}");
        }
        else
        {
            Debug.LogWarning("VisualEffect组件未分配");
        }
    }
    public void SetDeadAppearance()
    {
        // 改变角色精灵颜色
        if (sr != null)
        {
            sr.color = deadColor;
        }

        // 停止所有VFX
        if (MoveVFX != null) MoveVFX.Stop();
        if (RunOutActionPoint != null) RunOutActionPoint.Stop();
        // 停止其他VFX...
    }

    public void ClearAllVFX()
    {
        Debug.Log($"清理 {name} 身上的所有VFX效果");

        // 停止并隐藏所有VFX
        if (MoveVFX != null)
        {
            MoveVFX.Stop();
            MoveVFX.gameObject.SetActive(false);
        }

        if (RunOutActionPoint != null)
        {
            RunOutActionPoint.Stop();
            RunOutActionPoint.gameObject.SetActive(false);
        }

        if (XN != null)
        {
            XN.Stop();
            XN.gameObject.SetActive(false);
        }

        if (Attacked != null)
        {
            Attacked.Stop();
            Attacked.gameObject.SetActive(false);
        }

        if (AttackedByArrow != null)
        {
            AttackedByArrow.Stop();
            AttackedByArrow.gameObject.SetActive(false);
        }

        if (Attack1 != null)
        {
            Attack1.Stop();
            Attack1.gameObject.SetActive(false);
        }

        if (Cure != null)
        {
            Cure.Stop();
            Cure.gameObject.SetActive(false);
        }

        if (sheild != null)
        {
            sheild.Stop();
            sheild.gameObject.SetActive(false);
        }

        // 重置角色颜色状态
        if (sr != null)
        {
            sr.color = Color.white;
        }

        // 重置所有攻击状态
        RecoverState();

        Debug.Log($"{name} 的VFX效果已清理完毕");
    }
    private void UpdateDirectionSprite(Vector2Int from, Vector2Int to)
    {
        Vector2Int dir = to - from;

        attackway = attackType;

        if (dir.y < 0) // 向前（地图上y减小）
        {
            sr.sprite = frontSprite;
            sr.flipX = true;

            // ✅ 调整MoveVFX缩放（X反转）
            if (MoveVFX != null)
            {
                Vector3 scale = MoveVFX.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                MoveVFX.transform.localScale = scale;
            }
            if (Attack1 != null)
            {
                Vector2 AttackAnimation = new Vector2(attackway, 1);
                ChangeVFXVectorProperty(AttackAnimation);
            }
        }
        else if (dir.y > 0) // 向后（地图上y增大）
        {
            sr.sprite = backSprite;
            sr.flipX = false;

            if (MoveVFX != null)
            {
                Vector3 scale = MoveVFX.transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                MoveVFX.transform.localScale = scale;
            }
            if (Attack1 != null)
            {
                Vector2 AttackAnimation = new Vector2(attackway, 2);
                ChangeVFXVectorProperty(AttackAnimation);
            }
        }
        else if (dir.x > 0) // 向右
        {
            sr.sprite = backSprite;
            sr.flipX = true;

            if (MoveVFX != null)
            {
                Vector3 scale = MoveVFX.transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                MoveVFX.transform.localScale = scale;
            }
            if (Attack1 != null)
            {
                Vector2 AttackAnimation = new Vector2(attackway, 0);
                ChangeVFXVectorProperty(AttackAnimation);
            }
        }
        else if (dir.x < 0) // 向左
        {
            sr.sprite = frontSprite;
            sr.flipX = false;

            if (MoveVFX != null)
            {
                Vector3 scale = MoveVFX.transform.localScale;
                scale.x = Mathf.Abs(scale.x);
                MoveVFX.transform.localScale = scale;
            }
            if (Attack1 != null)
            {
                Vector2 AttackAnimation = new Vector2(attackway, 3);
                ChangeVFXVectorProperty(AttackAnimation);
            }
        }
    }
    

}


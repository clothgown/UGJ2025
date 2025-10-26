using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueTriggerType
{
    SceneStart,          // 场景开始时
    EnemyHealthBelow,    // 敌人血量低于阈值
    AllyHealthBelow,     // 友方血量低于阈值  
    EnemyDeath,          // 敌人死亡时
    AllyDeath,           // 友方死亡时
    TurnStart,           // 回合开始时
    TurnEnd,             // 回合结束时
    CustomEvent          // 自定义事件
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("基础设置")]
    public TextAsset dialogDataFile; // 对话数据文件
    public DialogueTriggerType triggerType = DialogueTriggerType.SceneStart;
    public bool onlyTriggerOnce = true; // 是否只触发一次

    [Header("血量触发条件")]
    public float healthThreshold = 0f; // 血量阈值（百分比 0-100）
    public UnitController targetAlly;  // 目标友方（可选，为空则检查任意）
    public EnemyUnit targetEnemy;      // 目标敌人（可选，为空则检查任意）

    [Header("回合触发条件")]
    public int turnNumber = 1; // 触发回合数

    [Header("事件触发条件")]
    public string customEventName; // 自定义事件名称

    // 示例：HealthSystem.OnDeath += OnUnitDeath;
    [Header("触发设置")]
    public bool waitForCurrentDialog = true; // 是否等待当前对话结束
    public float retryDelay = 1f; // 重试延迟（秒）

    private Coroutine retryCoroutine;

    private bool hasTriggered = false;
    private DialogueSystem dialogueSystem;

    void Start()
    {
        dialogueSystem = FindObjectOfType<DialogueSystem>();

        // 场景开始时触发
        if (triggerType == DialogueTriggerType.SceneStart)
        {
            TriggerDialogue();
        }

        // 订阅事件
        SubscribeToEvents();
    }

    void Update()
    {
        if (hasTriggered && onlyTriggerOnce) return;

        // 实时检查血量条件
        if (triggerType == DialogueTriggerType.EnemyHealthBelow ||
            triggerType == DialogueTriggerType.AllyHealthBelow)
        {
            CheckHealthCondition();
        }
    }

    void OnDestroy()
    {
        if (retryCoroutine != null)
        {
            StopCoroutine(retryCoroutine);
        }
        UnsubscribeFromEvents();
    }

    // 订阅相关事件
    private void SubscribeToEvents()
    {
        // 死亡事件
        if (triggerType == DialogueTriggerType.EnemyDeath ||
            triggerType == DialogueTriggerType.AllyDeath)
        {
            // 这里需要你的HealthSystem或其他系统提供死亡事件
            // 示例：HealthSystem.OnDeath += OnUnitDeath;
        }

        // 回合事件
        if (triggerType == DialogueTriggerType.TurnStart ||
            triggerType == DialogueTriggerType.TurnEnd)
        {
            // 这里需要订阅TurnManager的回合事件
            // 示例：TurnManager.OnTurnStart += OnTurnStart;
            // 示例：TurnManager.OnTurnEnd += OnTurnEnd;
        }
    }

    // 取消订阅
    private void UnsubscribeFromEvents()
    {
        // 取消订阅所有事件
    }

    // 检查血量条件
    private void CheckHealthCondition()
    {
        if (triggerType == DialogueTriggerType.EnemyHealthBelow)
        {
            if (targetEnemy != null)
            {
                // 检查特定敌人
                float healthPercent = (targetEnemy.currentHealth / targetEnemy.maxHealth) * 100f;
                if (healthPercent <= healthThreshold)
                {
                    TriggerDialogue();
                }
            }
            else
            {
                // 检查任意敌人
                EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
                foreach (EnemyUnit enemy in enemies)
                {
                    float healthPercent = (enemy.currentHealth / enemy.maxHealth) * 100f;
                    if (healthPercent <= healthThreshold)
                    {
                        TriggerDialogue();
                        break;
                    }
                }
            }
        }
        else if (triggerType == DialogueTriggerType.AllyHealthBelow)
        {
            if (targetAlly != null)
            {
                // 检查特定友方
                float healthPercent = (targetAlly.currentHealth / targetAlly.maxHealth) * 100f;
                if (healthPercent <= healthThreshold)
                {
                    TriggerDialogue();
                }
            }
            else
            {
                // 检查任意友方
                UnitController[] allies = FindObjectsOfType<UnitController>();
                foreach (UnitController ally in allies)
                {
                    if (!ally.IsDead())
                    {
                        float healthPercent = (ally.currentHealth / ally.maxHealth) * 100f;
                        if (healthPercent <= healthThreshold)
                        {
                            TriggerDialogue();
                            break;
                        }
                    }
                }
            }
        }
    }

    // 触发对话
    public void TriggerDialogue()
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (DialogueSystem.Instance != null && dialogDataFile != null)
        {
            // 如果正在对话且需要等待
            if (DialogueSystem.Instance.IsInDialogue() && waitForCurrentDialog)
            {
                // 启动重试协程
                if (retryCoroutine == null)
                {
                    retryCoroutine = StartCoroutine(RetryTriggerDialogue());
                }
                return;
            }

            // 直接触发对话
            DialogueSystem.Instance.StartNewDialogue(dialogDataFile);
            hasTriggered = true;

            Debug.Log($"对话已触发: {gameObject.name} - {triggerType}");
        }
        else
        {
            Debug.LogWarning("对话系统或对话文件未设置！");
        }
    }
    private IEnumerator RetryTriggerDialogue()
    {
        while (DialogueSystem.Instance.IsInDialogue())
        {
            yield return new WaitForSeconds(retryDelay);
        }

        // 对话系统空闲，现在触发
        if (!hasTriggered || !onlyTriggerOnce)
        {
            DialogueSystem.Instance.StartNewDialogue(dialogDataFile);
            hasTriggered = true;
            Debug.Log($"延迟触发对话: {gameObject.name}");
        }

        retryCoroutine = null;
    }

    // 手动触发（用于自定义事件）
    public void TriggerManually()
    {
        TriggerDialogue();
    }

    // 重置触发器状态
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // 事件处理方法（需要你根据实际的事件系统来实现）
    private void OnUnitDeath(UnitController deadUnit)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.AllyDeath)
        {
            if (targetAlly == null || targetAlly == deadUnit)
            {
                TriggerDialogue();
            }
        }
    }

    private void OnEnemyDeath(EnemyUnit deadEnemy)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.EnemyDeath)
        {
            if (targetEnemy == null || targetEnemy == deadEnemy)
            {
                TriggerDialogue();
            }
        }
    }

    private void OnTurnStart(int currentTurn)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.TurnStart && currentTurn == turnNumber)
        {
            TriggerDialogue();
        }
    }

    private void OnTurnEnd(int currentTurn)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.TurnEnd && currentTurn == turnNumber)
        {
            TriggerDialogue();
        }
    }
}
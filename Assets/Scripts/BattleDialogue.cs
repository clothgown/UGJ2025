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

    private static Queue<DialogueTrigger> dialogueQueue = new Queue<DialogueTrigger>();
    private static bool isProcessingQueue = false;


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
        if (hasTriggered && onlyTriggerOnce)
        {
            Debug.Log($"对话已触发过，跳过: {gameObject.name}");
            return;
        }

        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
            {
                Debug.LogError("未找到对话系统！");
                return;
            }
        }

        if (dialogDataFile == null)
        {
            Debug.LogError($"对话文件未设置: {gameObject.name}");
            return;
        }

        Debug.Log($"尝试触发对话: {gameObject.name}, 类型: {triggerType}");

        // 如果正在对话，加入队列
        if (dialogueSystem.isDialoguing && waitForCurrentDialog)
        {
            EnqueueDialogue();
            return;
        }

        // 直接触发对话
        StartCoroutine(ExecuteDialogue());
        hasTriggered = true; // 关键：防止再次触发
    }

    // 加入对话队列
    private void EnqueueDialogue()
    {
        dialogueQueue.Enqueue(this);
        Debug.Log($"对话加入队列: {gameObject.name}, 队列长度: {dialogueQueue.Count}");

        // 如果队列处理器没有运行，启动它
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessDialogueQueue());
        }
    }

    // 处理对话队列
    private static IEnumerator ProcessDialogueQueue()
    {
        isProcessingQueue = true;
        Debug.Log("开始处理对话队列");

        while (dialogueQueue.Count > 0)
        {
            DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();

            // 等待当前对话结束
            while (dialogueSystem != null && dialogueSystem.isDialoguing)
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (dialogueQueue.Count > 0)
            {
                DialogueTrigger nextTrigger = dialogueQueue.Dequeue();
                if (nextTrigger != null && (!nextTrigger.hasTriggered || !nextTrigger.onlyTriggerOnce))
                {
                    yield return nextTrigger.StartCoroutine(nextTrigger.ExecuteDialogue());
                }
            }

            yield return new WaitForSeconds(0.1f); // 短暂延迟
        }

        isProcessingQueue = false;
        Debug.Log("对话队列处理完成");
    }

    // 执行对话
    private IEnumerator ExecuteDialogue()
    {
        Debug.Log($"开始执行对话: {gameObject.name}");

        // 再次检查对话系统
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
        }

        if (dialogueSystem == null)
        {
            Debug.LogError("对话系统未找到！");
            yield break;
        }

        // 等待直到对话系统空闲
        int maxWaitTime = 30; // 最大等待时间（30 * 0.1s = 3秒）
        int waited = 0;
        while (dialogueSystem.isDialoguing && waited < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waited++;
        }

        if (dialogueSystem.isDialoguing)
        {
            Debug.LogWarning($"对话系统忙碌超时，跳过对话: {gameObject.name}");
            yield break;
        }

        // 设置对话文件并开始对话
        dialogueSystem.battleDialogDataFile = dialogDataFile;
        dialogueSystem.StartNewDialogue();

        // 标记为已触发
        hasTriggered = true;

        Debug.Log($"对话成功触发: {gameObject.name}");

        // 等待对话开始
        yield return new WaitUntil(() => dialogueSystem.isDialoguing);
        Debug.Log($"对话已开始: {gameObject.name}");

        // 等待对话结束
        yield return new WaitUntil(() => !dialogueSystem.isDialoguing);
        Debug.Log($"对话已结束: {gameObject.name}");
    }

    // 手动触发（用于自定义事件）
    public void TriggerManually()
    {
        Debug.Log($"手动触发对话: {gameObject.name}");
        TriggerDialogue();
    }

    // 重置触发器状态
    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log($"重置对话触发器: {gameObject.name}");
    }

    // 事件处理方法...
    private void OnUnitDeath(UnitController deadUnit)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.AllyDeath)
        {
            if (targetAlly == null || targetAlly == deadUnit)
            {
                Debug.Log($"友方死亡触发对话: {deadUnit.name}");
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
                Debug.Log($"敌人死亡触发对话: {deadEnemy.name}");
                TriggerDialogue();
            }
        }
    }

    private void OnTurnStart(int currentTurn)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.TurnStart && currentTurn == turnNumber)
        {
            Debug.Log($"回合开始触发对话: 第{currentTurn}回合");
            TriggerDialogue();
        }
    }

    private void OnTurnEnd(int currentTurn)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (triggerType == DialogueTriggerType.TurnEnd && currentTurn == turnNumber)
        {
            Debug.Log($"回合结束触发对话: 第{currentTurn}回合");
            TriggerDialogue();
        }
    }
}
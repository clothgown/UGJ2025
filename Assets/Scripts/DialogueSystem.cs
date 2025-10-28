using DG.Tweening; // 记得引用DoTween命名空间
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class Dialogue
{
    public string Type;        // Type列，如 Start, #, @, E ...
    public int ID;
    public string Speaker;     // 说话者
    public string IMG;         // 表情（如 sad、jingya、normal）
    public string Position;    // 位置（左/右）
    public int NextID;
    public string Text;        // 对话文本
    public string CG;          // CG名（可选）
    public bool IsWatched;     // 是否已观看
    public string Condition;
}
public class DialogueSystem : MonoBehaviour
{
    public bool isDialoguing;
    public TextAsset dialogDataFile; // 指向你的CSV/TSV文件
    public List<Dialogue> dialogues = new List<Dialogue>();
    public TMP_Text nameTMP;     // 显示角色名字
    public TMP_Text dialogTMP;   // 显示对话内容

    private int currentID = 0;   // 当前对话ID（从0或Start开始）
    private bool isLoaded = false;

    public int currentIndex = 0; // 当前对话索引
    private Tween typingTween;    // 保存打字机Tween

    public float fadeDuration = 0.5f; // 立绘淡入时间
    public float speakTime = 1.5f;

    public DialogueCharacterManager characterManager;
    public Image leftImage;
    public Image rightImage;

    public Image cgImage;  // Inspector 指定 UI 背景 Image
    public float cgFadeDuration = 0.5f; // CG 淡入淡出时间
    public CanvasGroup UIGroup;

    public TextAsset battleDialogDataFile; // 指向你的CSV/TSV文件
    public Button[] optionButtons; // Inspector 里拖 3 个按钮进来
    public TMP_Text[] optionTexts; // 每个按钮上的文字
    public bool isChoosing = false; // 是否正在显示选项中
    private Coroutine typingCoroutine;
    private bool isTyping;

    [Header("书本对话设置")]
    public GameObject bookPanel; // 书本对话面板
    public TMP_Text bookText;    // 书本对话文本
    public Button bookCloseButton; // 书本关闭按钮
    public bool isBookDialogActive = false; // 是否正在显示书本对话


    [Header("触发器支持")]
    public bool allowMultipleDialogs = false; // 是否允许多个对话同时触发
    private Queue<TextAsset> dialogQueue = new Queue<TextAsset>(); // 对话队列

    [Header("点击控制")]
    public Button nextDialogueButton; // 用于触发下一句对话的UI按钮
    public Button skipAllButton; // 跳过所有对话的按钮
    public Image overlayBlock;
    [Header("探索模式集成")]
    public bool canTriggerExploration = true; // 是否允许通过对话触发探索模式
    // 添加一个静态实例以便全局访问
    public static DialogueSystem Instance { get; private set; }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // 初始化书本对话系统
        InitializeBookDialog();

        // 初始化按钮事件
        if (nextDialogueButton != null)
        {
            nextDialogueButton.onClick.RemoveAllListeners();
            nextDialogueButton.onClick.AddListener(OnNextDialogueButtonClick);
        }

        if (skipAllButton != null)
        {
            skipAllButton.onClick.RemoveAllListeners();
            skipAllButton.onClick.AddListener(SkipAllDialogues);
        }

        if (dialogDataFile == null)
        {
            FadeOutUI();
            isDialoguing = false;
            return;
        }

        UIGroup.gameObject.SetActive(true);
        UIGroup.alpha = 1f;
        // 初始化CG
        if (cgImage != null)
        {
            cgImage.gameObject.SetActive(true);
            cgImage.color = new Color(1, 1, 1, 0); // 透明
        }

        // 左右立绘初始化
        if (leftImage != null)
        {
            leftImage.gameObject.SetActive(true);
            leftImage.color = new Color(1, 1, 1, 0);
        }
        if (rightImage != null)
        {
            rightImage.gameObject.SetActive(true);
            rightImage.color = new Color(1, 1, 1, 0);
        }

        LoadDialogues(dialogDataFile.text);
        isLoaded = true;

        ShowDialogue(currentIndex);
    }

    void Update()
    {
        // 移除了原来的鼠标点击触发下一句对话的逻辑
        // 现在只能通过UI按钮触发
    }

    // UI按钮点击事件
    public void OnNextDialogueButtonClick()
    {
        if (isBookDialogActive) return;
        if (isLoaded && !isChoosing)
        {
            NextDialogue();
        }
    }
    private void InitializeBookDialog()
    {
        if (bookPanel != null)
        {
            bookPanel.SetActive(false);
        }

        if (bookCloseButton != null)
        {
            bookCloseButton.onClick.RemoveAllListeners();
            bookCloseButton.onClick.AddListener(OnBookCloseClicked);
        }
    }

    // 书本关闭按钮点击事件
    private void OnBookCloseClicked()
    {
        Debug.Log("书本对话关闭按钮被点击");
        HideBookDialog();
        ContinueAfterBookDialog();
    }

    // 显示书本对话
    private void ShowBookDialog(string text)
    {
        Debug.Log($"显示书本对话: {text}");

        if (bookPanel != null)
        {
            bookPanel.SetActive(true);
        }

        if (bookText != null)
        {
            bookText.text = text;
        }

        if (bookCloseButton != null)
        {
            bookCloseButton.gameObject.SetActive(true);
        }

        isBookDialogActive = true;

        // 隐藏常规对话UI
        
    }

    // 隐藏书本对话
    private void HideBookDialog()
    {
        Debug.Log("隐藏书本对话");

        if (bookPanel != null)
        {
            bookPanel.SetActive(false);
        }

        isBookDialogActive = false;
    }

    // 书本对话结束后继续
    private void ContinueAfterBookDialog()
    {
        Debug.Log("书本对话结束，继续下一段对话");

        

        // 继续下一段对话
        NextDialogue();
    }
    public void StartNewDialogue(TextAsset dialogFile = null)
    {
        // 如果指定了对话文件，使用它
        if (dialogFile != null)
        {
            battleDialogDataFile = dialogFile;
        }

        // 如果已经有对话在进行中，且不允许多重对话，则加入队列
        if (isDialoguing && !allowMultipleDialogs)
        {
            if (battleDialogDataFile != null)
            {
                dialogQueue.Enqueue(battleDialogDataFile);
                Debug.Log($"对话已加入队列，当前队列长度: {dialogQueue.Count}");
            }
            return;
        }
        currentIndex = 0;
        if (battleDialogDataFile != null)
        {
            LoadDialogues(battleDialogDataFile.text);
            isLoaded = true;
        }

        if (UIGroup != null)
        {
            UIGroup.gameObject.SetActive(true);
            UIGroup.DOFade(1f, fadeDuration).OnComplete(() => {

            });
        }
        ShowDialogue(currentIndex);
        isDialoguing = true;
    }

    void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Count)
        {
            Debug.LogWarning($"对话索引超出范围: {index}");
            FadeOutUI();
            return;
        }

        Dialogue d = dialogues[index];
        nameTMP.text = d.Speaker;
        dialogTMP.text = "";
        if (d.Type == "Exploration" || d.Text.Contains("[探索模式]"))
        {
            TriggerExplorationFromDialogue();
            return;
        }

        
        if (!string.IsNullOrEmpty(d.Condition) && !CheckCondition(d.Condition))
        {
            // 条件不满足，跳过这句对话
            int nextIndex = d.NextID;
            if (nextIndex == -1 || nextIndex >= dialogues.Count)
            {
                FadeOutUI();
                isDialoguing = false;
            }
            else
            {
                ShowDialogue(nextIndex);
                currentIndex = nextIndex;
            }
            return;
        }
        if (d.Type == "&")
        {
            Debug.Log("检测到书本对话类型");
            ShowBookDialog(d.Text);
            return;
        }

        // 立绘
        Sprite s = characterManager.GetPortrait(d.Speaker, d.IMG);
        if (s != null)
        {
            if (d.Position == "左" && leftImage != null)
            {
                leftImage.sprite = s;
                leftImage.DOFade(1f, fadeDuration);
            }
            else if (d.Position == "右" && rightImage != null)
            {
                rightImage.sprite = s;
                rightImage.DOFade(1f, fadeDuration);
            }
        }

        // CG处理
        if (cgImage != null)
        {
            if (!string.IsNullOrEmpty(d.CG))
            {
                // 有CG → 渐显
                Sprite cgSprite = characterManager.GetCG(d.CG);
                if (cgSprite != null) cgImage.sprite = cgSprite;
                cgImage.DOKill();
                cgImage.gameObject.SetActive(true);
                cgImage.DOFade(1f, cgFadeDuration);
            }
            else
            {
                // 没CG → 渐隐
                cgImage.DOKill();
                cgImage.DOFade(0f, cgFadeDuration);
                cgImage.gameObject.SetActive(false);
            }
        }

        // 打字机效果
        typingTween?.Kill();
        typingTween = dialogTMP.DOText(d.Text, speakTime).SetEase(Ease.Linear);

        if (d.Type == "Select" || d.Type == "@")
        {
            // 停止输入事件
            typingTween?.Kill();

            // 假设当前选项在 CSV 中是连续的三行
            ShowOptions(index);
        }
        else
        {
            HideOptions();
        }
        if (d.Type == "&")
        {
            // 在Book对象的TextMeshPro中显示文本
            GameObject bookObj = GameObject.Find("Book");
            if (bookObj != null)
            {
                TMP_Text bookText = bookObj.GetComponent<TMP_Text>();
                if (bookText != null)
                {
                    bookText.text = d.Text;
                }
                else
                {
                    Debug.LogWarning("Book对象上没有找到TextMeshPro组件");
                }
            }
            else
            {
                Debug.LogWarning("没有找到名为Book的GameObject");
            }

            // 书本显示后直接结束对话（NextID为E）
            FadeOutUI();
            isDialoguing = false;
            return;
        }
    }

    void ShowOptions(int startIndex)
    {

        HideOptions(); // 先隐藏所有按钮
        int count = 0;
        isChoosing = true; // 进入选项状态

        // 遍历接下来的几行，把 Type 仍是 @ 的当成选项
        for (int i = startIndex; i < dialogues.Count && count < 3; i++)
        {
            Dialogue d = dialogues[i];
            if (d.Type == "@" || d.Type == "Select")
            {
                optionButtons[count].gameObject.SetActive(true);
                optionTexts[count].text = d.Text;

                int next = d.NextID; // 捕获正确的 NextID
                Debug.Log(next);
                int idx = i;         // 捕获当前索引
                optionButtons[count].onClick.RemoveAllListeners();
                optionButtons[count].onClick.AddListener(() =>
                {

                    HideOptions();
                    isChoosing = false; // 选完恢复正常点击

                    JumpToDialogueByID(next);
                });

                count++;
            }
            else break;
        }
    }

    void JumpToDialogueByID(int id)
    {
        int idx = dialogues.FindIndex(d => d.ID == id);
        if (idx >= 0)
        {
            currentIndex = idx;
            ShowDialogue(idx);
        }
        else
        {
            Debug.LogWarning("找不到目标对话 ID：" + id);
            FadeOutUI();
        }
    }

    void HideOptions()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
        isChoosing = false;
    }

    void NextDialogue()
    {
        if (isBookDialogActive)
        {
            Debug.Log("正在显示书本对话，请先关闭书本");
            return;
        }
        // 如果当前打字还没结束，直接跳完本句
        if (typingTween != null && typingTween.IsActive() && typingTween.IsPlaying())
        {
            typingTween.Complete();
            return;
        }

        // 否则跳到下一句
        int nextIndex = dialogues[currentIndex].NextID;
        if (nextIndex == -1)
        {
            Debug.Log("对话结束。");
            FadeOutUI();
            isDialoguing = false;
        }
        else if (nextIndex < dialogues.Count)
        {
            ShowDialogue(nextIndex);
            currentIndex = nextIndex;
        }
        else
        {
            Debug.Log("对话结束。");
            FadeOutUI();
            isDialoguing = false;
        }
    }

    void FadeOutUI()
    {
        
        if (UIGroup != null)
        {
            UIGroup.DOFade(0f, fadeDuration).OnComplete(() => {
                cgImage.gameObject.SetActive(false);
                UIGroup.gameObject.SetActive(false);

                // 对话结束后检查队列
                isDialoguing = false;
                Scene currentScene = SceneManager.GetActiveScene();
                string sceneName = currentScene.name;
                if (sceneName == "1-0" && battleDialogDataFile.name == "1-0-talk2")
                {
                    FindAnyObjectByType<NextSceneManager>().EnemySpawn();
                }
                CheckDialogQueue();
            });
        }
        else
        {
            isDialoguing = false;
            CheckDialogQueue();
        }
    }
    private void ClearAllPlayerVFX()
    {
        UnitController[] allPlayers = FindObjectsOfType<UnitController>();
        Debug.Log($"找到 {allPlayers.Length} 个玩家单位，准备清理VFX");

        foreach (UnitController player in allPlayers)
        {
            if (player != null && !player.IsDead())
            {
                player.ClearAllVFX();
            }
        }

        Debug.Log("所有玩家VFX已清理");
    }
    public void TriggerExplorationFromDialogue()
    {
        if (!canTriggerExploration) return;

        Debug.Log("从对话系统触发探索模式");
        ClearAllEnemies();
        ClearAllPlayerVFX();
        // 确保TurnManager存在
        if (TurnManager.instance != null)
        {
            

            // 直接进入探索模式
            TurnManager.instance.StartExplorationMode(true);
        }
        else
        {
            Debug.LogError("TurnManager instance 未找到！");
        }

        // 结束当前对话
        ForceEndDialogue();
    }

    private void ClearAllEnemies()
    {
        EnemyUnit[] allEnemies = FindObjectsOfType<EnemyUnit>();
        Debug.Log($"找到 {allEnemies.Length} 个敌人，准备清除");

        foreach (EnemyUnit enemy in allEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        Debug.Log("所有敌人已清除");
    }

    private void CheckDialogQueue()
    {
        if (dialogQueue.Count > 0)
        {
            TextAsset nextDialog = dialogQueue.Dequeue();
            battleDialogDataFile = nextDialog;
            StartNewDialogue();
            Debug.Log($"播放队列中的下一个对话，剩余: {dialogQueue.Count}");
        }
    }

    private void EnableOverlay()
    {
        if (overlayBlock != null)
        {
            overlayBlock.raycastTarget = true;
            overlayBlock.color = new Color(0, 0, 0, 0.01f);
        }
    }

    // 禁用覆盖层
    private void DisableOverlay()
    {
        if (overlayBlock != null)
        {
            overlayBlock.raycastTarget = false;
            overlayBlock.color = new Color(0, 0, 0, 0f);
        }
    }
    // 清空对话队列
    public void ClearDialogQueue()
    {
        dialogQueue.Clear();
        Debug.Log("对话队列已清空");
    }

    // 检查是否正在对话
    public bool IsInDialogue()
    {
        return isDialoguing;
    }

    // 强制结束当前对话（用于紧急情况）
    public void ForceEndDialogue()
    {
        typingTween?.Kill();
        FadeOutUI();
        Debug.Log("overover");
        ClearDialogQueue();
    }

    // 跳过所有对话
    // 跳过所有对话
    public void SkipAllDialogues()
    {
        // 检查对话系统是否正在运行，而不是只检查isDialoguing标志
        bool hasActiveDialogues = (dialogues != null && dialogues.Count > 0) ||
                                 isDialoguing ||
                                 (UIGroup != null && UIGroup.gameObject.activeInHierarchy);

        if (!hasActiveDialogues)
        {
            Debug.Log("没有可跳过的对话。");
            return;
        }

        // 终止当前打字机效果
        if (typingTween != null && typingTween.IsActive())
        {
            typingTween.Kill();
        }

        // 清除所有选项按钮
        HideOptions();

        bool hasExplorationTrigger = dialogues.Any(d => d.Type == "Exploration" || d.Text.Contains("[探索模式]"));
        if (hasExplorationTrigger && canTriggerExploration)
        {
            Debug.Log("跳过对话时检测到探索模式触发");
            TriggerExplorationFromDialogue();
            return;
        }   // 标记所有对话为已观看（如果对话列表存在）
            if (dialogues != null)
            {
                foreach (var d in dialogues)
                {
                    d.IsWatched = true;
                }
            }

            Debug.Log("已跳过所有对话。");

            // 直接淡出UI并结束当前对话
            FadeOutUI();

            // 清除对话状态
            isDialoguing = false;
            isLoaded = false;
            currentIndex = 0;

            // 清空对话队列
            ClearDialogQueue();
        }
    
    private bool CheckCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;

        // 支持多个条件用逗号分隔
        string[] conditions = condition.Split(',');

        foreach (string cond in conditions)
        {
            if (!EvaluateSingleCondition(cond.Trim()))
            {
                return false;
            }
        }

        return true;
    }

    // ✅ 新增：单个条件评估
    private bool EvaluateSingleCondition(string condition)
    {
        // 条件格式示例：
        // "enemy_health<50"      - 任意敌人血量低于50%
        // "enemy1_health<30"     - 特定敌人血量低于30%
        // "ally_health>80"       - 任意友方血量高于80%
        // "ally2_health>50"      - 特定友方血量高于50%
        // "ally_dead"            - 任意友方死亡
        // "ally1_dead"           - 特定友方死亡
        // "enemy_all_dead"       - 所有敌人都死亡

        string[] parts = condition.Split('_');
        if (parts.Length < 2) return true; // 条件格式错误，默认通过

        string target = parts[0];  // enemy 或 ally
        string conditionType = parts[1]; // health 或 dead

        // 处理死亡条件
        if (conditionType == "dead")
        {
            return CheckDeathCondition(target);
        }

        // 处理血量条件
        if (conditionType.StartsWith("health"))
        {
            return CheckHealthCondition(target, conditionType);
        }

        // 处理全体死亡条件
        if (conditionType == "all_dead")
        {
            return CheckAllDeadCondition(target);
        }

        return true; // 未知条件类型，默认通过
    }

    // ✅ 新增：检查死亡条件
    private bool CheckDeathCondition(string target)
    {
        if (target == "ally")
        {
            // 检查是否有任意友方死亡
            foreach (UnitController ally in TurnManager.instance.unitControllers)
            {
                if (ally != null && ally.IsDead())
                {
                    return true;
                }
            }
            return false;
        }
        else if (target.StartsWith("ally_"))
        {
            // 检查特定友方死亡 (ally_Player1, ally_Knight等)
            string allyName = target.Substring(5); // 去掉 "ally_" 前缀
            foreach (UnitController ally in TurnManager.instance.unitControllers)
            {
                if (ally != null && ally.gameObject.name == allyName && ally.IsDead())
                {
                    return true;
                }
            }
            return false;
        }
        else if (target == "enemy")
        {
            // 检查是否有任意敌人死亡
            EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null && enemy.IsDead())
                {
                    return true;
                }
            }
            return false;
        }
        else if (target.StartsWith("enemy_"))
        {
            // 检查特定敌人死亡 (enemy_Goblin, enemy_Boss等)
            string enemyName = target.Substring(6); // 去掉 "enemy_" 前缀
            EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.name == enemyName && enemy.IsDead())
                {
                    return true;
                }
            }
            return false;
        }

        return false;
    }

    // ✅ 新增：检查血量条件
    // ✅ 修改：检查血量条件（使用GameObject名称）
    private bool CheckHealthCondition(string target, string conditionType)
    {
        // 解析条件：health<50 或 health>80
        string operatorStr = conditionType.Contains(">") ? ">" : "<";
        string[] healthParts = conditionType.Split(new char[] { '<', '>' });

        if (healthParts.Length < 2) return true;

        float threshold;
        if (!float.TryParse(healthParts[1], out threshold)) return true;

        if (target == "ally")
        {
            // 检查任意友方血量条件
            foreach (UnitController ally in TurnManager.instance.unitControllers)
            {
                if (ally != null && !ally.IsDead())
                {
                    float healthPercent = (ally.currentHealth / ally.maxHealth) * 100f;
                    if (operatorStr == ">" && healthPercent > threshold) return true;
                    if (operatorStr == "<" && healthPercent < threshold) return true;
                }
            }
            return false;
        }
        else if (target.StartsWith("ally_"))
        {
            // 检查特定友方血量条件
            string allyName = target.Substring(5); // 去掉 "ally_" 前缀
            foreach (UnitController ally in TurnManager.instance.unitControllers)
            {
                if (ally != null && !ally.IsDead() && ally.gameObject.name == allyName)
                {
                    float healthPercent = (ally.currentHealth / ally.maxHealth) * 100f;
                    if (operatorStr == ">" && healthPercent > threshold) return true;
                    if (operatorStr == "<" && healthPercent < threshold) return true;
                }
            }
            return false;
        }
        else if (target == "enemy")
        {
            // 检查任意敌人数值条件
            EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    float healthPercent = (enemy.currentHealth / enemy.maxHealth) * 100f;
                    if (operatorStr == ">" && healthPercent > threshold) return true;
                    if (operatorStr == "<" && healthPercent < threshold) return true;
                }
            }
            return false;
        }
        else if (target.StartsWith("enemy_"))
        {
            // 检查特定敌人血量条件
            string enemyName = target.Substring(6); // 去掉 "enemy_" 前缀
            EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead() && enemy.gameObject.name == enemyName)
                {
                    float healthPercent = (enemy.currentHealth / enemy.maxHealth) * 100f;
                    if (operatorStr == ">" && healthPercent > threshold) return true;
                    if (operatorStr == "<" && healthPercent < threshold) return true;
                }
            }
            return false;
        }

        return false;
    }

    // ✅ 新增：检查全体死亡条件
    private bool CheckAllDeadCondition(string target)
    {
        if (target == "enemy")
        {
            // 检查是否所有敌人都死亡
            EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
            if (enemies.Length == 0) return true;

            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    return false;
                }
            }
            return true;
        }
        else if (target == "ally")
        {
            // 检查是否所有友方都死亡
            foreach (UnitController ally in TurnManager.instance.unitControllers)
            {
                if (ally != null && !ally.IsDead())
                {
                    return false;
                }
            }
            return true;
        }

        return false;
    }

    void OnClickNext()
    {
        ShowDialogue(currentID);
    }

    /// <summary>
    /// 加载并解析对话文本（支持逗号或制表符分隔，支持引号内含分隔符）
    /// </summary>
    /// <param name="text">TextAsset.text</param>
    public void LoadDialogues(string text)
    {
        dialogues.Clear();
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("输入文本为空。");
            return;
        }

        // 规范换行（统一为 \n）
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // 先拿第一行（表头）判断分隔符（逗号或制表符）
        int firstNewline = text.IndexOf('\n');
        string headerLine = firstNewline >= 0 ? text.Substring(0, firstNewline) : text;
        char delimiter = headerLine.Contains("\t") ? '\t' : ','; // 简单检测：优先制表符

        // 我们需要按行遍历，但要注意可能有字段内的换行（被引号包裹）。
        // 所以不能简单 Split('\n') —— 改为手动逐字符解析，按 CSV 引号规则切行。
        List<string> rows = SplitCsvRows(text);

        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("未解析出任何行。");
            return;
        }

        // 跳过表头（假设第一行是 header）
        for (int i = 1; i < rows.Count; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrEmpty(row)) continue;

            List<string> cells = ParseCsvLine(row);
            // 我们期望至少 6 列（Type, ID, Speaker, Position, NextID, Text），第7列CG可选
            if (cells.Count < 6)
            {
                Debug.LogWarning($"第 {i + 1} 行列数不足：{cells.Count} -> \"{row}\"");
                continue;
            }

            Dialogue d = new Dialogue();
            d.Type = cells[0].Trim();
            int id; if (!int.TryParse(cells[1].Trim(), out id)) id = -1;
            d.ID = id;
            d.Speaker = cells[2].Trim();
            d.IMG = cells[3].Trim();
            d.Position = cells[4].Trim();
            int nextid; if (!int.TryParse(cells[5].Trim(), out nextid)) nextid = -1;
            d.NextID = nextid;
            d.Text = cells[6].Replace("\\n", "\n"); // 如果CSV里用 \n 表示换行
            d.CG = cells.Count > 7 ? cells[7].Trim() : "";
            d.Condition = cells.Count > 8 ? cells[8].Trim() : "";
            d.IsWatched = false;

            dialogues.Add(d);
        }
    }

    /// <summary>
    /// 将整个文本拆为"逻辑行"，考虑引号内的换行
    /// </summary>
    List<string> SplitCsvRows(string text)
    {
        List<string> rows = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                // 引号遇到双引号转义时要跳过成对的双引号
                if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    cur.Append('"');
                    i++; // 跳过下一个双引号
                }
                else
                {
                    inQuotes = !inQuotes;
                    // 不把外层引号加入结果
                }
            }
            else if (c == '\n')
            {
                if (inQuotes)
                {
                    // 引号内部的换行应保留为字段内容的一部分
                    cur.Append('\n');
                }
                else
                {
                    rows.Add(cur.ToString());
                    cur.Length = 0;
                }
            }
            else
            {
                cur.Append(c);
            }
        }
        // 添加最后一行（如果有残余）
        if (cur.Length > 0)
        {
            rows.Add(cur.ToString());
        }
        return rows;
    }

    /// <summary>
    /// 解析单行 CSV，支持逗号/制表符与引号规则
    /// </summary>
    List<string> ParseCsvLine(string line)
    {
        List<string> cells = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 双引号转义 -> 添加一个引号到字段中
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && (c == ',' || c == '\t'))
            {
                // 逗号或制表符为分隔符（仅在非引号内有效）
                cells.Add(cur.ToString());
                cur.Length = 0;
            }
            else
            {
                cur.Append(c);
            }
        }
        // 最后一格
        cells.Add(cur.ToString());
        // Trim 每个单元格两侧的空白（但保留内部空格）
        for (int j = 0; j < cells.Count; j++)
        {
            cells[j] = cells[j].Trim();
        }
        return cells;
    }
}
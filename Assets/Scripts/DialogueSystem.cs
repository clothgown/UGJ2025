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
    // 新增：随机分支支持
    [NonSerialized] public List<RandomBranch> randomBranches = new List<RandomBranch>();
    public string RandomBranchesData; // 用于存储CSV中的随机分支数据

    // 新增：效果支持
    public string Effects; // 效果字符串，格式：effect1:param1,param2;effect2:param1
}

// 新增：随机分支类
[Serializable]
public class RandomBranch
{
    public int nextID;         // 跳转到的对话ID
    public int probability;    // 概率（百分比）
    public string condition;   // 额外条件（可选）

    public RandomBranch(int nextID, int probability, string condition = "")
    {
        this.nextID = nextID;
        this.probability = probability;
        this.condition = condition;
    }
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

    [Header("随机分支设置")]
    public bool enableRandomBranches = true;

    [Header("立绘设置")]
    public bool autoSetNativeSize = true; // 自动设置为图片原生大小
    public Vector2 maxPortraitSize = new Vector2(400, 400); // 最大立绘尺寸限制
    public bool maintainAspectRatio = true; // 保持宽高比

    

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

        // 检查条件
        if (!string.IsNullOrEmpty(d.Condition) && !CheckCondition(d.Condition))
        {
            Debug.Log($"条件不满足，跳过对话: {d.Condition}");
            int nextIndex = dialogues.FindIndex(dial => dial.ID == d.NextID);
            if (nextIndex == -1)
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
        if (d.Type == "Exploration" || d.Text.Contains("[探索模式]"))
        {
            TriggerExplorationFromDialogue();
            return;
        }
        // 处理随机分支
        if (d.Type == "R" && enableRandomBranches)
        {
            Debug.Log($"处理随机分支对话 ID={d.ID}, 分支数={d.randomBranches.Count}");
            HandleRandomBranch(d);
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
                SetPortraitImage(leftImage, s, d.Position);
                leftImage.DOFade(1f, fadeDuration);

                // 淡出另一侧的立绘
                if (rightImage != null)
                    rightImage.DOFade(0.6f, fadeDuration);
            }
            else if (d.Position == "右" && rightImage != null)
            {
                SetPortraitImage(rightImage, s, d.Position);
                rightImage.DOFade(1f, fadeDuration);

                // 淡出另一侧的立绘
                if (leftImage != null)
                    leftImage.DOFade(0.6f, fadeDuration);
            }
        }
        else
        {
            // 如果没有立绘，淡出所有立绘
            if (leftImage != null) leftImage.gameObject.SetActive(false);
            if (rightImage != null) rightImage.gameObject.SetActive(false);
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
                cgImage.DOFade(0.5f, cgFadeDuration);
                cgImage.gameObject.SetActive(false);
            }
        }

        // 打字机效果
        typingTween?.Kill();
        typingTween = dialogTMP.DOText(d.Text, speakTime).SetEase(Ease.Linear);

        if (d.Type == "Select" || d.Type == "@")
        {
            // 停止打字机效果
            typingTween?.Kill();
            dialogTMP.text = d.Text;

            // 显示选项（包含条件检查）
            ShowOptions(index);
        }
        else
        {
            HideOptions();
        }
        if (!string.IsNullOrEmpty(d.Effects))
        {
            Debug.Log($"执行效果: {d.Effects}");
            ExecuteEffects(d.Effects);
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
    private void SetPortraitImage(Image image, Sprite sprite, string position)
    {
        if (image == null || sprite == null) return;

        image.sprite = sprite;
        image.gameObject.SetActive(true);

        // 自动设置原生大小
        if (autoSetNativeSize)
        {
            SetImageNativeSize(image, maxPortraitSize);
        }

        // 根据位置调整一些额外设置（可选）
       

        Debug.Log($"设置立绘: {sprite.name}, 尺寸: {image.rectTransform.sizeDelta}, 位置: {position}");
    }

    // 设置图片为原生大小，带最大尺寸限制
    private void SetImageNativeSize(Image image, Vector2 maxSize)
    {
        if (image.sprite == null) return;

        RectTransform rectTransform = image.rectTransform;
        Sprite sprite = image.sprite;

        // 获取精灵的原始尺寸
        Vector2 nativeSize = new Vector2(sprite.rect.width, sprite.rect.height);

        // 如果启用了保持宽高比，并且有最大尺寸限制
        if (maintainAspectRatio && maxSize.x > 0 && maxSize.y > 0)
        {
            // 计算缩放比例，确保图片不超过最大尺寸
            float widthRatio = maxSize.x / nativeSize.x;
            float heightRatio = maxSize.y / nativeSize.y;
            float scaleRatio = Mathf.Min(widthRatio, heightRatio, 1f); // 不超过1，即不超过原始大小

            // 应用缩放后的尺寸
            rectTransform.sizeDelta = nativeSize * scaleRatio;
        }
        else
        {
            // 直接使用原始尺寸
            rectTransform.sizeDelta = nativeSize;
        }

        

        Debug.Log($"图片原生尺寸设置: {sprite.name} -> 原始: {nativeSize}, 最终: {rectTransform.sizeDelta}");
    }

    void ShowOptions(int startIndex)
    {
        HideOptions(); // 先隐藏所有按钮
        int count = 0;
        isChoosing = true; // 进入选项状态

        // 收集所有满足条件的选项
        List<Dialogue> validOptions = new List<Dialogue>();
        List<int> optionIndices = new List<int>();

        // 遍历接下来的几行，把 Type 仍是 @ 或 Select 的当成选项
        for (int i = startIndex; i < dialogues.Count && count < 3; i++)
        {
            Dialogue d = dialogues[i];
            if (d.Type == "@" || d.Type == "Select")
            {
                // 检查选项条件
                if (string.IsNullOrEmpty(d.Condition) || CheckCondition(d.Condition))
                {
                    validOptions.Add(d);
                    optionIndices.Add(i);
                    count++;
                }
                else
                {
                    Debug.Log($"选项条件不满足，跳过选项: {d.Text}, 条件: {d.Condition}");
                }
            }
            else
            {
                break; // 遇到非选项对话，停止收集
            }
        }

        // 如果没有有效的选项，处理默认行为
        if (validOptions.Count == 0)
        {
            Debug.LogWarning("没有满足条件的选项，寻找默认跳转");
            HandleNoValidOptions(startIndex);
            return;
        }

        // 显示有效的选项
        for (int i = 0; i < validOptions.Count && i < 3; i++)
        {
            Dialogue d = validOptions[i];
            int optionIndex = optionIndices[i]; // 捕获当前选项索引

            optionButtons[i].gameObject.SetActive(true);
            optionTexts[i].text = d.Text;

            int next = d.NextID; // 捕获正确的 NextID
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() =>
            {
                HideOptions();
                isChoosing = false; // 选完恢复正常点击

                JumpToDialogueByID(next);

                // 原有的场景特殊处理代码...
                Scene currentScene = SceneManager.GetActiveScene();
                string sceneName = currentScene.name;
                if (sceneName == "1-0" && battleDialogDataFile != null && battleDialogDataFile.name == "1-0-talk3")
                {
                    // 使用闭包捕获的 optionIndex
                    if (optionIndex == startIndex + 2) // 第三个选项（索引从0开始）
                    {
                        TeamManager teamManager = FindAnyObjectByType<TeamManager>();
                        if (teamManager != null)
                        {
                            CharacterInfo targetInfo = teamManager.characterInfos.Find(c => c.id == 1);
                            if (targetInfo != null)
                            {
                                targetInfo.isUnlocked = true;
                                Debug.Log("✅ 已解锁角色 ID=1！");

                                Button matchedButton = teamManager.headUIButtons.Find(b => b.name == targetInfo.buttonName);
                                if (matchedButton != null)
                                    matchedButton.gameObject.SetActive(true);

                                UnitController matchedUnit = teamManager.unitControllers.Find(u => u.name == targetInfo.characterName);
                                if (matchedUnit != null)
                                    matchedUnit.gameObject.SetActive(true);
                            }
                            else
                            {
                                Debug.LogWarning("⚠️ 未找到 ID=1 的角色信息。");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ 未找到 TeamManager 实例。");
                        }
                    }
                }
            });
        }
    }

    // 新增：处理没有有效选项的情况
    private void HandleNoValidOptions(int startIndex)
    {
        // 寻找第一个无条件选项作为默认选项
        for (int i = startIndex; i < dialogues.Count; i++)
        {
            Dialogue d = dialogues[i];
            if (d.Type == "@" || d.Type == "Select")
            {
                if (string.IsNullOrEmpty(d.Condition))
                {
                    Debug.Log($"使用无条件选项作为默认: {d.Text}");
                    // 自动选择这个选项
                    int nextIndex = dialogues.FindIndex(dial => dial.ID == d.NextID);
                    if (nextIndex >= 0)
                    {
                        currentIndex = nextIndex;
                        ShowDialogue(nextIndex);
                    }
                    else
                    {
                        FadeOutUI();
                    }
                    return;
                }
            }
            else
            {
                break; // 遇到非选项对话，停止搜索
            }
        }

        // 如果找不到无条件选项，寻找父对话的NextID
        Dialogue parentDialogue = dialogues[startIndex - 1]; // 假设选项前是父对话
        if (parentDialogue != null && parentDialogue.NextID != -1)
        {
            Debug.Log($"使用父对话的NextID作为默认跳转: {parentDialogue.NextID}");
            int nextIndex = dialogues.FindIndex(dial => dial.ID == parentDialogue.NextID);
            if (nextIndex >= 0)
            {
                currentIndex = nextIndex;
                ShowDialogue(nextIndex);
            }
            else
            {
                FadeOutUI();
            }
        }
        else
        {
            // 最后保底：结束对话
            Debug.LogWarning("没有找到有效的选项或默认跳转，结束对话");
            FadeOutUI();
        }
    }

    void JumpToDialogueByID(int id)
    {
        int idx = dialogues.FindIndex(d => d.ID == id);
        if (idx >= 0)
        {
            // 检查目标对话的条件
            Dialogue targetDialogue = dialogues[idx];
            if (!string.IsNullOrEmpty(targetDialogue.Condition) && !CheckCondition(targetDialogue.Condition))
            {
                Debug.Log($"跳转目标对话条件不满足: {targetDialogue.Condition}");
                // 如果条件不满足，寻找下一个有效的对话
                int nextIndex = dialogues.FindIndex(dial => dial.ID == targetDialogue.NextID);
                if (nextIndex >= 0)
                {
                    JumpToDialogueByID(dialogues[nextIndex].ID);
                }
                else
                {
                    FadeOutUI();
                }
                return;
            }

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
    // 新增：跳到下一个决策点（选项、随机分支、探索模式等）
    // 新的跳过逻辑：跳到决策前一句话
    public void SkipToNextDecisionPoint()
    {
        // 如果正在显示书本对话，先关闭它
        if (isBookDialogActive)
        {
            HideBookDialog();
        }

        // 终止当前打字机效果，立即显示完整文本
        if (typingTween != null && typingTween.IsActive())
        {
            typingTween.Kill();
            if (currentIndex >= 0 && currentIndex < dialogues.Count)
            {
                Dialogue currentDialogue = dialogues[currentIndex];
                dialogTMP.text = currentDialogue.Text;
            }
            return; // 第一次点击跳过，只完成当前打字效果
        }

        // 清除所有选项按钮（如果有）
        HideOptions();

        // 从当前对话的下一个开始，寻找决策点
        int searchIndex = currentIndex + 1;
        int lastNormalDialogue = -1; // 记录最后一个普通对话

        while (searchIndex < dialogues.Count)
        {
            Dialogue candidate = dialogues[searchIndex];

            // 检查是否是决策点
            if (IsDecisionPoint(candidate))
            {
                Debug.Log($"找到决策点 at index {searchIndex}, ID={candidate.ID}, Type={candidate.Type}");

                // 如果找到了决策点，但我们之前记录过普通对话，就停在最后一个普通对话
                if (lastNormalDialogue != -1)
                {
                    Debug.Log($"停在决策点前一句话: index {lastNormalDialogue}");
                    currentIndex = lastNormalDialogue;
                    ShowDialogue(currentIndex);
                    return;
                }
                else
                {
                    // 如果没有普通对话，直接停在决策点
                    Debug.Log($"直接停在决策点: index {searchIndex}");
                    currentIndex = searchIndex;
                    ShowDialogue(currentIndex);
                    return;
                }
            }

            // 检查是否是普通对话（非决策点）
            if (IsNormalDialogue(candidate))
            {
                lastNormalDialogue = searchIndex;
                // 标记为已观看
                candidate.IsWatched = true;
            }

            // 如果遇到结束点，也停下来
            if (candidate.NextID == -1 || candidate.Type == "E")
            {
                Debug.Log($"遇到对话结束点 at index {searchIndex}, 停止跳过");
                if (lastNormalDialogue != -1)
                {
                    currentIndex = lastNormalDialogue;
                    ShowDialogue(currentIndex);
                }
                else
                {
                    // 直接结束对话
                    FadeOutUI();
                }
                return;
            }

            searchIndex++;
        }

        // 如果没找到决策点，且已到末尾，则正常结束对话
        Debug.Log("未找到后续决策点，结束对话");
        FadeOutUI();
    }

    // 判断是否是普通对话（可以安全跳过的对话）
    private bool IsNormalDialogue(Dialogue dialogue)
    {
        // 普通对话类型：Start, #, 以及其他没有特殊功能的类型
        string[] normalTypes = { "Start", "#" };

        // 没有特殊条件、效果、分支的对话
        return Array.Exists(normalTypes, type => type == dialogue.Type) &&
               string.IsNullOrEmpty(dialogue.Condition) &&
               string.IsNullOrEmpty(dialogue.Effects) &&
               string.IsNullOrEmpty(dialogue.RandomBranchesData);
    }

    // 判断是否是决策点（需要玩家参与的对话）
    private bool IsDecisionPoint(Dialogue dialogue)
    {
        // 包含选项的对话
        if (dialogue.Type == "Select" || dialogue.Type == "@")
        {
            return true;
        }

        // 随机分支对话
        if (dialogue.Type == "R" && enableRandomBranches)
        {
            return true;
        }

        // 书本对话
        if (dialogue.Type == "&")
        {
            return true;
        }

        // 有条件的对话（可能影响分支）
        if (!string.IsNullOrEmpty(dialogue.Condition))
        {
            return true;
        }

        // 有效果的对话（可能影响游戏状态）
        if (!string.IsNullOrEmpty(dialogue.Effects))
        {
            return true;
        }

        return false;
    }

    // 修改原来的 SkipAllDialogues 方法，或者用上面的新方法替代
    public void SkipAllDialogues()
    {
        // 检查是否正在运行
        bool hasActiveDialogues = (dialogues != null && dialogues.Count > 0) ||
                                 isDialoguing ||
                                 (UIGroup != null && UIGroup.gameObject.activeInHierarchy);

        if (!hasActiveDialogues)
        {
            Debug.Log("没有可跳过的对话。");
            return;
        }

        // 如果有探索模式触发，特殊处理
        bool hasExplorationTrigger = dialogues.Any(d => d.Type == "Exploration" || d.Text.Contains("[探索模式]"));
        if (hasExplorationTrigger && canTriggerExploration)
        {
            Debug.Log("跳过对话时检测到探索模式触发");
            TriggerExplorationFromDialogue();
            return;
        }

        // 使用新的跳到决策点功能
        SkipToNextDecisionPoint();
    }

    private bool CheckCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
        {
            Debug.Log("条件为空，默认通过");
            return true;
        }

        Debug.Log($"检查条件: {condition}");

        // 支持多个条件用逗号分隔
        string[] conditions = condition.Split(',');

        foreach (string cond in conditions)
        {
            bool result = EvaluateSingleCondition(cond.Trim());
            Debug.Log($"条件 '{cond.Trim()}' 结果: {result}");

            if (!result)
            {
                Debug.Log($"条件不满足: {cond.Trim()}");
                return false;
            }
        }

        Debug.Log("所有条件都满足");
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

        if (condition.StartsWith("coin"))
        {
            return CheckCoinCondition(condition);
        }

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
    private bool CheckCoinCondition(string condition)
    {
        try
        {
            // 格式: coin>=10, coin<50, coin==100
            string operatorStr = "";
            string valueStr = "";

            // 提取运算符和数值
            if (condition.Contains(">="))
            {
                operatorStr = ">=";
                valueStr = condition.Substring(condition.IndexOf(">=") + 2);
            }
            else if (condition.Contains("<="))
            {
                operatorStr = "<=";
                valueStr = condition.Substring(condition.IndexOf("<=") + 2);
            }
            else if (condition.Contains(">"))
            {
                operatorStr = ">";
                valueStr = condition.Substring(condition.IndexOf(">") + 1);
            }
            else if (condition.Contains("<"))
            {
                operatorStr = "<";
                valueStr = condition.Substring(condition.IndexOf("<") + 1);
            }
            else if (condition.Contains("=="))
            {
                operatorStr = "==";
                valueStr = condition.Substring(condition.IndexOf("==") + 2);
            }
            else if (condition.Contains("!="))
            {
                operatorStr = "!=";
                valueStr = condition.Substring(condition.IndexOf("!=") + 2);
            }
            else
            {
                Debug.LogWarning($"未知的金币条件运算符: {condition}");
                return true;
            }

            // 解析数值
            if (int.TryParse(valueStr.Trim(), out int requiredCoins))
            {
                // 获取当前金币数量
                int currentCoins = 0;
                if (CollectionManager.instance != null)
                {
                    currentCoins = CollectionManager.instance.GetCoinCount();
                }
                else
                {
                    Debug.LogWarning("CollectionManager 实例未找到，无法检查金币条件");
                    return true; // 如果找不到管理器，默认通过条件
                }

                // 根据运算符进行比较
                bool result = false;
                switch (operatorStr)
                {
                    case ">=": result = currentCoins >= requiredCoins; break;
                    case "<=": result = currentCoins <= requiredCoins; break;
                    case ">": result = currentCoins > requiredCoins; break;
                    case "<": result = currentCoins < requiredCoins; break;
                    case "==": result = currentCoins == requiredCoins; break;
                    case "!=": result = currentCoins != requiredCoins; break;
                }

                Debug.Log($"金币条件检查: {currentCoins} {operatorStr} {requiredCoins} = {result}");
                return result;
            }
            else
            {
                Debug.LogWarning($"无法解析金币条件数值: {condition}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"检查金币条件时出错: {condition}, 错误: {e.Message}");
            return true;
        }
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
    private void HandleRandomBranch(Dialogue d)
    {
        if (d.randomBranches.Count == 0)
        {
            Debug.LogError($"随机分支对话 {d.ID} 没有配置分支选项！");
            FadeOutUI();
            isDialoguing = false;
            return;
        }

        // 计算总概率并筛选满足条件的分支
        var validBranches = new List<RandomBranch>();
        int totalProbability = 0;

        foreach (var branch in d.randomBranches)
        {
            // 检查分支条件
            if (string.IsNullOrEmpty(branch.condition) || CheckCondition(branch.condition))
            {
                validBranches.Add(branch);
                totalProbability += branch.probability;
            }
        }

        if (validBranches.Count == 0)
        {
            Debug.LogWarning($"随机分支对话 {d.ID} 没有满足条件的分支！");
            FadeOutUI();
            isDialoguing = false;
            return;
        }

        // 随机选择分支
        int randomValue = UnityEngine.Random.Range(0, totalProbability);
        int accumulatedProbability = 0;
        RandomBranch selectedBranch = null;

        foreach (var branch in validBranches)
        {
            accumulatedProbability += branch.probability;
            if (randomValue < accumulatedProbability)
            {
                selectedBranch = branch;
                break;
            }
        }

        if (selectedBranch == null)
        {
            selectedBranch = validBranches[0]; // 保底选择第一个
        }

        Debug.Log($"随机分支选择: 从 {validBranches.Count} 个分支中选择 ID={selectedBranch.nextID}, 概率={selectedBranch.probability}%");

        // 跳转到选中的分支
        int nextIndex = dialogues.FindIndex(dial => dial.ID == selectedBranch.nextID);
        if (nextIndex >= 0)
        {
            currentIndex = nextIndex;
            ShowDialogue(nextIndex);
        }
        else
        {
            Debug.LogError($"找不到随机分支目标对话 ID: {selectedBranch.nextID}");
            FadeOutUI();
            isDialoguing = false;
        }
    }

    // 执行效果
    private void ExecuteEffects(string effectsString)
    {
        Debug.Log($"执行效果: {effectsString}");

        // 格式: effect1:param1,param2;effect2:param1,param2
        string[] effectGroups = effectsString.Split(';');

        foreach (string effectGroup in effectGroups)
        {
            if (string.IsNullOrEmpty(effectGroup.Trim())) continue;

            string[] parts = effectGroup.Split(':');
            if (parts.Length < 2) continue;

            string effectType = parts[0].Trim().ToLower();
            string parameters = parts[1].Trim();

            ExecuteSingleEffect(effectType, parameters);
        }
    }

    // 执行单个效果
    private void ExecuteSingleEffect(string effectType, string parameters)
    {
        string[] paramArray = parameters.Split('|');

        switch (effectType)
        {
            case "heal":
                HandleHealEffect(paramArray);
                break;
            case "damage":
                HandleDamageEffect(paramArray);
                break;
            case "coin":
                HandleCoinEffect(paramArray);
                break;
            case "maxhealth":
                HandleMaxHealthEffect(paramArray);
                break;
            case "sethealth":
                HandleSetHealthEffect(paramArray);
                break;
            case "event": // 新增：事件触发
                Debug.Log("检测到 event 效果类型");
                HandleEventEffect(paramArray);
                break;
            default:
                Debug.LogWarning($"未知的效果类型: {effectType}");
                break;
        }
    }
    private void HandleEventEffect(string[] parameters)
    {
        if (parameters.Length == 0) return;

        string eventName = parameters[0].Trim();

        switch (eventName)
        {
            case "endDialogue":
                OnDialogueEnd();
                break;
            case "startBattle":
                StartBattle();
                break;
            case "openShop":
                OpenShop();
                break;
            case "changeScene":
                if (parameters.Length > 1)
                    ChangeScene(parameters[1]);
                break;
            case "activate": // 新增加：激活GameObject
                if (parameters.Length > 1)
                {
                    string objectName = parameters[1].Trim();
                    Debug.Log($"尝试激活子级对象: '{objectName}'");

                    // 在当前脚本所在GameObject的子级中查找
                    GameObject obj = FindChildObject(objectName);

                    if (obj != null)
                    {
                        Debug.Log($"找到子级对象: {obj.name}, 激活前状态: {obj.activeSelf}");
                        obj.SetActive(true);
                        Debug.Log($"已激活 {obj.name}, 激活后状态: {obj.activeSelf}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到名为 '{objectName}' 的子级对象");
                        ListAllChildren(); // 列出所有子对象帮助调试
                    }
                }
                else
                {
                    Debug.LogError("activate 事件缺少对象名称参数");
                }
                break;
            case "deactivate": // 新增加：禁用GameObject
                if (parameters.Length > 1)
                {
                    string objectName = parameters[1].Trim();
                    Debug.Log($"尝试激活子级对象: '{objectName}'");

                    // 在当前脚本所在GameObject的子级中查找
                    GameObject obj = FindChildObject(objectName);

                    if (obj != null)
                    {
                        Debug.Log($"找到子级对象: {obj.name}, 激活前状态: {obj.activeSelf}");
                        obj.SetActive(true);
                        Debug.Log($"已激活 {obj.name}, 激活后状态: {obj.activeSelf}");
                    }
                    else
                    {
                        Debug.LogWarning($"未找到名为 '{objectName}' 的子级对象");
                        ListAllChildren(); // 列出所有子对象帮助调试
                    }
                }
                else
                {
                    Debug.LogError("activate 事件缺少对象名称参数");
                }
                break;
            default:
                Debug.LogWarning($"未知的事件: {eventName}");
                break;
        }

        Debug.Log($"触发事件: {eventName}");
    }
    private GameObject FindChildObject(string childName)
    {
        // 方法1: 使用 Transform.Find (支持简单路径)
        Transform child = transform.Find(childName);
        if (child != null)
        {
            Debug.Log($"方法1 - Transform.Find 找到: {child.name}");
            return child.gameObject;
        }

        // 方法2: 递归查找所有子对象
        GameObject found = FindInChildren(transform, childName);
        if (found != null)
        {
            Debug.Log($"方法2 - 递归查找找到: {found.name}");
            return found;
        }

        // 方法3: 使用 GetComponentsInChildren (包括非激活对象)
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (t.name == childName && t != transform) // 排除自己
            {
                Debug.Log($"方法3 - GetComponentsInChildren 找到: {t.name}");
                return t.gameObject;
            }
        }

        return null;
    }

    // 递归在子对象中查找
    private GameObject FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;

            GameObject found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // 列出所有子对象
    private void ListAllChildren()
    {
        Debug.Log($"=== {gameObject.name} 的所有子对象 ===");
        ListChildren(transform, 0);
    }

    // 递归列出所有子对象
    private void ListChildren(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        foreach (Transform child in parent)
        {
            if (child != transform) // 不显示自己
            {
                string activeStatus = child.gameObject.activeSelf ? "激活" : "非激活";
                Debug.Log($"{indent}- {child.name} ({activeStatus})");
                ListChildren(child, depth + 1);
            }
        }
    }
    private void OnDialogueEnd()
    {
        Debug.Log("对话结束事件被触发");

        // 这里可以添加你需要的逻辑，例如：
        // - 激活游戏对象
        // - 开始新任务
        // - 播放音效
        // - 等等

        // 示例：激活一个游戏对象
        GameObject endTrigger = GameObject.Find("DialogueEndTrigger");
        if (endTrigger != null)
        {
            endTrigger.SetActive(true);
        }

        // 示例：发送消息给其他对象
        GameObject[] targets = GameObject.FindGameObjectsWithTag("DialogueTarget");
        foreach (GameObject target in targets)
        {
            target.SendMessage("OnDialogueEnd", SendMessageOptions.DontRequireReceiver);
        }
    }

    // 开始战斗事件
    private void StartBattle()
    {
        Debug.Log("开始战斗事件");
        // 调用你的战斗系统
    }

    // 打开商店事件
    private void OpenShop()
    {
        Debug.Log("打开商店事件");
        // 调用你的商店系统
    }

    // 切换场景事件
    private void ChangeScene(string sceneName)
    {
        Debug.Log($"切换场景到: {sceneName}");
        // 调用场景切换
        // SceneManager.LoadScene(sceneName);
    }
    // 治疗效果
    private void HandleHealEffect(string[] parameters)
    {
        if (parameters.Length < 2) return;

        string target = parameters[0].Trim();
        if (float.TryParse(parameters[1], out float healAmount))
        {
            if (target == "all")
            {
                // 治疗所有玩家
                foreach (var unit in FindObjectsOfType<UnitController>())
                {
                    if (!unit.IsDead())
                    {
                        unit.Heal(healAmount);
                        Debug.Log($"治疗 {unit.characterName}: +{healAmount} HP");
                    }
                }
            }
            else
            {
                // 治疗特定玩家
                var unit = FindUnitByName(target);
                if (unit != null && !unit.IsDead())
                {
                    unit.Heal(healAmount);
                    Debug.Log($"治疗 {unit.characterName}: +{healAmount} HP");
                }
            }

            // 更新全局状态
            if (AllPlayerState.Instance != null)
            {
                AllPlayerState.Instance.UpdateUnitStates();
            }
        }
    }

    // 伤害效果
    private void HandleDamageEffect(string[] parameters)
    {
        if (parameters.Length < 2) return;

        string target = parameters[0].Trim();
        if (float.TryParse(parameters[1], out float damageAmount))
        {
            if (target == "all")
            {
                // 伤害所有玩家
                foreach (var unit in FindObjectsOfType<UnitController>())
                {
                    if (!unit.IsDead())
                    {
                        unit.TakeDamage(damageAmount);
                        Debug.Log($"对 {unit.characterName} 造成伤害: -{damageAmount} HP");
                    }
                }
            }
            else
            {
                // 伤害特定玩家
                var unit = FindUnitByName(target);
                if (unit != null && !unit.IsDead())
                {
                    unit.TakeDamage(damageAmount);
                    Debug.Log($"对 {unit.characterName} 造成伤害: -{damageAmount} HP");
                }
            }

            // 更新全局状态
            if (AllPlayerState.Instance != null)
            {
                AllPlayerState.Instance.UpdateUnitStates();
            }
        }
    }

    // 金币效果
    private void HandleCoinEffect(string[] parameters)
    {
        if (parameters.Length < 2) return;

        string operation = parameters[0].Trim();
        if (int.TryParse(parameters[1], out int coinAmount))
        {
            if (CollectionManager.instance != null)
            {
                switch (operation.ToLower())
                {
                    case "add":
                        CollectionManager.instance.AddCoin(coinAmount);
                        Debug.Log($"获得金币: +{coinAmount}");
                        break;
                    case "remove":
                        CollectionManager.instance.AddCoin(-coinAmount);
                        Debug.Log($"失去金币: -{coinAmount}");
                        break;
                    case "set":
                        // 如果需要设置金币，可能需要扩展 CollectionManager
                        Debug.LogWarning("设置金币功能需要扩展 CollectionManager");
                        break;
                }
            }
        }
    }

    // 最大血量效果
    private void HandleMaxHealthEffect(string[] parameters)
    {
        if (parameters.Length < 2) return;

        string target = parameters[0].Trim();
        if (float.TryParse(parameters[1], out float maxHealthChange))
        {
            if (target == "all")
            {
                // 修改所有玩家最大血量
                foreach (var unit in FindObjectsOfType<UnitController>())
                {
                    unit.maxHealth += maxHealthChange;
                    unit.currentHealth = Mathf.Min(unit.currentHealth, unit.maxHealth);
                    Debug.Log($"修改 {unit.characterName} 最大血量: {unit.maxHealth}");
                }
            }
            else
            {
                // 修改特定玩家最大血量
                var unit = FindUnitByName(target);
                if (unit != null)
                {
                    unit.maxHealth += maxHealthChange;
                    unit.currentHealth = Mathf.Min(unit.currentHealth, unit.maxHealth);
                    Debug.Log($"修改 {unit.characterName} 最大血量: {unit.maxHealth}");
                }
            }

            // 更新全局状态
            if (AllPlayerState.Instance != null)
            {
                AllPlayerState.Instance.UpdateUnitStates();
            }
        }
    }

    // 设置血量效果
    private void HandleSetHealthEffect(string[] parameters)
    {
        if (parameters.Length < 2) return;

        string target = parameters[0].Trim();
        if (float.TryParse(parameters[1], out float healthValue))
        {
            if (target == "all")
            {
                // 设置所有玩家血量
                foreach (var unit in FindObjectsOfType<UnitController>())
                {
                    if (!unit.IsDead())
                    {
                        unit.currentHealth = Mathf.Clamp(healthValue, 1, unit.maxHealth);
                        Debug.Log($"设置 {unit.characterName} 血量: {unit.currentHealth}");
                    }
                }
            }
            else
            {
                // 设置特定玩家血量
                var unit = FindUnitByName(target);
                if (unit != null && !unit.IsDead())
                {
                    unit.currentHealth = Mathf.Clamp(healthValue, 1, unit.maxHealth);
                    Debug.Log($"设置 {unit.characterName} 血量: {unit.currentHealth}");
                }
            }

            // 更新全局状态
            if (AllPlayerState.Instance != null)
            {
                AllPlayerState.Instance.UpdateUnitStates();
            }
        }
    }

    // 根据名字查找单位
    private UnitController FindUnitByName(string unitName)
    {
        UnitController[] units = FindObjectsOfType<UnitController>();
        foreach (var unit in units)
        {
            if (unit.characterName == unitName || unit.gameObject.name == unitName)
            {
                return unit;
            }
        }
        return null;
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

        // 规范换行
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // 检测分隔符
        int firstNewline = text.IndexOf('\n');
        string headerLine = firstNewline >= 0 ? text.Substring(0, firstNewline) : text;
        char delimiter = headerLine.Contains("\t") ? '\t' : ',';

        List<string> rows = SplitCsvRows(text);

        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("未解析出任何行。");
            return;
        }

        // 跳过表头
        for (int i = 1; i < rows.Count; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrEmpty(row)) continue;

            List<string> cells = ParseCsvLine(row);
            if (cells.Count < 6)
            {
                Debug.LogWarning($"第 {i + 1} 行列数不足：{cells.Count} -> \"{row}\"");
                continue;
            }

            Dialogue d = new Dialogue();
            d.Type = cells[0].Trim();

            // 解析基础字段
            int id; if (!int.TryParse(cells[1].Trim(), out id)) id = -1;
            d.ID = id;
            d.Speaker = cells[2].Trim();
            d.IMG = cells[3].Trim();
            d.Position = cells[4].Trim();

            int nextid; if (!int.TryParse(cells[5].Trim(), out nextid)) nextid = -1;
            d.NextID = nextid;

            d.Text = cells[6].Replace("\\n", "\n");
            d.CG = cells.Count > 7 ? cells[7].Trim() : "";

            // 解析 IsWatched
            bool isWatched;
            if (cells.Count > 8 && bool.TryParse(cells[8].Trim(), out isWatched))
                d.IsWatched = isWatched;
            else
                d.IsWatched = false;

            d.Condition = cells.Count > 9 ? cells[9].Trim() : "";

            // 关键修改：解析 RandomBranches
            d.RandomBranchesData = cells.Count > 10 ? cells[10].Trim() : "";

            // 解析 Effects
            d.Effects = cells.Count > 11 ? cells[11].Trim() : "";

            dialogues.Add(d);
        }

        // 解析随机分支数据
        ProcessRandomBranches();

        Debug.Log($"对话加载完成，总计 {dialogues.Count} 条对话");
    }
    private void ProcessRandomBranches()
    {
        foreach (var dialogue in dialogues)
        {
            // 清空现有分支
            dialogue.randomBranches.Clear();

            // 解析随机分支数据
            if (!string.IsNullOrEmpty(dialogue.RandomBranchesData))
            {
                ParseRandomBranches(dialogue);
            }
        }
    }

    private void ParseRandomBranches(Dialogue dialogue)
    {
        // 格式: nextID1:probability1;nextID2:probability2;...
        string[] branchStrings = dialogue.RandomBranchesData.Split(';');

        foreach (string branchString in branchStrings)
        {
            if (string.IsNullOrEmpty(branchString.Trim())) continue;

            string[] parts = branchString.Split(':');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int nextID) && int.TryParse(parts[1], out int probability))
                {
                    string condition = parts.Length > 2 ? parts[2] : "";
                    dialogue.randomBranches.Add(new RandomBranch(nextID, probability, condition));
                    Debug.Log($"添加随机分支: ID={nextID}, 概率={probability}%, 条件={condition}");
                }
                else
                {
                    Debug.LogWarning($"无法解析随机分支: {branchString}");
                }
            }
        }

        Debug.Log($"对话 {dialogue.ID} 解析到 {dialogue.randomBranches.Count} 个随机分支");
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
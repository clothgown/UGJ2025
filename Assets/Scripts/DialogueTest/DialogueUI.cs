using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI组件引用")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI contentText;
    public Image portraitLeft;
    public Image portraitRight;
    public Image cgImage;
    public GameObject branchPanel;
    public Transform branchButtonContainer;
    public Button continueButton;
    public Button skipButton;
    public GameObject backgroundPanel;

    [Header("UI预设")]
    public GameObject branchButtonPrefab;

    [Header("动画设置")]
    public float fadeDuration = 0.3f;
    public float typewriterSpeed = 0.05f;
    public bool enableAnimations = true;

    [Header("音效设置")]
    public AudioClip typewriterSound;
    public AudioClip dialogueOpenSound;
    public AudioClip dialogueCloseSound;
    public AudioClip branchSelectSound;

    [Header("引用")]
    public DialogueManager dialogueManager;

    // 私有变量
    private CanvasGroup _dialogueCanvasGroup;
    private Coroutine _typewriterCoroutine;
    private Coroutine _fadeCoroutine;
    private AudioSource _audioSource;
    private Dictionary<string, Sprite> _portraitCache = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _cgCache = new Dictionary<string, Sprite>();

    // 当前显示状态
    private bool _isShowing = false;
    private DialogueNode _currentNode;

    void Start()
    {
        InitializeComponents();
        RegisterEvents();
        HideAllUI();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 获取或添加AudioSource
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        // 获取CanvasGroup
        _dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (_dialogueCanvasGroup == null)
        {
            _dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
        }

        // 设置按钮事件
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClick);
        }
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    private void RegisterEvents()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueStart.AddListener(OnDialogueStart);
            dialogueManager.OnDialogueNodeChange.AddListener(OnDialogueNodeChange);
            dialogueManager.OnDialogueContentUpdate.AddListener(OnDialogueContentUpdate);
            dialogueManager.OnDialogueEnd.AddListener(OnDialogueEnd);
            dialogueManager.OnBranchOptionsShow.AddListener(OnBranchOptionsShow);
        }
    }

    /// <summary>
    /// 隐藏所有UI元素
    /// </summary>
    private void HideAllUI()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (branchPanel != null) branchPanel.SetActive(false);
        if (cgImage != null) cgImage.gameObject.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);

        portraitLeft.gameObject.SetActive(false);
        portraitRight.gameObject.SetActive(false);

        _isShowing = false;
    }

    /// <summary>
    /// 对话开始事件处理
    /// </summary>
    private void OnDialogueStart(DialogueNode node)
    {
        ShowDialogueUI();
        PlaySound(dialogueOpenSound);
        UpdateUIForNode(node);
    }

    /// <summary>
    /// 对话节点变化事件处理
    /// </summary>
    private void OnDialogueNodeChange(DialogueNode node)
    {
        _currentNode = node;
        UpdateUIForNode(node);

        // 重置继续按钮状态
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 对话内容更新事件处理
    /// </summary>
    private void OnDialogueContentUpdate(DialogueNode node)
    {
        // 停止之前的打字机效果
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }

        // 立即显示完整文本或开始打字机效果
        if (typewriterSpeed <= 0 || !enableAnimations)
        {
            contentText.text = node.GetFormattedContent();
            ShowContinueButton();
        }
        else
        {
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(node.GetFormattedContent()));
        }
    }

    /// <summary>
    /// 对话结束事件处理
    /// </summary>
    private void OnDialogueEnd()
    {
        PlaySound(dialogueCloseSound);
        HideDialogueUI();
    }

    /// <summary>
    /// 分支选项显示事件处理
    /// </summary>
    private void OnBranchOptionsShow(List<DialogueNode> branchNodes)
    {
        ShowBranchOptions(branchNodes);
    }

    /// <summary>
    /// 显示对话UI
    /// </summary>
    private void ShowDialogueUI()
    {
        if (_isShowing) return;

        _isShowing = true;

        if (enableAnimations && _dialogueCanvasGroup != null)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeInUI());
        }
        else
        {
            dialoguePanel.SetActive(true);
            if (backgroundPanel != null) backgroundPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏对话UI
    /// </summary>
    private void HideDialogueUI()
    {
        if (!_isShowing) return;

        _isShowing = false;

        if (enableAnimations && _dialogueCanvasGroup != null)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutUI());
        }
        else
        {
            dialoguePanel.SetActive(false);
            if (backgroundPanel != null) backgroundPanel.SetActive(false);
        }

        // 清理分支选项
        ClearBranchOptions();
    }

    /// <summary>
    /// 淡入UI效果
    /// </summary>
    private IEnumerator FadeInUI()
    {
        dialoguePanel.SetActive(true);
        if (backgroundPanel != null) backgroundPanel.SetActive(true);

        _dialogueCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _dialogueCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        _dialogueCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 淡出UI效果
    /// </summary>
    private IEnumerator FadeOutUI()
    {
        float elapsed = 0f;
        float startAlpha = _dialogueCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        _dialogueCanvasGroup.alpha = 0f;
        dialoguePanel.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);
    }

    /// <summary>
    /// 更新节点UI
    /// </summary>
    private void UpdateUIForNode(DialogueNode node)
    {
        if (node == null) return;

        // 更新角色名
        characterNameText.text = node.GetCharacterDisplayName();

        // 更新立绘
        UpdatePortrait(node);

        // 更新CG
        UpdateCG(node);

        // 清空内容文本（等待打字机效果填充）
        contentText.text = "";
    }

    /// <summary>
    /// 更新立绘显示
    /// </summary>
    private void UpdatePortrait(DialogueNode node)
    {
        // 隐藏所有立绘
        portraitLeft.gameObject.SetActive(false);
        portraitRight.gameObject.SetActive(false);

        string portraitName = node.DisplayPortrait;
        string portraitPosition = node.DisplayPortraitPosition;

        if (string.IsNullOrEmpty(portraitName)) return;

        // 确定目标立绘
        Image targetPortrait = portraitPosition == "左" ? portraitLeft : portraitRight;

        // 加载立绘精灵
        Sprite portraitSprite = LoadPortrait(portraitName);
        if (portraitSprite != null)
        {
            targetPortrait.sprite = portraitSprite;
            targetPortrait.gameObject.SetActive(true);

            // 可以在这里添加立绘动画
            if (enableAnimations)
            {
                StartCoroutine(AnimatePortraitAppear(targetPortrait));
            }
        }
    }

    /// <summary>
    /// 更新CG显示
    /// </summary>
    private void UpdateCG(DialogueNode node)
    {
        if (cgImage == null) return;

        string cgName = node.DisplayCG;

        if (string.IsNullOrEmpty(cgName))
        {
            cgImage.gameObject.SetActive(false);
            return;
        }

        Sprite cgSprite = LoadCG(cgName);
        if (cgSprite != null)
        {
            cgImage.sprite = cgSprite;
            cgImage.gameObject.SetActive(true);

            if (enableAnimations)
            {
                StartCoroutine(AnimateCGAppear(cgImage));
            }
        }
        else
        {
            cgImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 加载立绘
    /// </summary>
    private Sprite LoadPortrait(string portraitName)
    {
        if (_portraitCache.ContainsKey(portraitName))
        {
            return _portraitCache[portraitName];
        }

        // 从Resources加载立绘
        string portraitPath = $"Portraits/{portraitName}";
        Sprite portraitSprite = Resources.Load<Sprite>(portraitPath);

        if (portraitSprite != null)
        {
            _portraitCache[portraitName] = portraitSprite;
        }
        else
        {
            Debug.LogWarning($"找不到立绘: {portraitPath}");
        }

        return portraitSprite;
    }

    /// <summary>
    /// 加载CG
    /// </summary>
    private Sprite LoadCG(string cgName)
    {
        if (_cgCache.ContainsKey(cgName))
        {
            return _cgCache[cgName];
        }

        // 从Resources加载CG
        string cgPath = $"CGs/{cgName}";
        Sprite cgSprite = Resources.Load<Sprite>(cgPath);

        if (cgSprite != null)
        {
            _cgCache[cgName] = cgSprite;
        }
        else
        {
            Debug.LogWarning($"找不到CG: {cgPath}");
        }

        return cgSprite;
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private IEnumerator TypewriterEffect(string fullText)
    {
        contentText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            contentText.text += fullText[i];

            // 播放打字音效（可选）
            if (typewriterSound != null && i % 2 == 0) // 每2个字符播放一次，避免太密集
            {
                PlaySound(typewriterSound, 0.3f);
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        ShowContinueButton();
    }

    /// <summary>
    /// 显示继续按钮
    /// </summary>
    private void ShowContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 显示分支选项
    /// </summary>
    private void ShowBranchOptions(List<DialogueNode> branchNodes)
    {
        if (branchPanel == null || branchButtonPrefab == null) return;

        ClearBranchOptions();

        branchPanel.SetActive(true);

        for (int i = 0; i < branchNodes.Count; i++)
        {
            DialogueNode branchNode = branchNodes[i];
            GameObject buttonObj = Instantiate(branchButtonPrefab, branchButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = branchNode.GetFormattedContent();
            }

            int branchIndex = i; // 避免闭包问题
            button.onClick.AddListener(() => OnBranchSelected(branchIndex));
        }

        // 分支面板动画
        if (enableAnimations)
        {
            StartCoroutine(AnimateBranchPanelAppear());
        }
    }

    /// <summary>
    /// 清理分支选项
    /// </summary>
    private void ClearBranchOptions()
    {
        if (branchButtonContainer == null) return;

        foreach (Transform child in branchButtonContainer)
        {
            Destroy(child.gameObject);
        }

        if (branchPanel != null)
        {
            branchPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 分支选择回调
    /// </summary>
    private void OnBranchSelected(int branchIndex)
    {
        PlaySound(branchSelectSound);
        dialogueManager.SelectBranch(branchIndex);
        ClearBranchOptions();
    }

    /// <summary>
    /// 继续按钮点击
    /// </summary>
    private void OnContinueButtonClick()
    {
        dialogueManager.ContinueDialogue();
    }

    /// <summary>
    /// 跳过按钮点击
    /// </summary>
    private void OnSkipButtonClick()
    {
        dialogueManager.SkipTextDisplay();
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// 立绘出现动画
    /// </summary>
    private IEnumerator AnimatePortraitAppear(Image portrait)
    {
        RectTransform rectTransform = portrait.GetComponent<RectTransform>();
        Vector3 originalScale = rectTransform.localScale;

        rectTransform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, elapsed / 0.3f);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    /// <summary>
    /// CG出现动画
    /// </summary>
    private IEnumerator AnimateCGAppear(Image cg)
    {
        CanvasGroup cgCanvasGroup = cg.GetComponent<CanvasGroup>();
        if (cgCanvasGroup == null)
        {
            cgCanvasGroup = cg.gameObject.AddComponent<CanvasGroup>();
        }

        cgCanvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            cgCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }

        cgCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 分支面板出现动画
    /// </summary>
    private IEnumerator AnimateBranchPanelAppear()
    {
        CanvasGroup branchCanvasGroup = branchPanel.GetComponent<CanvasGroup>();
        if (branchCanvasGroup == null)
        {
            branchCanvasGroup = branchPanel.AddComponent<CanvasGroup>();
        }

        branchCanvasGroup.alpha = 0f;
        branchPanel.transform.localScale = Vector3.one * 0.8f;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.3f;

            branchCanvasGroup.alpha = progress;
            branchPanel.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, progress);

            yield return null;
        }

        branchCanvasGroup.alpha = 1f;
        branchPanel.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 设置打字机速度
    /// </summary>
    public void SetTypewriterSpeed(float speed)
    {
        typewriterSpeed = speed;
    }

    /// <summary>
    /// 启用/禁用动画
    /// </summary>
    public void SetAnimationsEnabled(bool enabled)
    {
        enableAnimations = enabled;
    }

    /// <summary>
    /// 手动显示对话（用于调试）
    /// </summary>
    public void DebugShowDialogue(DialogueNode node)
    {
        ShowDialogueUI();
        UpdateUIForNode(node);
        contentText.text = node.GetFormattedContent();
        ShowContinueButton();
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    public void ClearCache()
    {
        _portraitCache.Clear();
        _cgCache.Clear();
        Resources.UnloadUnusedAssets();
    }

    void OnDestroy()
    {
        // 清理协程
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
    }
}
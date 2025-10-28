using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入 TMPro 命名空间
using System.Collections;

public class UIPositionController : MonoBehaviour
{
    [System.Serializable]
    public class UIElement
    {
        public string name;
        public RectTransform rectTransform;
        public Vector2 startPosition;
        public Vector2 personPosition;
        public Vector2 backPosition;
    }

    [Header("UI元素设置")]
    public UIElement elementA;
    public UIElement elementB;

    [Header("按钮设置")]
    public Button personButton;
    public Button backButton; // B组件中的返回按钮

    [Header("打字机效果设置")]
    public TextMeshProUGUI typewriterText; // 直接引用 TMP 文本组件
    [TextArea] // 让字符串在 Inspector 中可以多行编辑
    public string typewriterContent = "欢迎使用系统..."; // 这里仍然是字符串，用于存储要显示的内容
    public float typingSpeed = 0.05f;
    public float startTypingDelay = 0.5f;

    [Header("动画设置")]
    public float moveDuration = 0.8f;
    public Ease moveEase = Ease.OutCubic;

    private bool isInPersonMode = false;
    private Tween typewriterTween;

    void Start()
    {
        // 记录初始位置
        if (elementA.rectTransform != null)
        {
            elementA.startPosition = elementA.rectTransform.anchoredPosition;
        }

        if (elementB.rectTransform != null)
        {
            elementB.startPosition = elementB.rectTransform.anchoredPosition;
        }

        // 设置按钮点击事件
        if (personButton != null)
        {
            personButton.onClick.AddListener(OnPersonButtonClick);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClick);
        }

        // 初始化UI位置
        ResetToStartPositions();
    }

    // 人员按钮点击事件
    public void OnPersonButtonClick()
    {
        if (isInPersonMode) return;

        isInPersonMode = true;

        // 停止所有动画
        StopAllAnimations();

        // 播放进入动画
        PlayEnterAnimation();

        Debug.Log("切换到人员模式");
    }

    // 返回按钮点击事件
    public void OnBackButtonClick()
    {
        if (!isInPersonMode) return;

        isInPersonMode = false;

        // 停止所有动画
        StopAllAnimations();

        // 播放返回动画
        PlayExitAnimation();

        Debug.Log("返回到初始模式");
    }

    void PlayEnterAnimation()
    {
        // 移动A组件到人员位置
        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.DOAnchorPosX(elementA.personPosition.x, moveDuration)
                .SetEase(moveEase);
        }

        // 移动B组件到人员位置
        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.DOAnchorPosX(elementB.personPosition.x, moveDuration)
                .SetEase(moveEase);
        }

        // 延迟后开始打字机动画
        StartCoroutine(StartTypewriterAfterDelay());
    }

    void PlayExitAnimation()
    {
        // 停止打字机动画
        if (typewriterTween != null && typewriterTween.IsActive())
        {
            typewriterTween.Kill();
        }

        // 清空文本
        if (typewriterText != null)
        {
            typewriterText.text = "";
        }

        // 移动A组件到返回位置
        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.DOAnchorPosX(elementA.backPosition.x, moveDuration)
                .SetEase(moveEase);
        }

        // 移动B组件到返回位置
        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.DOAnchorPosX(elementB.backPosition.x, moveDuration)
                .SetEase(moveEase);
        }
    }

    IEnumerator StartTypewriterAfterDelay()
    {
        yield return new WaitForSeconds(startTypingDelay);

        if (typewriterText != null && !string.IsNullOrEmpty(typewriterContent))
        {
            PlayTypewriterAnimation();
        }
    }

    void PlayTypewriterAnimation()
    {
        // 清空文本
        typewriterText.text = "";

        // 使用 DOTween.To 实现 TMP 的打字机效果[citation:1]
        float duration = typewriterContent.Length * typingSpeed;
        typewriterTween = DOTween.To(
            () => "", // 起始值为空字符串
            value => typewriterText.text = value, // 每帧更新 TMP 文本
            typewriterContent, // 最终要显示的完整文本
            duration // 总持续时间
        ).SetEase(Ease.Linear);
    }

    // 停止所有动画
    public void StopAllAnimations()
    {
        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.DOKill();
        }

        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.DOKill();
        }

        if (typewriterTween != null && typewriterTween.IsActive())
        {
            typewriterTween.Kill();
        }
    }

    // 重置到初始位置
    public void ResetToStartPositions()
    {
        StopAllAnimations();

        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.anchoredPosition = elementA.startPosition;
        }

        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.anchoredPosition = elementB.startPosition;
        }

        if (typewriterText != null)
        {
            typewriterText.text = "";
        }

        isInPersonMode = false;
    }

    // 强制立即切换到人员模式
    public void ForcePersonMode()
    {
        StopAllAnimations();

        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.anchoredPosition = elementA.personPosition;
        }

        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.anchoredPosition = elementB.personPosition;
        }

        if (typewriterText != null && !string.IsNullOrEmpty(typewriterContent))
        {
            typewriterText.text = typewriterContent; // 直接设置完整文本
        }

        isInPersonMode = true;
    }

    // 强制立即切换到返回模式
    public void ForceBackMode()
    {
        StopAllAnimations();

        if (elementA.rectTransform != null)
        {
            elementA.rectTransform.anchoredPosition = elementA.backPosition;
        }

        if (elementB.rectTransform != null)
        {
            elementB.rectTransform.anchoredPosition = elementB.backPosition;
        }

        if (typewriterText != null)
        {
            typewriterText.text = "";
        }

        isInPersonMode = false;
    }

    // 设置打字机内容
    public void SetTypewriterContent(string newContent)
    {
        typewriterContent = newContent;
    }

    // 编辑器调试方法
    [ContextMenu("测试人员模式")]
    public void TestPersonMode()
    {
        OnPersonButtonClick();
    }

    [ContextMenu("测试返回模式")]
    public void TestBackMode()
    {
        OnBackButtonClick();
    }

    [ContextMenu("重置位置")]
    public void DebugResetPositions()
    {
        ResetToStartPositions();
    }

    [ContextMenu("强制人员模式")]
    public void DebugForcePersonMode()
    {
        ForcePersonMode();
    }

    [ContextMenu("强制返回模式")]
    public void DebugForceBackMode()
    {
        ForceBackMode();
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }
}
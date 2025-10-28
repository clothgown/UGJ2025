using UnityEngine;
using DG.Tweening;

public class CanvasFadeIn : MonoBehaviour
{
    [Header("淡入设置")]
    public CanvasGroup canvasGroup;   // 拖入 CanvasGroup 组件
    public float duration = 1f;       // 淡入时间（秒）
    public float delay = 0f;          // 可选延迟时间

    void Start()
    {
        // 如果没手动指定，就自动获取
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // 初始化透明
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 开始淡入
        FadeIn();
    }

    public void FadeIn()
    {
        // 确保先停止之前的动画
        canvasGroup.DOKill();

        // 执行淡入动画
        canvasGroup
            .DOFade(1f, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .OnStart(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            })
            .OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
    }
}

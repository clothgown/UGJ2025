using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("引用对象")]
    [Tooltip("卡牌详情框对象（如 card_preview）")]
    public CanvasGroup previewGroup; // 拖入 card_preview 上的 CanvasGroup

    [Header("动画参数")]
    public float fadeDuration = 0.3f;       // 详情框淡入淡出时间
    public float scaleUpFactor = 1.1f;      // 卡牌放大比例
    public float scaleDuration = 0.25f;     // 卡牌放大时间

    private Vector3 originalScale;          // 记录原始大小
    private Tween scaleTween;               // 存放缩放动画引用
    private Tween fadeTween;                // 存放淡入淡出动画引用

    private void Start()
    {
        originalScale = transform.localScale;

        // 初始化详情框为隐藏
        if (previewGroup != null)
        {
            previewGroup.alpha = 0;
            previewGroup.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Q弹放大效果
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * scaleUpFactor, scaleDuration)
                              .SetEase(Ease.OutBack);

        // 淡入显示详情框
        if (previewGroup != null)
        {
            fadeTween?.Kill();
            previewGroup.gameObject.SetActive(true);
            fadeTween = previewGroup.DOFade(1, fadeDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 回到原始大小
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, scaleDuration)
                              .SetEase(Ease.InBack);

        // 淡出隐藏详情框
        if (previewGroup != null)
        {
            fadeTween?.Kill();
            fadeTween = previewGroup.DOFade(0, fadeDuration)
                                     .OnComplete(() => previewGroup.gameObject.SetActive(false));
        }
    }
}

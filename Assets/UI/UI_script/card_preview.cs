using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

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
    private Transform originalParent;       // 原始父物体
    private int originalSiblingIndex;       // 原始层级顺序
    private Canvas cardCanvas;              // 独立Canvas用于顶层显示

    private Tween scaleTween;
    private Tween fadeTween;

    public CardData cardData;
    public bool isItemCard = false;
    public ItemType itemType;

    public string cardName;
    public string description;
    private void Start()
    {
        originalScale = transform.localScale;

        if (previewGroup != null)
        {
            previewGroup.alpha = 0;
            previewGroup.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ------------------- 提升层级 -------------------
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        //  给这张卡加个独立Canvas，让它永远在最上层
        cardCanvas = gameObject.AddComponent<Canvas>();
        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 999; // 保证最顶层
        gameObject.AddComponent<GraphicRaycaster>(); // 保持可交互

        // ------------------- 动画部分 -------------------
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * scaleUpFactor, scaleDuration)
                              .SetEase(Ease.OutBack);

        if (previewGroup != null)
        {
            fadeTween?.Kill();
            previewGroup.gameObject.SetActive(true);
            fadeTween = previewGroup.DOFade(1, fadeDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ------------------- 动画 -------------------
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, scaleDuration)
                              .SetEase(Ease.InBack);

        if (previewGroup != null)
        {
            fadeTween?.Kill();
            fadeTween = previewGroup.DOFade(0, fadeDuration)
                                     .OnComplete(() => previewGroup.gameObject.SetActive(false));
        }

        // ------------------- 恢复层级 -------------------
        if (cardCanvas != null)
        {
            Destroy(cardCanvas.GetComponent<GraphicRaycaster>());
            Destroy(cardCanvas);
        }

        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
    }
}

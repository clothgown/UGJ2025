using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class NextTurnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;

    [Header("动画参数")]
    public float hoverScale = 1.1f;          // 悬浮放大比例
    public float clickScale = 0.9f;          // 点击缩放比例
    public float animDuration = 0.2f;        // 动画时长
    public float flyDistance = 300f;         // 点击后向右飞出距离
    public float flyDuration = 0.5f;         // 飞出动画时长
    public Color clickColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 点击变暗颜色
    public Ease scaleEase = Ease.OutBack;    // 放大/弹性缩放曲线
    public Ease flyEase = Ease.InOutQuad;    // 飞出曲线

    private void Start()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();

        if (buttonImage != null)
            originalColor = buttonImage.color;
    }

    // 鼠标悬浮时放大
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * hoverScale, animDuration).SetEase(scaleEase);
    }

    // 鼠标离开时恢复
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, animDuration).SetEase(scaleEase);
    }

    // 点击时缩小 → 颜色变深 → 弹起 → 向右弹走
    public void OnPointerClick(PointerEventData eventData)
    {
        Sequence seq = DOTween.Sequence();

        // 轻微点击反馈
        seq.Append(transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad));

        // 同时颜色变暗
        if (buttonImage != null)
            seq.Join(buttonImage.DOColor(clickColor, 0.1f));

        // 弹起回到悬浮状态
        seq.Append(transform.DOScale(originalScale * hoverScale, 0.2f).SetEase(Ease.OutBack));

        // 颜色恢复
        if (buttonImage != null)
            seq.Join(buttonImage.DOColor(originalColor, 0.2f));

        // 向右弹走
        seq.Append(transform.DOLocalMoveX(transform.localPosition.x + flyDistance, flyDuration)
            .SetEase(flyEase));
    }
}

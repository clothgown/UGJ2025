using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class CardHoverDragBasic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("动画参数")]
    public bool scaleAnimations = true;
    public float scaleOnHover = 1.15f;
    public float scaleOnDrag = 1.25f;
    public float scaleTransition = 0.15f;
    public Ease scaleEase = Ease.OutBack;

    [Header("Hover 动画")]
    public float hoverPunchAngle = 5f;
    public float hoverTransition = 0.15f;

    [Header("拖拽透明度变化")]
    public float dragAlpha = 0.75f;

    [Header("可选：高亮参数（无 IsoGrid2D 可忽略）")]
    public bool enableHighlight = false;
    public int highlightRange = 2;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private GameObject cardPreview;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;

        Transform previewTransform = transform.Find("card_assets_preview");
        if (previewTransform != null)
        {
            cardPreview = previewTransform.gameObject;
            cardPreview.SetActive(false);
        }
    }

    // ========= Hover =========
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(1);
        if (scaleAnimations)
            transform.DOScale(originalScale * scaleOnHover, scaleTransition).SetEase(scaleEase);

        transform.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1);

        if (cardPreview != null)
            cardPreview.SetActive(true);

        // 可选高亮（如果有 IsoGrid2D）
        if (enableHighlight && IsoGrid2D.instance != null)
        {
            var playerPos = TurnManager.instance.currentController.currentGridPos;
            IsoGrid2D.instance.HighlightArea(playerPos, highlightRange);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleAnimations)
            transform.DOScale(originalScale, scaleTransition).SetEase(scaleEase);

        if (cardPreview != null)
            cardPreview.SetActive(false);

        if (enableHighlight && IsoGrid2D.instance != null)
            IsoGrid2D.instance.ClearHighlight();
    }

    // ========= Drag =========
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scaleAnimations)
            transform.DOScale(originalScale * scaleOnDrag, scaleTransition).SetEase(scaleEase);

        canvasGroup.alpha = dragAlpha;
        canvas.overrideSorting = true;

        if (enableHighlight && IsoGrid2D.instance != null)
        {
            var playerPos = TurnManager.instance.currentController.currentGridPos;
            IsoGrid2D.instance.HighlightArea(playerPos, highlightRange);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scaleAnimations)
            transform.DOScale(originalScale, scaleTransition).SetEase(scaleEase);

        canvasGroup.alpha = 1f;
        canvas.overrideSorting = false;

        if (enableHighlight && IsoGrid2D.instance != null)
            IsoGrid2D.instance.ClearHighlight();
    }
}

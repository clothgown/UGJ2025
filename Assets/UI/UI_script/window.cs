using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class Window : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{

    private Tween currentTween;

    private Vector3 originalScale;

    [Header("动画参数")]
    public float hoverScale = 1.1f;     // 悬浮时放大比例
    public float clickScale = 0.9f;     // 点击瞬间缩小比例
    public float animDuration = 0.2f;   // 动画时长
    public Ease animEase = Ease.OutBack; // 缩放曲线（可以换成 Ease.OutElastic）
    public CardData cardToSell;
    public Image cardImage;
    public bool canInteract = true;
    private void Awake()
    {
        if(cardImage!=null && cardToSell!=null)
        {
            cardImage.sprite = cardToSell.cardSprite;
        }
    }
    void Start()
    {
        originalScale = transform.localScale;
    }
    private void Update()//每一帧  
    {
        
    }

    private void FixedUpdate()//每一固定频率帧
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canInteract == false) return;
        PlayScaleAnim(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (canInteract == false) return;
        PlayScaleAnim(originalScale);
    }

    public void Click()
    {
        if (canInteract == false) return;
        // 点击动画：快速缩小后恢复到悬浮状态
        if (currentTween != null) currentTween.Kill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale * hoverScale, 0.2f).SetEase(Ease.OutBack));
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (canInteract == false) return;
        // 点击动画：快速缩小后恢复到悬浮状态
        if (currentTween != null) currentTween.Kill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale * hoverScale, 0.2f).SetEase(Ease.OutBack));
    }

    private void PlayScaleAnim(Vector3 target)
    {
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(target, animDuration).SetEase(animEase);
    }
}

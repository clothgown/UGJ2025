using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

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
    public bool isInstore = false;

    private void Awake()
    {
        if (cardImage != null && cardToSell != null)
        {
            cardImage.sprite = cardToSell.cardSprite;
        }
    }

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canInteract) return;
        PlayScaleAnim(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!canInteract) return;
        PlayScaleAnim(originalScale);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canInteract) return;

        // 点击动画
        if (currentTween != null) currentTween.Kill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale * hoverScale, 0.2f).SetEase(Ease.OutBack));

        if (isInstore)
        {
            Store store = FindAnyObjectByType<Store>();
            store.currentWindow = this;
            if (store != null && store.detailPanel != null)
            {
                store.ShowDetailPanel();

                // ① 找到 detailPanel 下名为 "Cost" 的 TMP Text
                Transform costTransform = FindDeepChild(store.detailPanel.transform, "cost");
                // ② 找到当前对象下名为 "Value" 的 TMP Text
                Transform valueTransform = FindDeepChild(transform, "Value");

                if (costTransform != null && valueTransform != null)
                {
                    TextMeshProUGUI costText = costTransform.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI valueText = valueTransform.GetComponent<TextMeshProUGUI>();
                    if (costText != null && valueText != null)
                    {
                        costText.text = valueText.text;
                    }
                }

                // ③ 找到 detailPanel 下名为 "Card" 的对象
                Transform cardParent = store.detailPanel.transform.Find("Card");
                if (cardParent != null)
                {
                    // 清空原有子物体
                    foreach (Transform child in cardParent)
                    {
                        GameObject.Destroy(child.gameObject);
                    }

                    // ④ 找到自己下的 "goods" 子物件
                    Transform goods = transform.Find("goods");
                    if (goods != null)
                    {
                        // ⑤ 生成它的克隆到 detailPanel 的 Card 下
                        GameObject newGoods = Instantiate(goods.gameObject, cardParent);
                        newGoods.transform.localPosition = Vector3.zero;
                        newGoods.transform.localScale = Vector3.one;
                    }
                }
            }
        }
    }

    private void PlayScaleAnim(Vector3 target)
    {
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(target, animDuration).SetEase(animEase);
    }


    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}

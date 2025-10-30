using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CradInStore : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("悬停动画参数")]
    public float hoverScale = 1.1f;      // 鼠标悬停时的缩放比例
    public float hoverDuration = 0.2f;   // 动画持续时间

    private Vector3 originalScale;        // 原始缩放
    private Tween currentTween;           // 当前动画
    private GameObject cardAssetsPreview; // 预览图对象
    public string nname;
    public string descript;
    public CardData cardData;

    void Start()
    {
        originalScale = transform.localScale;

        // 自动查找子物体名为 "card_assets_preview" 的对象
        Transform previewTransform = transform.Find("card_assets_preview  ");
        if (previewTransform != null)
        {
            cardAssetsPreview = previewTransform.gameObject;
            cardAssetsPreview.SetActive(false); // 初始隐藏
        }
    }

    // 鼠标移入
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 放大动画
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(originalScale * hoverScale, hoverDuration).SetEase(Ease.OutBack);

        // 显示预览
        if (cardAssetsPreview != null)
            cardAssetsPreview.SetActive(true);
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData)
    {
        // 缩回动画
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(originalScale, hoverDuration).SetEase(Ease.OutBack);

        // 隐藏预览
        if (cardAssetsPreview != null)
            cardAssetsPreview.SetActive(false);
    }



}
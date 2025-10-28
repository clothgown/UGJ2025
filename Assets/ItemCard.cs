using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // 用于检测鼠标事件
using DG.Tweening; // DOTween命名空间

public enum ItemType
{
    locate,
    god,
    fortune
}
public class ItemCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("动画参数")]
    public float hoverScale = 1.1f; // 鼠标悬停缩放比例
    public float hoverDuration = 0.2f; // 动画持续时间
    public float clickScale = 0.9f; // 点击缩放比例
    public float clickDuration = 0.1f; // 点击动画时间
    public float dragReturnDuration = 0.3f; // 拖拽返回动画时间
    public float maxDragRotation = 15f; // 拖拽时最大旋转角度
    public float dragRotationFactor = 0.05f; // 旋转因子，根据偏移计算

    private Vector3 originalScale; // 原始缩放
    private Vector2 originalPosition; // 原始位置（针对UI）
    private Quaternion originalRotation; // 原始旋转
    private Tween currentTween; // 当前动画，用于停止

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup; // 用于控制拖拽时是否阻挡射线

    private GameObject cardAssetsPreview;
    public int godRange = 8;
    public ItemType itemType;
    public List<GameObject> godTargets;

    public int locateRange = 8;
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>(); // 确保有CanvasGroup

        originalScale = transform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = transform.localRotation;

        Transform previewTransform = transform.Find("card_assets_preview  ");
        if (previewTransform != null)
        {
            cardAssetsPreview = previewTransform.gameObject;
            cardAssetsPreview.SetActive(false); // 默认隐藏
        }
    }

    // 鼠标悬停
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsoGrid2D.instance.isWaitingForGridClick) return;
        if (cardAssetsPreview != null)
            cardAssetsPreview.SetActive(true);
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(originalScale * hoverScale, hoverDuration).SetEase(Ease.OutBack);
        if (TurnManager.instance.currentController.isMoving == true) return;
        if (itemType == ItemType.fortune)
        {
            IsoGrid2D.instance.HighlightSingleTile(IsoGrid2D.instance.controller.GetComponent<UnitController>().currentGridPos);
        }
        else if(itemType == ItemType.god)
        {
            godTargets = IsoGrid2D.instance.GetAndHighlightUnitsInRange(IsoGrid2D.instance.controller.GetComponent<UnitController>().currentGridPos, godRange);
        }
        else if(itemType == ItemType.locate)
        {
            IsoGrid2D.instance.HighlightEditableTiles(IsoGrid2D.instance.controller.GetComponent<UnitController>().currentGridPos, locateRange);
        }
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        if (cardAssetsPreview != null)
            cardAssetsPreview.SetActive(false);
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOScale(originalScale, hoverDuration).SetEase(Ease.OutBack);
        IsoGrid2D.instance.controller.GetComponent<UnitController>().Move();
        godTargets.Clear();
    }

    // 鼠标点击
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsoGrid2D.instance.isWaitingForGridClick) return;
        if (currentTween != null) currentTween.Kill();
        currentTween = transform.DOPunchScale(Vector3.one * (hoverScale - clickScale), clickDuration, 1, 0.5f);
        // DOPunchScale 会产生一个弹性的缩放效果
        if (itemType == ItemType.fortune && IsoGrid2D.instance.isFortune != true)
        {
            IsoGrid2D.instance.isFortune = true;
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    // 渐隐完成后可以选择销毁物体或隐藏
                    gameObject.SetActive(false);
                });
            }
        }
        else if(itemType == ItemType.god)
        {
            float baseDamage = 30f; // 基础伤害
            foreach (var tileObj in godTargets)
            {

                float finalDamage = baseDamage;

                // 50% 概率触发幸运效果（双倍伤害）
                if (IsoGrid2D.instance.isFortune && Random.value < 0.5f)
                {
                    finalDamage *= 2;
                    Debug.Log("幸运触发！造成双倍伤害！");
                }

                // 检查单位类型
                UnitController ally = tileObj.GetComponentInChildren<UnitController>();
                EnemyUnit enemy = tileObj.GetComponentInChildren<EnemyUnit>();

                if (ally != null && ally != IsoGrid2D.instance.GetComponentInChildren<UnitController>())
                {
                    ally.TakeDamage(finalDamage);
                    Debug.Log($"玩家 {ally.name} 受到 {finalDamage} 点伤害");
                }

                if (enemy != null)
                {
                    enemy.TakeDamage(finalDamage);
                    Debug.Log($"敌人 {enemy.name} 受到 {finalDamage} 点伤害");
                }
            }

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }

        }
        else if (itemType == ItemType.locate)
        {
            IsoGrid2D.instance.isWaitingForGridClick = true;
            IsoGrid2D.instance.isLocate = true;
            IsoGrid2D.instance.locateCard = this;
        }

    }

    // 开始拖拽
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentTween != null) currentTween.Kill();
        canvasGroup.blocksRaycasts = false; // 允许拖拽时不阻挡其他事件
        // 可选：transform.SetAsLastSibling(); // 将对象置于UI层级最上
    }

    // 拖拽中
    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor; // 跟随鼠标移动，考虑Canvas缩放

            // 根据偏移计算旋转
            Vector2 delta = rectTransform.anchoredPosition - originalPosition;
            float rotationZ = Mathf.Clamp(-delta.x * dragRotationFactor, -maxDragRotation, maxDragRotation);
            transform.localRotation = Quaternion.Euler(0, 0, rotationZ);
        }
    }

    // 结束拖拽
    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentTween != null) currentTween.Kill();

        // 使用 Sequence 同时动画位置和旋转
        currentTween = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(originalPosition, dragReturnDuration).SetEase(Ease.OutBack))
            .Join(transform.DORotateQuaternion(originalRotation, dragReturnDuration).SetEase(Ease.OutBack));

        canvasGroup.blocksRaycasts = true; // 恢复射线阻挡
    }
}
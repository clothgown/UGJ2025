using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BagSwitch : MonoBehaviour
{
    [Header("当前背包实例（运行中会被销毁）")]
    public GameObject currentBag;

    [Header("另一个背包 Prefab（拖入对应的）")]
    public GameObject nextBagPrefab;

    [Header("切换闪黑效果")]
    public float fadeDuration = 0.2f;
    public Color fadeColor = new Color(0, 0, 0, 1);

    private Button switchBtn;
    private Canvas rootCanvas;

    void Start()
    {
        switchBtn = GetComponent<Button>();
        if (switchBtn == null)
        {
            Debug.LogError("BagSwitch 脚本必须挂在一个 Button 上！");
            return;
        }

        rootCanvas = FindCanvas();
        switchBtn.onClick.AddListener(SwitchBag);
    }

    void SwitchBag()
    {
        if (currentBag == null || nextBagPrefab == null)
        {
            Debug.LogError("BagSwitch: currentBag 或 nextBagPrefab 未绑定！");
            return;
        }

        // 确保我们有可用的 Canvas
        if (rootCanvas == null)
        {
            rootCanvas = FindCanvas();
            if (rootCanvas == null)
            {
                Debug.LogError("BagSwitch: 无法找到可用的 Canvas！");
                return;
            }
        }

        // 创建临时黑幕
        GameObject fadeObj = new GameObject("FadeScreen");
        Image fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        fadeObj.transform.SetParent(rootCanvas.transform, false);
        fadeImg.rectTransform.anchorMin = Vector2.zero;
        fadeImg.rectTransform.anchorMax = Vector2.one;
        fadeImg.rectTransform.offsetMin = Vector2.zero;
        fadeImg.rectTransform.offsetMax = Vector2.zero;

        // 淡入 -> 切换 -> 淡出
        fadeImg.DOFade(1f, fadeDuration)
            .OnComplete(() =>
            {
                // 销毁旧背包
                if (currentBag != null)
                {
                    Destroy(currentBag);
                }

                // 实例化新背包
                GameObject nextBag = Instantiate(nextBagPrefab, rootCanvas.transform);
                nextBag.transform.SetAsLastSibling();

                // 更新引用，避免下次切换还指向旧的
                currentBag = nextBag;

                // 淡出黑幕
                fadeImg.DOFade(0f, fadeDuration).OnComplete(() => Destroy(fadeObj));
            });
    }

    private Canvas FindCanvas()
    {
        // 优先查找当前背包所在的 Canvas
        if (currentBag != null)
        {
            Canvas bagCanvas = currentBag.GetComponentInParent<Canvas>();
            if (bagCanvas != null)
            {
                return bagCanvas;
            }
        }

        // 其次查找当前按钮所在的 Canvas
        if (switchBtn != null)
        {
            Canvas buttonCanvas = switchBtn.GetComponentInParent<Canvas>();
            if (buttonCanvas != null)
            {
                return buttonCanvas;
            }
        }

        // 最后查找场景中的任何 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        // 如果确实没有找到任何 Canvas，才创建新的（作为备用方案）
        Debug.LogWarning("BagSwitch: 场景中未找到 Canvas，将创建新的 Canvas");
        GameObject go = new GameObject("Canvas", typeof(Canvas));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return go.GetComponent<Canvas>();
    }
}
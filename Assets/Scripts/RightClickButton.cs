using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickButton : MonoBehaviour, IPointerClickHandler
{
    [Header("右键设置")]
    public GameObject targetPanel; // 要打开的面板
    public bool togglePanel = true; // true:切换开关, false:只打开不关闭
    public bool closeOnClickOutside = true; // 点击面板外部关闭

    [Header("声音设置")]
    public AudioClip rightClickSound;

    private void Start()
    {
        // 确保面板初始状态正确
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }

        // 如果开启了点击外部关闭，添加全局点击检测
        if (closeOnClickOutside)
        {
            SetupClickOutsideDetection();
        }
    }

    // 处理鼠标点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查是否是右键点击
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
        }
    }

    private void HandleRightClick()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning("目标面板未设置！");
            return;
        }

        // 播放音效
        if (rightClickSound != null)
        {
            AudioSource.PlayClipAtPoint(rightClickSound, Camera.main.transform.position);
        }

        // 切换或打开面板
        if (togglePanel)
        {
            targetPanel.SetActive(!targetPanel.activeSelf);
        }
        else
        {
            targetPanel.SetActive(true);
        }

        Debug.Log($"右键点击按钮，面板状态: {targetPanel.activeSelf}");
    }

    // 设置点击面板外部关闭功能
    private void SetupClickOutsideDetection()
    {
        // 创建透明的全屏背景用于检测点击
        GameObject clickBlocker = new GameObject("ClickBlocker");
        clickBlocker.transform.SetParent(targetPanel.transform.parent);
        clickBlocker.transform.SetAsFirstSibling();

        RectTransform rectTransform = clickBlocker.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = clickBlocker.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.01f); // 几乎透明

        Button blockerButton = clickBlocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(() => {
            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }
            clickBlocker.SetActive(false);
        });

        clickBlocker.SetActive(false);

        // 当面板打开时显示点击拦截器
        targetPanel.SetActive(false); // 确保初始关闭
    }

    // 在Inspector中调用的方法
    [ContextMenu("手动触发右键点击")]
    public void ManualRightClick()
    {
        HandleRightClick();
    }

    // 公共方法，供其他脚本调用
    public void OpenPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    public bool IsPanelOpen()
    {
        return targetPanel != null && targetPanel.activeSelf;
    }
}
using UnityEngine;
using UnityEngine.UI;

public class OpenBagButton : MonoBehaviour
{
    [Header("背包 Prefab（拖入你的背包预制体）")]
    public GameObject bagPrefab;

    [Header("父级 Canvas 或 UI 根节点")]
    public Transform uiRoot; // 一般是 Canvas

    private GameObject currentBagInstance;

    void Start()
    {
        // 注册点击事件（如果这个物体上有 Button 组件）
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickOpenBag);
        }
    }

    public void OnClickOpenBag()
    {
        if (bagPrefab == null)
        {
            Debug.LogWarning("未设置 bagPrefab！");
            return;
        }

        // 如果背包已经存在，直接切换显示状态
        if (currentBagInstance != null)
        {
            bool isActive = currentBagInstance.activeSelf;
            currentBagInstance.SetActive(!isActive);
            return;
        }

        // 创建背包实例
        currentBagInstance = Instantiate(bagPrefab, uiRoot != null ? uiRoot : transform.parent);

        // 可选：调整层级或位置
        currentBagInstance.transform.SetAsLastSibling(); // 确保在最上层显示
    }
}


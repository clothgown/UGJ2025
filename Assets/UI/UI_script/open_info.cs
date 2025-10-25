using UnityEngine;
using UnityEngine.UI;

public class OpenDetailPanel : MonoBehaviour
{
    [Header("要生成的详情界面 Prefab")]
    public GameObject detailPrefab;

    [Header("父级 Canvas 或 UI 根节点（通常是 Canvas）")]
    public Transform uiRoot;

    [Header("目标显示层（从上往下数，第几层）")]
    [Tooltip("例如 5 表示放在倒数第5层")]
    public int targetSiblingFromTop = 5;

    private GameObject currentDetailInstance;

    void Start()
    {
        // 如果这个物体有 Button 组件，自动绑定点击事件
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickShowDetail);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 没有 Button 组件！");
        }
    }

    public void OnClickShowDetail()
    {
        if (detailPrefab == null)
        {
            Debug.LogWarning("未指定 detailPrefab！");
            return;
        }

        // 如果已经生成了，直接切换显示状态
        if (currentDetailInstance != null)
        {
            bool isActive = currentDetailInstance.activeSelf;
            currentDetailInstance.SetActive(!isActive);
            return;
        }

        // 创建详情实例
        Transform parent = uiRoot != null ? uiRoot : transform.parent;
        currentDetailInstance = Instantiate(detailPrefab, parent);

        // 放置到倒数第 targetSiblingFromTop 层
        int totalChildren = parent.childCount;
        int targetIndex = Mathf.Max(0, totalChildren - targetSiblingFromTop);
        currentDetailInstance.transform.SetSiblingIndex(targetIndex);

        Debug.Log($"详情界面生成完毕，当前层级索引：{targetIndex}/{totalChildren}");
    }
}

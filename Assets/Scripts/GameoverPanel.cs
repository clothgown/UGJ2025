using UnityEngine;

public class VictoryPanel : MonoBehaviour
{

    public string panelname;
    void OnEnable()
    {
        // 面板打开时播放特殊BGM
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSpecialPanelOpened(panelname);
        }

        // 其他面板初始化代码...
    }

    void OnDisable()
    {
        // 面板关闭时恢复原BGM
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSpecialPanelClosed(panelname);
        }
    }

    // 或者手动控制
    public void ShowVictory()
    {
        gameObject.SetActive(true);
        GameManager.Instance.OnSpecialPanelOpened(panelname);
    }

    public void HideVictory()
    {
        GameManager.Instance.OnSpecialPanelClosed(panelname);
        gameObject.SetActive(false);
    }
}
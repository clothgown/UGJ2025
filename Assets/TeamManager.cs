using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager : MonoBehaviour
{
    [Header("HeadUI 下的所有按钮")]
    public List<Button> headUIButtons = new List<Button>();

    void Start()
    {
        // 找到名为 "HeadUI" 的物体
        GameObject headUI = GameObject.Find("HeadUI");
        if (headUI == null)
        {
            Debug.LogWarning("未找到名为 'HeadUI' 的物体！");
            return;
        }

        // 获取 HeadUI 下所有 Button 组件（包含子物体）
        Button[] buttons = headUI.GetComponentsInChildren<Button>(true); // true 表示包含隐藏物体
        headUIButtons.AddRange(buttons);

        // 测试输出
        foreach (var btn in headUIButtons)
        {
            Debug.Log("找到按钮：" + btn.name);
        }
    }
}

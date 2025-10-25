using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupTrigger : MonoBehaviour
{
    [Header("场景中已有的二确弹窗对象（默认隐藏）")]
    public GameObject confirmPopup;

    private Button iconButton;

    void Start()
    {
        iconButton = GetComponent<Button>();
        if (iconButton == null)
        {
            Debug.LogError("?? ConfirmPopupTrigger 必须挂在 Button 上！");
            return;
        }

        if (confirmPopup == null)
        {
            Debug.LogError("?? confirmPopup 未绑定！");
            return;
        }

        // 点击按钮时显示弹窗
        iconButton.onClick.AddListener(ShowPopup);

        // 确保开始时隐藏
        confirmPopup.SetActive(false);
    }

    void ShowPopup()
    {
        confirmPopup.SetActive(true);

        // 可选：自动绑定弹窗按钮逻辑
        Button confirmBtn = confirmPopup.transform.Find("ConfirmButton")?.GetComponent<Button>();
        Button cancelBtn = confirmPopup.transform.Find("CancelButton")?.GetComponent<Button>();

        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                Debug.Log("? 点击确认");
                confirmPopup.SetActive(false);
                // TODO：在这里写确认逻辑
            });
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(() =>
            {
                Debug.Log("? 点击取消");
                confirmPopup.SetActive(false);
            });
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class CloseButton : MonoBehaviour
{
    public GameObject panelToClose; // 拖入要关闭的背包prefab

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(ClosePanel);
    }

    void ClosePanel()
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }
}

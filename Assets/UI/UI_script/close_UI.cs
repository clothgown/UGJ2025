using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CloseButton : MonoBehaviour
{
    [Header("要关闭的面板（拖入Prefab实例）")]
    public GameObject panelToClose;

    [Header("淡出时长（秒）")]
    public float fadeDuration = 0.1f;

    private Button button;
    private bool isClosing = false;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(ClosePanel);
    }

    void ClosePanel()
    {
        if (isClosing || panelToClose == null) return;

        CanvasGroup cg = panelToClose.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panelToClose.AddComponent<CanvasGroup>(); // 自动补上
        }

        isClosing = true;
        StartCoroutine(FadeOutAndClose(panelToClose, cg));
    }

    IEnumerator FadeOutAndClose(GameObject panel, CanvasGroup cg)
    {
        float time = 0f;
        Vector3 startScale = panel.transform.localScale;
        Vector3 endScale = startScale * 0.9f; // 缩小一点点

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            cg.alpha = Mathf.Lerp(1f, 0f, t);
            panel.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        cg.alpha = 0f;
        panel.transform.localScale = endScale;
        panel.SetActive(false);

        // 恢复状态，便于下次重新显示
        cg.alpha = 1f;
        panel.transform.localScale = startScale;
        isClosing = false;
    }
}

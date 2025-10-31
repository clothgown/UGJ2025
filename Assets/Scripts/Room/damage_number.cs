using UnityEngine;
using TMPro;
using System.Collections;

public class DamageNumber : MonoBehaviour
{
    [Header("动画参数")]
    public float moveUpDistance = 1.5f;    // 上升距离
    public float duration = 1.0f;          // 动画总时长
    public float fadeInTime = 0.15f;       // 淡入时间
    public float fadeOutTime = 0.3f;       // 淡出时间
    public float scalePunch = 1.3f;        // 出现时放大比例
    public Vector3 randomOffset = new Vector3(0.5f, 0f, 0f); // 初始偏移抖动

    private TextMeshPro textMesh;
    private Color startColor;
    private Vector3 originalLocalPos;
    private Transform originalParent;
    private Coroutine animCoroutine;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        startColor = textMesh.color;
        originalLocalPos = transform.localPosition;
        originalParent = transform.parent;
    }

    void OnEnable()
    {
        // 每次启用都执行动画
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        // 提升层级到最上
        transform.SetAsLastSibling();

        // 初始化位置和状态
        Vector3 startPos = originalLocalPos + new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z)
        );

        transform.localPosition = startPos;
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        transform.localScale = Vector3.one * scalePunch;

        float timer = 0f;

        // --- 1?? 淡入 + 缩放回正 ---
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInTime;
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, t);
            transform.localScale = Vector3.Lerp(Vector3.one * scalePunch, Vector3.one, t);
            yield return null;
        }

        // --- 2?? 上升 ---
        timer = 0f;
        Vector3 endPos = startPos + Vector3.up * moveUpDistance;
        while (timer < duration - fadeOutTime)
        {
            timer += Time.deltaTime;
            float t = timer / (duration - fadeOutTime);
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // --- 3?? 淡出 ---
        timer = 0f;
        Color c = startColor;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeOutTime;
            c.a = Mathf.Lerp(1f, 0f, t);
            textMesh.color = c;
            transform.localPosition = Vector3.Lerp(endPos, endPos + Vector3.up * 0.2f, t);
            yield return null;
        }

        // --- 4?? 恢复状态并隐藏 ---
        textMesh.color = startColor;
        transform.localScale = Vector3.one;
        transform.localPosition = originalLocalPos;
        gameObject.SetActive(false);
    }
}

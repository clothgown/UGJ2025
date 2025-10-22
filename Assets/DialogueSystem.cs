using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using DG.Tweening; // 记得引用DoTween命名空间
using UnityEngine.UI;

[Serializable]
public class Dialogue
{
    public string Type;        // Type列，如 Start, #, @, E ...
    public int ID;
    public string Speaker;     // 说话者
    public string IMG;         // 表情（如 sad、jingya、normal）
    public string Position;    // 位置（左/右）
    public int NextID;
    public string Text;        // 对话文本
    public string CG;          // CG名（可选）
    public bool IsWatched;     // 是否已观看
}
public class DialogueSystem : MonoBehaviour
{
    public bool isDialoguing;
    public TextAsset dialogDataFile; // 指向你的CSV/TSV文件
    public List<Dialogue> dialogues = new List<Dialogue>();
    public TMP_Text nameTMP;     // 显示角色名字
    public TMP_Text dialogTMP;   // 显示对话内容

    private int currentID = 0;   // 当前对话ID（从0或Start开始）
    private bool isLoaded = false;

    public int currentIndex = 0; // 当前对话索引
    private Tween typingTween;    // 保存打字机Tween

    public float fadeDuration = 0.5f; // 立绘淡入时间
    public float speakTime = 1.5f;

    public DialogueCharacterManager characterManager;
    public Image leftImage;
    public Image rightImage;

    public Image cgImage;  // Inspector 指定 UI 背景 Image
    public float cgFadeDuration = 0.5f; // CG 淡入淡出时间
    public CanvasGroup UIGroup;

    public TextAsset battleDialogDataFile; // 指向你的CSV/TSV文件
    void Start()
    {
        UIGroup.gameObject.SetActive(true);
        UIGroup.alpha = 1f;
        // 初始化CG
        if (cgImage != null)
        {
            cgImage.gameObject.SetActive(true);
            cgImage.color = new Color(1, 1, 1, 0); // 透明
        }

        // 左右立绘初始化
        if (leftImage != null)
        {
            leftImage.gameObject.SetActive(true);
            leftImage.color = new Color(1, 1, 1, 0);
        }
        if (rightImage != null)
        {
            rightImage.gameObject.SetActive(true);
            rightImage.color = new Color(1, 1, 1, 0);
        }

        LoadDialogues(dialogDataFile.text);
        isLoaded = true;

        ShowDialogue(currentIndex);
    }
    void Update()
    
    {
        // 点击鼠标左键（或触屏）时跳下一句
        if (isLoaded && Input.GetMouseButtonDown(0))
        {
            NextDialogue();
        }
    }

    public void StartNewDialogue()
    {
        currentIndex = 0;
        LoadDialogues(battleDialogDataFile.text);
        isLoaded = true;

        if (UIGroup != null)
        {
            UIGroup.gameObject.SetActive(true);
            UIGroup.DOFade(1f, fadeDuration).OnComplete(() => {

            });
        }
        ShowDialogue(currentIndex);
        isDialoguing = true;
    }


    void ShowDialogue(int index)
    {
        Dialogue d = dialogues[index];
        nameTMP.text = d.Speaker;
        dialogTMP.text = "";

        // 立绘
        Sprite s = characterManager.GetPortrait(d.Speaker, d.IMG);
        if (s != null)
        {
            if (d.Position == "左" && leftImage != null)
            {
                leftImage.sprite = s;
                leftImage.DOFade(1f, fadeDuration);
            }
            else if (d.Position == "右" && rightImage != null)
            {
                rightImage.sprite = s;
                rightImage.DOFade(1f, fadeDuration);
            }
        }

        // CG处理
        if (cgImage != null)
        {
            if (!string.IsNullOrEmpty(d.CG))
            {
                // 有CG → 渐显
                Sprite cgSprite = characterManager.GetCG(d.CG);
                if (cgSprite != null) cgImage.sprite = cgSprite;
                cgImage.DOKill();
                cgImage.gameObject.SetActive(true);
                cgImage.DOFade(1f, cgFadeDuration);
            }
            else
            {
                // 没CG → 渐隐
                cgImage.DOKill();
                cgImage.DOFade(0f, cgFadeDuration);
                cgImage.gameObject.SetActive(false);
            }
        }

        // 打字机效果
        typingTween?.Kill();
        typingTween = dialogTMP.DOText(d.Text, speakTime).SetEase(Ease.Linear);
    }

    void NextDialogue()
    {
        // 如果当前打字还没结束，直接跳完本句
        if (typingTween != null && typingTween.IsActive() && typingTween.IsPlaying())
        {
            typingTween.Complete();
            return;
        }

        // 否则跳到下一句
        int nextIndex = currentIndex + 1;
        if (nextIndex < dialogues.Count)
        {
            ShowDialogue(nextIndex);
            currentIndex = nextIndex;
        }
        else
        {
            Debug.Log("对话结束。");
            FadeOutUI();
            isDialoguing = false;
        }
    }
    void FadeOutUI()
    {
        if (UIGroup != null)
        {
            UIGroup.DOFade(0f, fadeDuration).OnComplete(() => {
                cgImage.gameObject.SetActive(false);
                // 渐隐完成后可以禁用UI组或执行其他操作
                UIGroup.gameObject.SetActive(false);
            });
        }
    }


    void OnClickNext()
    {
        ShowDialogue(currentID);
    }
    /// <summary>
    /// 加载并解析对话文本（支持逗号或制表符分隔，支持引号内含分隔符）
    /// </summary>
    /// <param name="text">TextAsset.text</param>
    public void LoadDialogues(string text)
    {
        dialogues.Clear();
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("输入文本为空。");
            return;
        }

        // 规范换行（统一为 \n）
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // 先拿第一行（表头）判断分隔符（逗号或制表符）
        int firstNewline = text.IndexOf('\n');
        string headerLine = firstNewline >= 0 ? text.Substring(0, firstNewline) : text;
        char delimiter = headerLine.Contains("\t") ? '\t' : ','; // 简单检测：优先制表符

        // 我们需要按行遍历，但要注意可能有字段内的换行（被引号包裹）。
        // 所以不能简单 Split('\n') —— 改为手动逐字符解析，按 CSV 引号规则切行。
        List<string> rows = SplitCsvRows(text);

        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("未解析出任何行。");
            return;
        }

        // 跳过表头（假设第一行是 header）
        for (int i = 1; i < rows.Count; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrEmpty(row)) continue;

            List<string> cells = ParseCsvLine(row);
            // 我们期望至少 6 列（Type, ID, Speaker, Position, NextID, Text），第7列CG可选
            if (cells.Count < 6)
            {
                Debug.LogWarning($"第 {i + 1} 行列数不足：{cells.Count} -> \"{row}\"");
                continue;
            }

            Dialogue d = new Dialogue();
            d.Type = cells[0].Trim();
            int id; if (!int.TryParse(cells[1].Trim(), out id)) id = -1;
            d.ID = id;
            d.Speaker = cells[2].Trim();
            d.IMG = cells[3].Trim();
            d.Position = cells[4].Trim();
            int nextid; if (!int.TryParse(cells[5].Trim(), out nextid)) nextid = -1;
            d.NextID = nextid;
            d.Text = cells[6].Replace("\\n", "\n"); // 如果CSV里用 \n 表示换行
            d.CG = cells.Count > 7 ? cells[7].Trim() : "";
            d.IsWatched = false;

            dialogues.Add(d);
        }
    }

    /// <summary>
    /// 将整个文本拆为“逻辑行”，考虑引号内的换行
    /// </summary>
    List<string> SplitCsvRows(string text)
    {
        List<string> rows = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                // 引号遇到双引号转义时要跳过成对的双引号
                if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    cur.Append('"');
                    i++; // 跳过下一个双引号
                }
                else
                {
                    inQuotes = !inQuotes;
                    // 不把外层引号加入结果
                }
            }
            else if (c == '\n')
            {
                if (inQuotes)
                {
                    // 引号内部的换行应保留为字段内容的一部分
                    cur.Append('\n');
                }
                else
                {
                    rows.Add(cur.ToString());
                    cur.Length = 0;
                }
            }
            else
            {
                cur.Append(c);
            }
        }
        // 添加最后一行（如果有残余）
        if (cur.Length > 0)
        {
            rows.Add(cur.ToString());
        }
        return rows;
    }

    /// <summary>
    /// 解析单行 CSV，支持逗号/制表符与引号规则
    /// </summary>
    List<string> ParseCsvLine(string line)
    {
        List<string> cells = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;


        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 双引号转义 -> 添加一个引号到字段中
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && (c == ',' || c == '\t'))
            {
                // 逗号或制表符为分隔符（仅在非引号内有效）
                cells.Add(cur.ToString());
                cur.Length = 0;
            }
            else
            {
                cur.Append(c);
            }
        }
        // 最后一格
        cells.Add(cur.ToString());
        // Trim 每个单元格两侧的空白（但保留内部空格）
        for (int j = 0; j < cells.Count; j++)
        {
            cells[j] = cells[j].Trim();
        }
        return cells;
    }
}

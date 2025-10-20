using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueType
{
    Battle,     // 30000-dialogue-关卡内-战斗后
    Explore,    // 20000-dialogue-关卡内-探索  
    Normal,     // 0-dialogue-关卡内S&E
    Outside,    // 10000-dialogue-关卡外
    Dead        // 50000-dialogue-dead
}

[System.Serializable]
public class DialogueNode
{
    // ========== 所有表格共有的基础字段 ==========
    [Header("=== 基础信息 ===")]
    public int ID;
    public string Next;
    public string Flag;
    public string Level;
    public DialogueType TableType;

    // ========== 各表格特有字段 ==========

    // 战斗后对话表 (30000-dialogue-关卡内-战斗后)
    [Header("=== 战斗后对话字段 ===")]
    public int BattleRound;          // 第几轮战斗后
    public string BattleCondition;   // 条件
    public string BattleCharacter;   // 人物
    public string BattlePortrait;    // 立绘
    public string BattlePortraitPos; // 立绘位置
    public string BattleContent;     // 内容
    public string BattleCG;          // CG
    public bool BattleSeen;          // 看过吗

    // 探索对话表 (20000-dialogue-关卡内-探索)
    [Header("=== 探索对话字段 ===")]
    public string ExploreCharacter;  // 人物
    public string ExplorePortrait;   // 人物立绘
    public string ExplorePortraitPos;// 立绘位置
    public string ExploreCondition;  // 条件
    public string ExploreItem;       // 东西
    public string ExploreEffect;     // 效果
    public string ExploreContent;    // 内容
    public bool ExploreSeen;         // 看过吗
    public bool ExploreOnceOnly;     // 只能看一次

    // 关卡内S&E表 (0-dialogue-关卡内S&E)
    [Header("=== 关卡内S&E字段 ===")]
    public string NormalPosition;    // 位置
    public string NormalCondition;   // 条件
    public string NormalCharacter;   // 人物
    public string NormalPortrait;    // 立绘
    public string NormalPortraitPos; // 立绘位置
    public string NormalContent;     // 内容
    public string NormalCG;          // CG
    public bool NormalSeen;          // 看过吗

    // 关卡外表 (10000-dialogue-关卡外)
    [Header("=== 关卡外对话字段 ===")]
    public string OutsidePosition;   // 位置
    public string OutsideCondition;  // 条件
    public string OutsideCharacter;  // 人物
    public string OutsidePortrait;   // 立绘
    public string OutsidePortraitPos;// 立绘位置
    public string OutsideContent;    // 内容
    public string OutsideCG;         // CG
    public bool OutsideSeen;         // 看过吗

    // 死亡结局表 (50000-dialogue-dead)
    [Header("=== 死亡结局字段 ===")]
    public string DeadLevel;         // 关卡
    public string DeadCondition;     // 条件
    public string DeadTitle;         // 标题
    public string DeadContent;       // 死亡内容

    // ========== 计算属性（统一访问接口） ==========

    public string DisplayCharacter
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattleCharacter,
                DialogueType.Explore => ExploreCharacter,
                DialogueType.Normal => NormalCharacter,
                DialogueType.Outside => OutsideCharacter,
                _ => ""
            };
        }
    }

    public string DisplayPortrait
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattlePortrait,
                DialogueType.Explore => ExplorePortrait,
                DialogueType.Normal => NormalPortrait,
                DialogueType.Outside => OutsidePortrait,
                _ => ""
            };
        }
    }

    public string DisplayPortraitPosition
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattlePortraitPos,
                DialogueType.Explore => ExplorePortraitPos,
                DialogueType.Normal => NormalPortraitPos,
                DialogueType.Outside => OutsidePortraitPos,
                _ => "左" // 默认位置
            };
        }
    }

    public string DisplayContent
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattleContent,
                DialogueType.Explore => ExploreContent,
                DialogueType.Normal => NormalContent,
                DialogueType.Outside => OutsideContent,
                DialogueType.Dead => DeadContent,
                _ => ""
            };
        }
    }

    public string DisplayCG
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattleCG,
                DialogueType.Normal => NormalCG,
                DialogueType.Outside => OutsideCG,
                _ => ""
            };
        }
    }

    public string Condition
    {
        get
        {
            return TableType switch
            {
                DialogueType.Battle => BattleCondition,
                DialogueType.Explore => ExploreCondition,
                DialogueType.Normal => NormalCondition,
                DialogueType.Outside => OutsideCondition,
                DialogueType.Dead => DeadCondition,
                _ => ""
            };
        }
    }

    public string Position
    {
        get
        {
            return TableType switch
            {
                DialogueType.Normal => NormalPosition,
                DialogueType.Outside => OutsidePosition,
                _ => ""
            };
        }
    }

    // ========== 方法 ==========

    public DialogueNode(DialogueType tableType)
    {
        TableType = tableType;
    }

    /// <summary>
    /// 获取下一个节点ID数组
    /// </summary>
    public int[] GetNextIDs()
    {
        if (string.IsNullOrEmpty(Next) || Next == "E")
            return Array.Empty<int>();

        string[] parts = Next.Split('&');
        int[] results = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            int.TryParse(parts[i], out results[i]);
        }
        return results;
    }

    /// <summary>
    /// 检查是否有分支选项
    /// </summary>
    public bool HasBranch()
    {
        return Flag == "&" && Next.Contains("&");
    }

    /// <summary>
    /// 检查是否是结束节点
    /// </summary>
    public bool IsEndNode()
    {
        return Next == "E" || string.IsNullOrEmpty(Next);
    }

    /// <summary>
    /// 获取格式化后的内容（处理换行等）
    /// </summary>
    public string GetFormattedContent()
    {
        string content = DisplayContent;
        if (string.IsNullOrEmpty(content)) return "";

        // 处理换行符
        content = content.Replace("<br>", "\n");

        // 处理括号内容（心理活动等）
        content = content.Replace("（", "<i>（").Replace("）", "）</i>");

        return content;
    }

    /// <summary>
    /// 获取角色显示名称（中文）
    /// </summary>
    public string GetCharacterDisplayName()
    {
        string character = DisplayCharacter;
        return character switch
        {
            "Heart" => "红心",
            "Maid" => "女仆",
            "Diamond" => "方块",
            "Club" => "梅花",
            "Spade" => "黑桃",
            "Soldier" => "士兵",
            "Servant1" => "仆人甲",
            "Servant2" => "仆人乙",
            "Servant3" => "仆人丙",
            _ => character
        };
    }

    /// <summary>
    /// 检查是否满足条件
    /// </summary>
    public bool CheckCondition(Dictionary<string, object> gameState)
    {
        string condition = Condition;
        if (string.IsNullOrEmpty(condition)) return true;

        // 这里可以添加具体的条件检查逻辑
        // 例如：检查角色是否在队伍、血量条件、物品条件等
        // 暂时返回true，实际使用时需要根据游戏状态实现

        Debug.Log($"检查条件: {condition}");
        return true;
    }

    /// <summary>
    /// 应用节点效果
    /// </summary>
    public void ApplyEffects(ref Dictionary<string, object> gameState)
    {
        if (TableType == DialogueType.Explore && !string.IsNullOrEmpty(ExploreEffect))
        {
            // 解析并应用探索效果
            // 例如：money+-3, blood+a++5 等
            ParseAndApplyEffect(ExploreEffect, ref gameState);
        }
    }

    private void ParseAndApplyEffect(string effect, ref Dictionary<string, object> gameState)
    {
        // 简化的效果解析，实际需要更完善的实现
        if (effect.Contains("money"))
        {
            // 解析金钱变化
            Debug.Log($"应用金钱效果: {effect}");
        }
        else if (effect.Contains("blood"))
        {
            // 解析血量变化  
            Debug.Log($"应用血量效果: {effect}");
        }
    }

    /// <summary>
    /// 调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"ID: {ID}, Type: {TableType}, Character: {GetCharacterDisplayName()}, " +
               $"Content: {GetFormattedContent().Substring(0, Math.Min(30, GetFormattedContent().Length))}...";
    }

    /// <summary>
    /// 从字典数据创建节点（用于数据导入）
    /// </summary>
    public static DialogueNode CreateFromDictionary(Dictionary<string, string> data, DialogueType tableType)
    {
        DialogueNode node = new DialogueNode(tableType);

        try
        {
            // 通用字段
            if (data.ContainsKey("ID") && int.TryParse(data["ID"], out int id))
                node.ID = id;

            if (data.ContainsKey("跳转"))
                node.Next = data["跳转"];

            if (data.ContainsKey("标志"))
                node.Flag = data["标志"];

            if (data.ContainsKey("关卡"))
                node.Level = data["关卡"];

            // 根据表格类型设置特有字段
            switch (tableType)
            {
                case DialogueType.Battle:
                    SetBattleFields(node, data);
                    break;
                case DialogueType.Explore:
                    SetExploreFields(node, data);
                    break;
                case DialogueType.Normal:
                    SetNormalFields(node, data);
                    break;
                case DialogueType.Outside:
                    SetOutsideFields(node, data);
                    break;
                case DialogueType.Dead:
                    SetDeadFields(node, data);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"创建对话节点失败: {e.Message}");
            return null;
        }

        return node;
    }

    private static void SetBattleFields(DialogueNode node, Dictionary<string, string> data)
    {
        if (data.ContainsKey("第几轮战斗后") && int.TryParse(data["第几轮战斗后"], out int round))
            node.BattleRound = round;

        if (data.ContainsKey("条件")) node.BattleCondition = data["条件"];
        if (data.ContainsKey("人物")) node.BattleCharacter = data["人物"];
        if (data.ContainsKey("立绘")) node.BattlePortrait = data["立绘"];
        if (data.ContainsKey("立绘位置")) node.BattlePortraitPos = data["立绘位置"];
        if (data.ContainsKey("内容")) node.BattleContent = data["内容"];
        if (data.ContainsKey("CG")) node.BattleCG = data["CG"];
        if (data.ContainsKey("看过吗")) node.BattleSeen = data["看过吗"] == "1";
    }

    private static void SetExploreFields(DialogueNode node, Dictionary<string, string> data)
    {
        if (data.ContainsKey("人物")) node.ExploreCharacter = data["人物"];
        if (data.ContainsKey("人物立绘")) node.ExplorePortrait = data["人物立绘"];
        if (data.ContainsKey("立绘位置")) node.ExplorePortraitPos = data["立绘位置"];
        if (data.ContainsKey("条件")) node.ExploreCondition = data["条件"];
        if (data.ContainsKey("东西")) node.ExploreItem = data["东西"];
        if (data.ContainsKey("效果")) node.ExploreEffect = data["效果"];
        if (data.ContainsKey("内容")) node.ExploreContent = data["内容"];
        if (data.ContainsKey("看过吗")) node.ExploreSeen = data["看过吗"] == "1";
        if (data.ContainsKey("只能看一次")) node.ExploreOnceOnly = data["只能看一次"] == "1";
    }

    private static void SetNormalFields(DialogueNode node, Dictionary<string, string> data)
    {
        if (data.ContainsKey("位置")) node.NormalPosition = data["位置"];
        if (data.ContainsKey("条件")) node.NormalCondition = data["条件"];
        if (data.ContainsKey("人物")) node.NormalCharacter = data["人物"];
        if (data.ContainsKey("立绘")) node.NormalPortrait = data["立绘"];
        if (data.ContainsKey("立绘位置")) node.NormalPortraitPos = data["立绘位置"];
        if (data.ContainsKey("内容")) node.NormalContent = data["内容"];
        if (data.ContainsKey("CG")) node.NormalCG = data["CG"];
        if (data.ContainsKey("看过吗")) node.NormalSeen = data["看过吗"] == "1";
    }

    private static void SetOutsideFields(DialogueNode node, Dictionary<string, string> data)
    {
        if (data.ContainsKey("位置")) node.OutsidePosition = data["位置"];
        if (data.ContainsKey("条件")) node.OutsideCondition = data["条件"];
        if (data.ContainsKey("人物")) node.OutsideCharacter = data["人物"];
        if (data.ContainsKey("立绘")) node.OutsidePortrait = data["立绘"];
        if (data.ContainsKey("立绘位置")) node.OutsidePortraitPos = data["立绘位置"];
        if (data.ContainsKey("内容")) node.OutsideContent = data["内容"];
        if (data.ContainsKey("CG")) node.OutsideCG = data["CG"];
        if (data.ContainsKey("看过吗")) node.OutsideSeen = data["看过吗"] == "1";
    }

    private static void SetDeadFields(DialogueNode node, Dictionary<string, string> data)
    {
        if (data.ContainsKey("关卡")) node.DeadLevel = data["关卡"];
        if (data.ContainsKey("条件")) node.DeadCondition = data["条件"];
        if (data.ContainsKey("标题")) node.DeadTitle = data["标题"];
        if (data.ContainsKey("死亡内容")) node.DeadContent = data["死亡内容"];
    }
}

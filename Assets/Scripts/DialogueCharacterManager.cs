using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个表情的资料（名字 + 立绘）
/// </summary>
[Serializable]
public class CharacterExpression
{
    public string expressionName;  // 表情名，如 "normal"、"sad"、"jingya"
    public Sprite portrait;        // 对应立绘
}

/// <summary>
/// 角色资料（名字 + 多个表情）
/// </summary>
[Serializable]
public class DialogueCharacter
{
    public string characterName;   // 角色名字
    public List<CharacterExpression> expressions = new List<CharacterExpression>();
}
[Serializable]
public class CGImage
{
    public string cgName;       // CG名称（如 PokerRoom, Battle, Night）
    public Sprite cgSprite;     // 对应Sprite
}

/// <summary>
/// 管理所有角色资料的组件
/// </summary>
public class DialogueCharacterManager : MonoBehaviour
{
    [Header("角色资料列表")]
    public List<DialogueCharacter> characters = new List<DialogueCharacter>();
    [Header("CG背景列表")]
    public List<CGImage> cgs = new List<CGImage>();
    /// <summary>
    /// 根据角色名和表情名获取对应立绘
    /// </summary>
    public Sprite GetPortrait(string characterName, string expressionName = "normal")
    {
        DialogueCharacter character = characters.Find(c => c.characterName == characterName);
        if (character == null)
        {
            Debug.LogWarning($"未找到角色 {characterName}");
            return null;
        }

        CharacterExpression exp = character.expressions.Find(e => e.expressionName == expressionName);
        if (exp == null)
        {
            Debug.LogWarning($"未找到角色 {characterName} 的表情 {expressionName}，已使用第一个表情代替。");
            if (character.expressions.Count > 0)
                return character.expressions[0].portrait;
            else
                return null;
        }

        return exp.portrait;
    }

    public Sprite GetCG(string cgName)
    {
        CGImage cg = cgs.Find(c => c.cgName == cgName);
        if (cg == null)
        {
            Debug.LogWarning($"未找到CG {cgName}");
            return null;
        }
        return cg.cgSprite;
    }
}

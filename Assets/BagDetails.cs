using System.Collections.Generic;
using UnityEngine;
using TMPro; // 用于 TextMeshProUGUI

[System.Serializable]
public class BagCardInfo
{
    public CardData cardData;      // 对应的卡牌数据（可能为空）
    public string cardObjectName;  // BagDetails 子集物件的名字（小写）
    public int quantity;           // 从 DeckManager 读取的数量

    public BagCardInfo(CardData data, string objectName, int count)
    {
        cardData = data;
        cardObjectName = objectName.ToLower();
        quantity = count;
    }
}

public class BagDetails : MonoBehaviour
{
    [Header("cards 子物件下的所有卡片")]
    public List<GameObject> cards = new List<GameObject>();

    [Header("匹配结果：Bag 内卡牌信息")]
    public List<BagCardInfo> bagCardInfos = new List<BagCardInfo>();

    void Start()
    {
        LoadCardsFromChild();
        GenerateBagCardInfos();
        UpdateCardLabels(); // ✅ 初始化后立即更新 UI
    }

    /// <summary>
    /// 读取 "cards" 子物件下的所有一级子对象
    /// </summary>
    private void LoadCardsFromChild()
    {
        Transform cardsParent = transform.Find("cards");
        if (cardsParent == null)
        {
            Debug.LogWarning("未找到名为 'cards' 的子物件！");
            return;
        }

        cards.Clear();
        foreach (Transform child in cardsParent)
        {
            cards.Add(child.gameObject);
            Debug.Log("找到卡片：" + child.name);
        }
        Debug.Log("共找到卡片数量：" + cards.Count);
    }

    /// <summary>
    /// 从 DeckManager 读取 initialDeck，匹配 cards 子物件，生成 BagCardInfo 列表
    /// </summary>
    private void GenerateBagCardInfos()
    {
        bagCardInfos.Clear();

        if (DeckManager.instance == null)
        {
            Debug.LogWarning("DeckManager 未加载！");
            return;
        }

        List<CardData> initialDeck = DeckManager.instance.initialDeck;
        if (initialDeck == null)
        {
            Debug.LogWarning("DeckManager 的 initialDeck 为空！");
            return;
        }

        foreach (var cardObj in cards)
        {
            string objName = cardObj.name;
            string capitalized = char.ToUpper(objName[0]) + objName.Substring(1);

            // 查找匹配的 CardData
            List<CardData> matches = initialDeck.FindAll(c => c.name == capitalized);
            int count = matches.Count;

            // 如果没找到，数量就是 0
            CardData match = matches.Count > 0 ? matches[0] : null;

            BagCardInfo info = new BagCardInfo(match, objName, count);
            bagCardInfos.Add(info);

            if (match != null)
                Debug.Log($"匹配成功：{capitalized} × {count}");
            else
                Debug.LogWarning($"未匹配到卡片：{capitalized}，数量设为 0");
        }

        Debug.Log($"✅ 成功生成 {bagCardInfos.Count} 个 BagCardInfo。");
    }

    /// <summary>
    /// ✅ 更新每个卡牌的“jiaobiao”子物件中的 TextMeshProUGUI 文本为数量（找不到显示 0）
    /// </summary>
    private void UpdateCardLabels()
    {
        foreach (var info in bagCardInfos)
        {
            GameObject cardObj = cards.Find(c => c.name.ToLower() == info.cardObjectName);
            if (cardObj == null)
            {
                Debug.LogWarning($"未找到卡物件：{info.cardObjectName}");
                continue;
            }

            // 查找 jiaobiao 子物件
            Transform jiaobiao = cardObj.transform.Find("jiaobiao");
            if (jiaobiao == null)
            {
                Debug.LogWarning($"卡片 {cardObj.name} 未找到 'jiaobiao' 子物件");
                continue;
            }

            // 在 jiaobiao 下寻找 TextMeshProUGUI
            TextMeshProUGUI text = jiaobiao.GetComponentInChildren<TextMeshProUGUI>();
            if (text == null)
            {
                Debug.LogWarning($"卡片 {cardObj.name} 的 jiaobiao 下未找到 TextMeshProUGUI");
                continue;
            }

            // ✅ 设置数量文字（即使是 0 也显示）
            text.text = info.quantity.ToString();
            Debug.Log($"更新 {cardObj.name} 数量文字为：{info.quantity}");
        }
    }
}

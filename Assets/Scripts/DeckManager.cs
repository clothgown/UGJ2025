using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("初始牌组 (在 Inspector 配置)")]
    public List<CardData> initialDeck; // 初始卡组

    public List<CardData> deck;       // 当前卡组
    private List<CardData> discardPile; // 弃牌堆

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 初始化牌组和弃牌堆
        deck = new List<CardData>(initialDeck);
        discardPile = new List<CardData>();
    }

    /// <summary>
    /// 抽一张牌，如果卡组空了可以洗牌
    /// </summary>
    public CardData DrawCard()
    {
        if (deck.Count == 0)
        {
            if (discardPile.Count > 0)
            {
                // 把弃牌堆洗回去
                deck.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(deck);
            }
            else
            {
                Debug.Log("卡组和弃牌堆都空了，不能抽牌");
                return null;
            }
        }



        CardData drawnCard = deck[0];
        deck.RemoveAt(0);
        
        return drawnCard;

    }

    /// <summary>
    /// 将牌加入弃牌堆
    /// </summary>
    public void Discard(CardData card)
    {
        discardPile.Add(card);
    }

    /// <summary>
    /// 添加新卡到卡组
    /// </summary>
    public void AddCard(CardData card)
    {
        deck.Add(card);
    }

    /// <summary>
    /// 从卡组移除卡
    /// </summary>
    public void RemoveCard(CardData card)
    {
        if (deck.Contains(card))
            deck.Remove(card);
    }

    /// <summary>
    /// 洗牌
    /// </summary>
    public void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}

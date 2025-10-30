using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("初始牌组 (在 Inspector 配置)")]
    public List<CardData> initialDeck; // 初始卡组

    public List<CardData> deck;         // 当前卡组
    private List<CardData> discardPile; // 弃牌堆

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // ✅ 每次加载场景时重置卡组
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化牌组和弃牌堆
        ResetDeckToInitial(); // ✅ 初始化时同步初始牌组
    }

    /// <summary>
    /// ✅ 每次加载场景时，将卡组重置为初始牌组
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetDeckToInitial();
    }

    /// <summary>
    /// ✅ 重置卡组为初始牌组并洗牌
    /// </summary>
    private void ResetDeckToInitial()
    {
        deck = new List<CardData>(initialDeck);
        discardPile = new List<CardData>();
        Shuffle(deck);
    }

    /// <summary>
    /// 抽一张牌，如果卡组空了则从初始牌组复制并洗牌
    /// </summary>
    public CardData DrawCard()
    {
        if (deck.Count == 0)
        {
            // ✅ 当卡组空时，用初始牌组重新复制并洗牌
            if (initialDeck.Count > 0)
            {
                deck = new List<CardData>(initialDeck);
                Shuffle(deck);
                Debug.Log("卡组为空，从初始牌组重新复制并洗牌。");
            }
            else
            {
                Debug.Log("初始牌组为空，无法重置。");
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
    /// 添加新卡到初始牌组
    /// </summary>
    public void AddCard(CardData card)
    {
        initialDeck.Add(card);
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

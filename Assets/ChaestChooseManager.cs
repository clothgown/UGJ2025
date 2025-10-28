using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChaestChooseManager : MonoBehaviour
{
    [Header("随机金币显示（3个TMP文字）")]
    public TextMeshProUGUI[] randomCoinTexts; // 前3个
    [Header("总金币显示（3个TMP文字）")]
    public TextMeshProUGUI[] totalCoinTexts;  // 后3个
    [Header("领取按钮（3个Button）")]
    public Button[] collectButtons;

    [Header("卡片Prefab（3个预制体）")]
    public GameObject[] cardPrefabs;

    [Header("卡片生成位置（3个Transform）")]
    public Transform[] spawnPoints;

    private int[] randomCoins = new int[3];
    private int[] cardIndexes = new int[3]; // 保存每张卡对应的 index
    private bool hasCollected = false; // 整个场景是否已领取过（全局锁）
    private List<GameObject> spawnedCards = new List<GameObject>();

    void Start()
    {
        GenerateRandomCoins();
        SpawnRandomCards();
        UpdateCoinTexts();
        SetupButtons();
    }

    /// <summary>
    /// 随机生成3个金币数（20~50）
    /// </summary>
    void GenerateRandomCoins()
    {
        for (int i = 0; i < 3; i++)
        {
            randomCoins[i] = Random.Range(20, 51);
        }
    }

    /// <summary>
    /// 随机生成三张卡
    /// </summary>
    void SpawnRandomCards()
    {
        if (cardPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("请在 Inspector 中设置 cardPrefabs 和 spawnPoints！");
            return;
        }

        // 清除旧卡
        foreach (var oldCard in spawnedCards)
        {
            if (oldCard != null) Destroy(oldCard);
        }
        spawnedCards.Clear();

        // 随机生成对应卡片
        for (int i = 0; i < 3; i++)
        {
            int randomCardIndex = Random.Range(0, cardPrefabs.Length);
            cardIndexes[i] = randomCardIndex; // 保存对应的卡牌 index

            GameObject card = Instantiate(cardPrefabs[randomCardIndex], spawnPoints[i].position, Quaternion.identity, spawnPoints[i]);
            spawnedCards.Add(card);
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    void UpdateCoinTexts()
    {
        if (randomCoinTexts.Length < 3 || totalCoinTexts.Length < 3)
        {
            Debug.LogError("请在 Inspector 中拖入6个 TextMeshProUGUI 对象（前3个是随机金币，后3个是总金币）");
            return;
        }

        int currentTotal = CollectionManager.instance.coins;

        for (int i = 0; i < 3; i++)
        {
            randomCoinTexts[i].text = randomCoins[i].ToString();
            totalCoinTexts[i].text = (currentTotal + randomCoins[i]).ToString();
        }
    }

    /// <summary>
    /// 初始化按钮点击事件
    /// </summary>
    void SetupButtons()
    {
        if (collectButtons.Length < 3)
        {
            Debug.LogError("请在 Inspector 中拖入3个 Button 对象");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            int index = i; // 防止闭包问题
            collectButtons[i].onClick.AddListener(() => CollectCoin(index));
        }
    }

    /// <summary>
    /// 点击领取金币（只能领取一次），并给队友增加卡牌
    /// </summary>
    void CollectCoin(int index)
    {
        if (hasCollected) return; // 已领取过则忽略

        hasCollected = true;

        // 增加金币
        CollectionManager.instance.coins += randomCoins[index];
        CollectionManager.instance.thisLevelGetCoins += randomCoins[index];

        // 增加卡牌给队友
        int cardIndex = cardIndexes[index];
        ItemCardManager.instance.AddCard(cardIndex, 1); // 增加1张对应卡牌

        // 禁用所有按钮
        foreach (var btn in collectButtons)
        {
            btn.interactable = false;
            if (btn.GetComponent<Window>())
            {
                btn.GetComponent<Window>().canInteract = false;
            }
        }

        Debug.Log($"领取了第 {index + 1} 个宝箱：{randomCoins[index]} 金币，获得卡牌 Index {cardIndex}，总金币：{CollectionManager.instance.coins}");
        SceneManager.LoadScene("Map");
        if (MapGridManager.instance != null)
        {
            MapGridManager.instance.gameObject.SetActive(true);
            MapGridManager.instance.HighlightNearbyGrids();

        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ItemCardData
{
    public int index;   // 卡牌编号
    public string name;
    public int count;   // 拥有数量
}

public class ItemCardManager : MonoBehaviour
{
    public static ItemCardManager instance;

    [Header("玩家拥有的卡牌（通过 index 管理，可在 Inspector 修改）")]
    public List<ItemCardData> itemCards = new List<ItemCardData>();

    [Header("场景中的道具物体")]
    public ItemCard[] items;

    void Awake()
    {
        // 单例模式
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 注册场景加载回调
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 注销事件，避免内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 每次场景加载时调用
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneItems();
        RefreshItemDisplay();
    }

    /// <summary>
    /// 增加卡牌数量
    /// </summary>
    public void AddCard(int index, int amount = 1)
    {
        ItemCardData card = itemCards.Find(c => c.index == index);
        if (card != null)
            card.count += amount;
        else
            itemCards.Add(new ItemCardData() { index = index, count = amount });

        Debug.Log($"获得卡牌：Index {index} × {amount}");
        RefreshItemDisplay();
    }

    /// <summary>
    /// 删除卡牌数量
    /// </summary>
    public void RemoveCard(int index, int amount = 1)
    {
        ItemCardData card = itemCards.Find(c => c.index == index);
        if (card == null)
        {
            Debug.LogWarning($"删除失败：没有 Index {index} 的卡牌。");
            return;
        }

        card.count -= amount;
        if (card.count <= 0)
        {
            itemCards.Remove(card);
            Debug.Log($"卡牌 Index {index} 已用完并被移除。");
        }
        else
        {
            Debug.Log($"使用卡牌：Index {index} × {amount}，剩余：{card.count}");
        }

        RefreshItemDisplay();
    }
    private void FindSceneItems()
    {
        // 查找场景里的所有 ItemCard
        ItemCard[] foundItems = FindObjectsOfType<ItemCard>();
        if (foundItems == null || foundItems.Length == 0) return;

        // 根据名字排序
        List<ItemCard> sortedItems = new List<ItemCard>(foundItems);
        sortedItems.Sort((a, b) => {
            int indexA = ParseItemCardName(a.name);
            int indexB = ParseItemCardName(b.name);
            return indexA.CompareTo(indexB);
        });

        items = sortedItems.ToArray();
    }

    /// <summary>
    /// 将 ItemCard 名字转换成索引
    /// ItemCard -> 0
    /// ItemCard(1) -> 1
    /// ItemCard(2) -> 2
    /// </summary>
    private int ParseItemCardName(string name)
    {
        if (name == "ItemCard") return 0;

        // 格式 ItemCard(x)
        int start = name.IndexOf('(');
        int end = name.IndexOf(')');
        if (start >= 0 && end > start)
        {
            string numStr = name.Substring(start + 1, end - start - 1);
            if (int.TryParse(numStr, out int index))
                return index;
        }

        return int.MaxValue; // 如果名字不符合规则放到最后
    }

    /// <summary>
    /// 根据场景和卡牌数量刷新道具显示
    /// </summary>
    public void RefreshItemDisplay()
    {
        
        // 如果场景不存在 TurnManager，全部隐藏
        if (FindObjectOfType<TurnManager>() == null)
        {
            HideAllItems();
            return;
        }

        // 先隐藏所有
        HideAllItems();

        // 遍历 itemCards，根据 count 显示对应 items
        if (items != null && items.Length > 0)
        {
            foreach (var card in itemCards)
            {
                if (card.count > 0 && card.index >= 0 && card.index < items.Length)
                {
                    if (items[card.index] != null)
                        items[card.index].gameObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// 隐藏所有 items
    /// </summary>
    private void HideAllItems()
    {
        if (items == null || items.Length == 0) return;

        foreach (var item in items)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 获取卡牌数量
    /// </summary>
    public int GetCardCount(int index)
    {
        ItemCardData card = itemCards.Find(c => c.index == index);
        return card != null ? card.count : 0;
    }

    /// <summary>
    /// 清空所有卡牌
    /// </summary>
    public void ClearAll()
    {
        itemCards.Clear();
        HideAllItems();
        Debug.Log("所有卡牌已清空。");
    }


    /// <summary>
    /// 尝试减少指定 index 的卡牌数量
    /// </summary>
    /// <param name="index">卡牌索引</param>
    /// <param name="amount">减少数量</param>
    /// <returns>如果减少成功返回 true，否则返回 false</returns>
    public bool DecreaseCard(int index, int amount = 1)
    {
        ItemCardData card = itemCards.Find(c => c.index == index);
        if (card == null)
        {
            Debug.LogWarning($"减少失败：没有 Index {index} 的卡牌。");
            return false;
        }

       
            card.count -= amount;
            Debug.Log($"使用卡牌：Index {index} × {amount}，剩余：{card.count}");
        

        // 刷新显示
        RefreshItemDisplay();
        return true;
    }

}

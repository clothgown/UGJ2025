using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Store : MonoBehaviour
{
    public GameObject rebornPanel;
    public GameObject detailPanel;

    [Header("可生成的卡牌Prefab列表（带CardInStore脚本）")]
    public List<GameObject> cardPrefabs; // 你自己在Inspector里赋值
    public Window currentWindow;
    void Start()
    {
        // 随机生成卡片
        SpawnCardsInGoods();

        // 自动寻找所有名字为 "Value" 的 TMP Text 并随机赋值
        AssignRandomValuesToAll();
    }

    public void BackToMap()
    {
        SceneManager.LoadScene("Map");
        FindAnyObjectByType<LevelSelectRoot>().SetGridManagerTrue();
    }

    public void ShowDeck()
    {
        foreach (CardData cardData in DeckManager.instance.initialDeck)
        {
            Debug.Log(cardData);
        }
    }

    public void ShowRebornPanel()
    {
        rebornPanel.gameObject.SetActive(true);
    }

    public void CloseRebornPanel()
    {
        rebornPanel.gameObject.SetActive(false);
    }

    public void ShowDetailPanel()
    {
        detailPanel.gameObject.SetActive(true);
    }

    public void CloseDetailPanel()
    {
        detailPanel.gameObject.SetActive(false);
        currentWindow = null;
    }

    public void BuyCard()
    {
        TextMeshProUGUI costText = FindDeepChild(detailPanel.transform, "cost").GetComponent<TextMeshProUGUI>(); ;
        int cost;
        int.TryParse(costText.text, out cost);
        if(CollectionManager.instance.coins >= cost)
        {
            FindAnyObjectByType<DeckManager>().AddCard(FindDeepChild(detailPanel.transform, "goods(Clone)").GetChild(0).GetComponent<CradInStore>().cardData);
            CollectionManager.instance.coins -= cost;

            currentWindow.gameObject.SetActive(false);
        }
    }
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    /// <summary>
    /// 查找所有名字为 "Value" 的 TextMeshProUGUI 并随机赋值
    /// </summary>
    private void AssignRandomValuesToAll()
    {
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (TextMeshProUGUI tmp in allTexts)
        {
            if (tmp.name == "Value")
            {
                int randomValue = Random.Range(150, 201);
                tmp.text = randomValue.ToString();
                count++;
            }
        }

        Debug.Log($"随机赋值完成，共修改 {count} 个名为 'Value' 的 TMP Text。");
    }

    /// <summary>
    /// 在所有名为 "goods" 的 GameObject 下随机生成卡片Prefab
    /// </summary>
    private void SpawnCardsInGoods()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int spawnCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "goods")
            {
                if (cardPrefabs != null && cardPrefabs.Count > 0)
                {
                    GameObject prefab = cardPrefabs[Random.Range(0, cardPrefabs.Count)];
                    GameObject newCard = Instantiate(prefab, obj.transform);
                    newCard.transform.localPosition = Vector3.zero; // 居中放置
                    spawnCount++;
                }
                else
                {
                    Debug.LogWarning("cardPrefabs 列表为空，请在 Inspector 中赋值。");
                    break;
                }
            }
        }

        Debug.Log($"已在 {spawnCount} 个 goods 对象下生成随机卡片。");
    }
}

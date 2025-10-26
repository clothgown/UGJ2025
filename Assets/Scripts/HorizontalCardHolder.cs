using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class HorizontalCardHolder : MonoBehaviour
{
    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    public List<Card> cards;

    [Header("Card Draw Settings")]
    [SerializeField] private float drawAnimationDuration = 0.5f;

    bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    [Header("起始手牌数量")]
    [SerializeField] private int startingHandSize = 5;
    public bool isDraging=false;
    void Start()
    {
        DeckManager.instance.Shuffle(DeckManager.instance.deck);
        rect = GetComponent<RectTransform>();
        cards = new List<Card>();
        
        // 起始抽牌
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawNewCardImmediate();
        }
    }

    public void DrawNewCardImmediate()
    {
        if (cards.Count >= startingHandSize)
        {
            return;
        }
        CardData data = DeckManager.instance.DrawCard();
        if (data == null) return;

        GameObject newSlot = Instantiate(slotPrefab, transform);
        Card newCard = newSlot.GetComponentInChildren<Card>();
        newCard.data = data;
        newCard.name = data.cardName;

        // 绑定事件
        newCard.PointerEnterEvent.AddListener(CardPointerEnter);
        newCard.PointerExitEvent.AddListener(CardPointerExit);
        newCard.BeginDragEvent.AddListener(BeginDrag);
        newCard.EndDragEvent.AddListener(EndDrag);

        cards.Add(newCard);
    }

    public IEnumerator DrawNewCard()
    {
        
        CardData data = DeckManager.instance.DrawCard();
        if (data == null) yield break;

        GameObject newSlot = Instantiate(slotPrefab, transform);
        Card newCard = newSlot.GetComponentInChildren<Card>();
        newCard.data = data;
        newCard.name = data.cardName;

        // 绑定事件
        newCard.PointerEnterEvent.AddListener(CardPointerEnter);
        newCard.PointerExitEvent.AddListener(CardPointerExit);
        newCard.BeginDragEvent.AddListener(BeginDrag);
        newCard.EndDragEvent.AddListener(EndDrag);

        // 动画
        Vector3 startPos = new Vector3(1000f, 0, 0);
        newSlot.transform.localPosition = startPos;
        newSlot.transform.DOLocalMove(Vector3.zero, drawAnimationDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(drawAnimationDuration);
        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect); 
        foreach (Card card in cards) 
        { 
            if (card.cardVisual != null) 
                card.cardVisual.UpdateIndex(transform.childCount); 
        }
        cards.Add(newCard);


    }

    private void BeginDrag(Card card)
    {
        
        selectedCard = card;
        
    }

    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0, selectedCard.selectionOffset, 0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;
        if(IsoGrid2D.instance.currentSelectedGrid == IsoGrid2D.instance.currentPlayerGrid)
        {
            UseCard(selectedCard);
        }

        selectedCard = null;
        SoundManager.Instance.PlayenddragAudio();

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        // New card draw input
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    StartCoroutine(DrawNewCard());
        //}

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        //Updated Visual Indexes
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }


    void UseCard(Card card)
    {
        if (card == null) return;

        // 检查行动点
        if (TurnManager.instance.currentController.actionPoints <= 0)
        {
            Debug.Log("没有行动点，无法出牌！");
            return;
        }

        bool executed = card.ExecuteEffect();
        if (!executed)
        {
            Debug.Log("卡牌未生效，不消耗。");
            return;
        }

        TurnManager.instance.currentController.UseActionPoint(1);
        // 丢进弃牌堆
        DeckManager.instance.Discard(card.data);

        // 移除手牌
        cards.Remove(card);

        StartCoroutine(PlayerAnimation());
        Destroy(card.transform.parent.gameObject);
        //StartCoroutine(DrawNewCard());
        //// 更新显示
        //foreach (var c in cards)
        //    if (c.cardVisual != null)
        //        c.cardVisual.UpdateIndex(transform.childCount);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        
    }

    public void DrawCardAndUpdate()
    {
        if (cards.Count > startingHandSize)
        {
            return;
        }
        StartCoroutine(DrawNewCard());
        // 更新显示
        foreach (var c in cards)
            if (c.cardVisual != null)
                c.cardVisual.UpdateIndex(transform.childCount);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private IEnumerator PlayerAnimation()
    {
        // TODO: 根据 card.data.effectType 播放不同的动画
        Debug.Log($"播放 动画...");

        // 先留个假延时模拟动画
        yield return new WaitForSeconds(0.5f);

        // 动画结束后的逻辑（如果有）
        Debug.Log("动画播放完成");
    }

    public void ChangeAllCardToDouble()
    {
        foreach (var card in cards)
        {
            if (card.cardVisual != null)
            {
                card.cardVisual.SetDouble();
            }
        }

        
    }

    public void ChangeAllCardToMass()
    {
        foreach (var card in cards)
        {
            if (card.cardVisual != null)
            {
                card.cardVisual.SetMass();
            }
        }

        
    }

    public void ChangeAllCardToNormal()
    {
        foreach (var card in cards)
        {
            if (card.cardVisual != null)
            {
                card.cardVisual.SetNormal();
            }
        }

        
    }

}
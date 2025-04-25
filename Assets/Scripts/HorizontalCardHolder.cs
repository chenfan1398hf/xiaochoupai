using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;  // 使用DOTween动画库
using System.Linq;

public class HorizontalCardHolder : MonoBehaviour
{
    [SerializeField] private Card selectedCard;   // 当前被选中的卡牌
    [SerializeReference] private Card hoveredCard; // 当前鼠标悬停的卡牌

    [SerializeField] private GameObject slotPrefab; // 卡槽预制体
    private RectTransform rect;                  // 卡牌容器的矩形变换组件

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7; // 初始生成卡牌数量
    public List<Card> cards;                      // 存储所有卡牌的列表

    bool isCrossing = false;                      // 防止交换过程中的重复操作标志
    [SerializeField] private bool tweenCardReturn = true; // 是否使用卡牌返回动画

    void Start()
    {
        // 生成指定数量的卡槽
        for (int i = 0; i < cardsToSpawn; i++)
        {
            Instantiate(slotPrefab, transform);
        }

        rect = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<Card>().ToList(); // 获取所有子对象的Card组件

        int cardCount = 0;

        // 为每个卡牌初始化事件监听
        foreach (Card card in cards)
        {
            card.PointerEnterEvent.AddListener(CardPointerEnter);  // 鼠标进入事件
            card.PointerExitEvent.AddListener(CardPointerExit);     // 鼠标离开事件
            card.BeginDragEvent.AddListener(BeginDrag);             // 开始拖拽事件
            card.EndDragEvent.AddListener(EndDrag);                 // 结束拖拽事件
            card.name = cardCount.ToString();                      // 设置卡牌名称
            cardCount++;
        }

        // 延迟一帧后执行初始化
        StartCoroutine(Frame());

        // 初始化协程
        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            // 更新所有卡牌的视觉索引
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
    }

    // 开始拖拽时的处理
    private void BeginDrag(Card card)
    {
        selectedCard = card;  // 设置当前选中的卡牌
    }

    // 结束拖拽时的处理
    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        // 使用DOTween动画移动卡牌回位
        selectedCard.transform.DOLocalMove(
            selectedCard.selected ?
            new Vector3(0, selectedCard.selectionOffset, 0) : // 如果被选中则保持偏移
            Vector3.zero,                                     // 否则回到原点
            tweenCardReturn ? .15f : 0)                      // 根据设置决定动画时间
            .SetEase(Ease.OutBack);                           // 设置缓动效果

        // 触发布局重建（通过临时修改尺寸）
        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;  // 清空当前选中卡牌
    }

    // 鼠标进入卡牌时的处理
    void CardPointerEnter(Card card)
    {
        hoveredCard = card;  // 设置当前悬停卡牌
    }

    // 鼠标离开卡牌时的处理
    void CardPointerExit(Card card)
    {
        hoveredCard = null;  // 清空悬停卡牌
    }

    void Update()
    {
        // 删除键处理：删除当前悬停的卡牌
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);
            }
        }

        // 右键点击处理：取消所有卡牌的选中状态
        if (Input.GetMouseButtonDown(1))
        {
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        // 检测卡牌位置交换
        for (int i = 0; i < cards.Count; i++)
        {
            // 当拖动卡牌位置超过其他卡牌时进行交换
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

    // 交换两个卡牌的位置
    void Swap(int index)
    {
        isCrossing = true;  // 设置交换标志防止重复操作

        // 获取需要交换的两个卡牌的父节点
        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        // 交换父节点（实际改变在层次结构中的顺序）
        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ?
            new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;  // 清除交换标志

        if (cards[index].cardVisual == null)
            return;

        // 确定交换方向（用于视觉效果）
        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        // 更新所有卡牌的视觉索引
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;  // ʹ��DOTween������
using System.Linq;

public class HorizontalCardHolder : MonoBehaviour
{
    [SerializeField] private Card selectedCard;   // ��ǰ��ѡ�еĿ���
    [SerializeReference] private Card hoveredCard; // ��ǰ�����ͣ�Ŀ���

    [SerializeField] private GameObject slotPrefab; // ����Ԥ����
    private RectTransform rect;                  // ���������ľ��α任���

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7; // ��ʼ���ɿ�������
    public List<Card> cards;                      // �洢���п��Ƶ��б�

    bool isCrossing = false;                      // ��ֹ���������е��ظ�������־
    [SerializeField] private bool tweenCardReturn = true; // �Ƿ�ʹ�ÿ��Ʒ��ض���

    void Start()
    {
        // ����ָ�������Ŀ���
        for (int i = 0; i < cardsToSpawn; i++)
        {
            Instantiate(slotPrefab, transform);
        }

        rect = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<Card>().ToList(); // ��ȡ�����Ӷ����Card���

        int cardCount = 0;

        // Ϊÿ�����Ƴ�ʼ���¼�����
        foreach (Card card in cards)
        {
            card.PointerEnterEvent.AddListener(CardPointerEnter);  // �������¼�
            card.PointerExitEvent.AddListener(CardPointerExit);     // ����뿪�¼�
            card.BeginDragEvent.AddListener(BeginDrag);             // ��ʼ��ק�¼�
            card.EndDragEvent.AddListener(EndDrag);                 // ������ק�¼�
            card.name = cardCount.ToString();                      // ���ÿ�������
            cardCount++;
        }

        // �ӳ�һ֡��ִ�г�ʼ��
        StartCoroutine(Frame());

        // ��ʼ��Э��
        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            // �������п��Ƶ��Ӿ�����
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
    }

    // ��ʼ��קʱ�Ĵ���
    private void BeginDrag(Card card)
    {
        selectedCard = card;  // ���õ�ǰѡ�еĿ���
    }

    // ������קʱ�Ĵ���
    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        // ʹ��DOTween�����ƶ����ƻ�λ
        selectedCard.transform.DOLocalMove(
            selectedCard.selected ?
            new Vector3(0, selectedCard.selectionOffset, 0) : // �����ѡ���򱣳�ƫ��
            Vector3.zero,                                     // ����ص�ԭ��
            tweenCardReturn ? .15f : 0)                      // �������þ�������ʱ��
            .SetEase(Ease.OutBack);                           // ���û���Ч��

        // ���������ؽ���ͨ����ʱ�޸ĳߴ磩
        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;  // ��յ�ǰѡ�п���
    }

    // �����뿨��ʱ�Ĵ���
    void CardPointerEnter(Card card)
    {
        hoveredCard = card;  // ���õ�ǰ��ͣ����
    }

    // ����뿪����ʱ�Ĵ���
    void CardPointerExit(Card card)
    {
        hoveredCard = null;  // �����ͣ����
    }

    void Update()
    {
        // ɾ��������ɾ����ǰ��ͣ�Ŀ���
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);
            }
        }

        // �Ҽ��������ȡ�����п��Ƶ�ѡ��״̬
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

        // ��⿨��λ�ý���
        for (int i = 0; i < cards.Count; i++)
        {
            // ���϶�����λ�ó�����������ʱ���н���
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

    // �����������Ƶ�λ��
    void Swap(int index)
    {
        isCrossing = true;  // ���ý�����־��ֹ�ظ�����

        // ��ȡ��Ҫ�������������Ƶĸ��ڵ�
        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        // �������ڵ㣨ʵ�ʸı��ڲ�νṹ�е�˳��
        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ?
            new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;  // ���������־

        if (cards[index].cardVisual == null)
            return;

        // ȷ���������������Ӿ�Ч����
        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        // �������п��Ƶ��Ӿ�����
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }
}
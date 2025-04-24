using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

// �����࣬�����ƵĽ�����Ϊ����ק����ͣ������ȣ�
public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;                  // �����Ļ������
    private Image imageComponent;          // ���Ƶ�ͼ�����
    [SerializeField] private bool instantiateVisual = true; // �Ƿ�ʵ�����Ӿ�����
    private VisualCardsHandler visualHandler; // �Ӿ���Ƭ������
    private Vector3 offset;                 // ��קʱ��ƫ����

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50; // �ƶ��ٶ�����

    [Header("Selection")]
    public bool selected;                   // �Ƿ�ѡ��
    public float selectionOffset = 50;      // ѡ��ʱ��ƫ����
    private float pointerDownTime;          // ��갴��ʱ��
    private float pointerUpTime;            // ���̧��ʱ��

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab; // �����Ӿ�Ԥ����
    [HideInInspector] public CardVisual cardVisual; // �����Ӿ����

    [Header("States")]
    public bool isHovering;                 // �Ƿ���ͣ��
    public bool isDragging;                 // �Ƿ�������ק
    [HideInInspector] public bool wasDragged; // �Ƿ���ק��

    [Header("Events")]
    // �����¼�����
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    void Start()
    {
        // ��ʼ���������
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        // �����Ҫʵ�����Ӿ�����
        if (!instantiateVisual)
            return;

        // ���������Ӿ�����
        visualHandler = FindObjectOfType<VisualCardsHandler>();
        cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this); // ��ʼ���Ӿ����
    }

    void Update()
    {
        ClampPosition(); // ÿ֡����λ������Ļ��

        // ��קʱ���ƶ��߼�
        if (isDragging)
        {
            // ����Ŀ��λ�ú��ƶ�����
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            // �����ٶȲ��ƶ�
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    // ���ƿ�������Ļ��Χ��
    void ClampPosition()
    {
        // ������Ļ�߽�
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        // ǯ��X/Y����
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    // ��ʼ��ק�¼�����
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        // �����ʼƫ����
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        // �������߼�����
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true; // �������ק
    }

    // ��ק���¼�������ʵ�֣��ƶ��߼���Update�У�
    public void OnDrag(PointerEventData eventData)
    {
    }

    // ������ק�¼�����
    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        // �ָ����߼��
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        // �ӳ�һ֡������ק���
        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    // �������¼�����
    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    // ����뿪�¼�����
    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    // ��갴���¼�����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time; // ��¼����ʱ��
    }

    // ���̧���¼�����
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time; // ��¼̧��ʱ��

        // ��������¼����ж��Ƿ��ǳ�����
        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f) // ����0.2����Ϊ����
            return;

        if (wasDragged) // �����������ק����Ե��
            return;

        // �л�ѡ��״̬
        selected = !selected;
        SelectEvent.Invoke(this, selected);

        // ����ѡ��״̬����λ��
        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }

    // ȡ��ѡ�з���
    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            // ��λλ�ã�ԭ�������ظ��жϣ������Ǳ���
            if (selected)
                transform.localPosition += (cardVisual.transform.up * 50);
            else
                transform.localPosition = Vector3.zero;
        }
    }

    // ��ȡͬ�㼶�����������������Slot���ͣ�
    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    // ��ȡ���������������������Slot���ͣ�
    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    // ��ȡ��׼��λ�ã�0-1��Χ��
    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    // ����ʱͬʱ�����Ӿ�����
    private void OnDestroy()
    {
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
    }
}
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

// 卡牌类，处理卡牌的交互行为（拖拽、悬停、点击等）
public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;                  // 所属的画布组件
    private Image imageComponent;          // 卡牌的图像组件
    [SerializeField] private bool instantiateVisual = true; // 是否实例化视觉对象
    private VisualCardsHandler visualHandler; // 视觉卡片管理器
    private Vector3 offset;                 // 拖拽时的偏移量

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50; // 移动速度限制

    [Header("Selection")]
    public bool selected;                   // 是否被选中
    public float selectionOffset = 50;      // 选中时的偏移量
    private float pointerDownTime;          // 鼠标按下时间
    private float pointerUpTime;            // 鼠标抬起时间

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab; // 卡牌视觉预制体
    [HideInInspector] public CardVisual cardVisual; // 卡牌视觉组件

    [Header("States")]
    public bool isHovering;                 // 是否悬停中
    public bool isDragging;                 // 是否正在拖拽
    [HideInInspector] public bool wasDragged; // 是否被拖拽过

    [Header("Events")]
    // 各种事件定义
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    void Start()
    {
        // 初始化组件引用
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        // 如果需要实例化视觉对象
        if (!instantiateVisual)
            return;

        // 创建卡牌视觉对象
        visualHandler = FindObjectOfType<VisualCardsHandler>();
        cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this); // 初始化视觉组件
    }

    void Update()
    {
        ClampPosition(); // 每帧限制位置在屏幕内

        // 拖拽时的移动逻辑
        if (isDragging)
        {
            // 计算目标位置和移动方向
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            // 计算速度并移动
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    // 限制卡牌在屏幕范围内
    void ClampPosition()
    {
        // 计算屏幕边界
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        // 钳制X/Y坐标
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    // 开始拖拽事件处理
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        // 计算初始偏移量
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        // 禁用射线检测组件
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true; // 标记已拖拽
    }

    // 拖拽中事件处理（空实现，移动逻辑在Update中）
    public void OnDrag(PointerEventData eventData)
    {
    }

    // 结束拖拽事件处理
    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        // 恢复射线检测
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        // 延迟一帧重置拖拽标记
        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    // 鼠标进入事件处理
    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    // 鼠标离开事件处理
    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    // 鼠标按下事件处理
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time; // 记录按下时间
    }

    // 鼠标抬起事件处理
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time; // 记录抬起时间

        // 触发点击事件（判断是否是长按）
        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f) // 超过0.2秒视为长按
            return;

        if (wasDragged) // 如果发生过拖拽则忽略点击
            return;

        // 切换选中状态
        selected = !selected;
        SelectEvent.Invoke(this, selected);

        // 根据选中状态调整位置
        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }

    // 取消选中方法
    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            // 复位位置（原代码有重复判断，可能是笔误）
            if (selected)
                transform.localPosition += (cardVisual.transform.up * 50);
            else
                transform.localPosition = Vector3.zero;
        }
    }

    // 获取同层级数量（如果父对象是Slot类型）
    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    // 获取父级索引（如果父对象是Slot类型）
    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    // 获取标准化位置（0-1范围）
    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    // 销毁时同时销毁视觉对象
    private void OnDestroy()
    {
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
    }
}
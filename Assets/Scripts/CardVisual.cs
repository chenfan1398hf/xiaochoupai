using System;
using UnityEngine;
using DG.Tweening;  // 使用DOTween动画插件
using System.Collections;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.UI;
using Unity.VisualScripting;

// 卡牌视觉表现处理类，负责所有视觉动画效果
public class CardVisual : MonoBehaviour
{
    private bool initalize = false;  // 初始化状态标记（注意拼写应为initialized）

    [Header("Card")]
    public Card parentCard;          // 关联的卡牌逻辑组件
    private Transform cardTransform; // 卡牌逻辑对象的Transform组件
    private Vector3 rotationDelta;   // 旋转差值（用于平滑旋转）
    private int savedIndex;          // 保存的父级索引（用于动画计算）
    Vector3 movementDelta;           // 移动差值（用于平滑移动）
    private Canvas canvas;           // 当前视觉对象的Canvas组件

    [Header("References")]
    public Transform visualShadow;   // 阴影视觉对象
    private float shadowOffset = 20; // 阴影偏移量
    private Vector2 shadowDistance;  // 阴影初始距离
    private Canvas shadowCanvas;     // 阴影Canvas组件
    [SerializeField] private Transform shakeParent;  // 震动父级对象
    [SerializeField] private Transform tiltParent;    // 倾斜父级对象
    [SerializeField] private Image cardImage;        // 卡牌图像组件

    [Header("Follow Parameters")]
    [SerializeField] private float followSpeed = 30;  // 跟随主体卡牌的速度

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;     // 基础旋转量
    [SerializeField] private float rotationSpeed = 20;      // 旋转速度
    [SerializeField] private float autoTiltAmount = 30;     // 自动倾斜幅度
    [SerializeField] private float manualTiltAmount = 20;   // 手动倾斜幅度
    [SerializeField] private float tiltSpeed = 20;          // 倾斜速度

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;    // 是否启用缩放动画
    [SerializeField] private float scaleOnHover = 1.15f;     // 悬停时缩放比例
    [SerializeField] private float scaleOnSelect = 1.25f;    // 选中时缩放比例
    [SerializeField] private float scaleTransition = .15f;   // 缩放过渡时间
    [SerializeField] private Ease scaleEase = Ease.OutBack;  // 缩放缓动类型

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;   // 选中时冲击动画强度

    [Header("Hober Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;      // 悬停时抖动角度
    [SerializeField] private float hoverTransition = .15f;   // 悬停动画过渡时间

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;     // 是否启用交换动画
    [SerializeField] private float swapRotationAngle = 30;   // 交换旋转角度
    [SerializeField] private float swapTransition = .15f;    // 交换动画时间
    [SerializeField] private int swapVibrato = 5;            // 交换动画震动频率

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;  // 曲线参数配置

    private float curveYOffset;          // 基于曲线的Y轴偏移
    private float curveRotationOffset;   // 基于曲线的旋转偏移
    private Coroutine pressCoroutine;    // 按下协程引用

    private void Start()
    {
        shadowDistance = visualShadow.localPosition;  // 保存阴影初始位置
    }

    // 初始化方法（由Card组件调用）
    public void Initialize(Card target, int index = 0)
    {
        // 组件引用获取
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        // 事件监听绑定
        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);
        parentCard.BeginDragEvent.AddListener(BeginDrag);
        parentCard.EndDragEvent.AddListener(EndDrag);
        parentCard.PointerDownEvent.AddListener(PointerDown);
        parentCard.PointerUpEvent.AddListener(PointerUp);
        parentCard.SelectEvent.AddListener(Select);

        initalize = true;  // 标记完成初始化
    }

    // 更新层级索引
    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        HandPositioning();  // 手牌位置计算
        SmoothFollow();     // 平滑跟随主体
        FollowRotation();   // 跟随旋转计算
        CardTilt();         // 卡牌倾斜计算
    }

    // 计算基于曲线的手牌布局位置
    private void HandPositioning()
    {
        // 使用曲线计算Y轴偏移量（考虑布局影响）
        curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
        curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;  // 数量较少时禁用偏移
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());  // 计算旋转曲线值
    }

    // 平滑跟随主体卡牌位置
    private void SmoothFollow()
    {
        // 计算垂直偏移（拖拽时禁用曲线偏移）
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        // 插值更新位置
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
    }

    // 处理卡牌跟随旋转逻辑
    private void FollowRotation()
    {
        // 计算移动差值
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        // 计算旋转量（拖拽时使用差值，否则直接使用移动量）
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        // 平滑旋转变化
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        // 应用Z轴旋转（限制最大角度）
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    // 处理卡牌倾斜效果
    private void CardTilt()
    {
        // 保存当前索引（拖拽时保持索引不变）
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();

        // 使用三角函数生成波动效果
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        // 计算鼠标位置偏移量
        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 计算各轴倾斜量
        float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        // 平滑过渡各轴角度
        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    // 处理选中/取消选中状态
    private void Select(Card card, bool state)
    {
        DOTween.Kill(2, true);  // 停止之前的动画
        float dir = state ? 1 : 0;
        // 播放冲击动画
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20, 1).SetId(2);

        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    // 交换动画方法
    public void Swap(float dir = 1)
    {
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1).SetId(3);
    }

    // 开始拖拽事件处理
    private void BeginDrag(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        canvas.overrideSorting = true;  // 确保置顶显示
    }

    // 结束拖拽事件处理
    private void EndDrag(Card card)
    {
        canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    // 鼠标进入事件处理
    private void PointerEnter(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    // 鼠标离开事件处理
    private void PointerExit(Card card)
    {
        if (!parentCard.wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    // 鼠标抬起事件处理
    private void PointerUp(Card card, bool longPress)
    {
        if (scaleAnimations)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        canvas.overrideSorting = false;

        visualShadow.localPosition = shadowDistance;  // 重置阴影位置
        shadowCanvas.overrideSorting = true;
    }

    // 鼠标按下事件处理
    private void PointerDown(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += (-Vector3.up * shadowOffset);  // 下移阴影
        shadowCanvas.overrideSorting = false;
    }
}
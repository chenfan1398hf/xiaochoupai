using System;
using UnityEngine;
using DG.Tweening;  // ʹ��DOTween�������
using System.Collections;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.UI;
using Unity.VisualScripting;

// �����Ӿ����ִ����࣬���������Ӿ�����Ч��
public class CardVisual : MonoBehaviour
{
    private bool initalize = false;  // ��ʼ��״̬��ǣ�ע��ƴдӦΪinitialized��

    [Header("Card")]
    public Card parentCard;          // �����Ŀ����߼����
    private Transform cardTransform; // �����߼������Transform���
    private Vector3 rotationDelta;   // ��ת��ֵ������ƽ����ת��
    private int savedIndex;          // ����ĸ������������ڶ������㣩
    Vector3 movementDelta;           // �ƶ���ֵ������ƽ���ƶ���
    private Canvas canvas;           // ��ǰ�Ӿ������Canvas���

    [Header("References")]
    public Transform visualShadow;   // ��Ӱ�Ӿ�����
    private float shadowOffset = 20; // ��Ӱƫ����
    private Vector2 shadowDistance;  // ��Ӱ��ʼ����
    private Canvas shadowCanvas;     // ��ӰCanvas���
    [SerializeField] private Transform shakeParent;  // �𶯸�������
    [SerializeField] private Transform tiltParent;    // ��б��������
    [SerializeField] private Image cardImage;        // ����ͼ�����

    [Header("Follow Parameters")]
    [SerializeField] private float followSpeed = 30;  // �������忨�Ƶ��ٶ�

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;     // ������ת��
    [SerializeField] private float rotationSpeed = 20;      // ��ת�ٶ�
    [SerializeField] private float autoTiltAmount = 30;     // �Զ���б����
    [SerializeField] private float manualTiltAmount = 20;   // �ֶ���б����
    [SerializeField] private float tiltSpeed = 20;          // ��б�ٶ�

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;    // �Ƿ��������Ŷ���
    [SerializeField] private float scaleOnHover = 1.15f;     // ��ͣʱ���ű���
    [SerializeField] private float scaleOnSelect = 1.25f;    // ѡ��ʱ���ű���
    [SerializeField] private float scaleTransition = .15f;   // ���Ź���ʱ��
    [SerializeField] private Ease scaleEase = Ease.OutBack;  // ���Ż�������

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;   // ѡ��ʱ�������ǿ��

    [Header("Hober Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;      // ��ͣʱ�����Ƕ�
    [SerializeField] private float hoverTransition = .15f;   // ��ͣ��������ʱ��

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;     // �Ƿ����ý�������
    [SerializeField] private float swapRotationAngle = 30;   // ������ת�Ƕ�
    [SerializeField] private float swapTransition = .15f;    // ��������ʱ��
    [SerializeField] private int swapVibrato = 5;            // ����������Ƶ��

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;  // ���߲�������

    private float curveYOffset;          // �������ߵ�Y��ƫ��
    private float curveRotationOffset;   // �������ߵ���תƫ��
    private Coroutine pressCoroutine;    // ����Э������

    private void Start()
    {
        shadowDistance = visualShadow.localPosition;  // ������Ӱ��ʼλ��
    }

    // ��ʼ����������Card������ã�
    public void Initialize(Card target, int index = 0)
    {
        // ������û�ȡ
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        // �¼�������
        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);
        parentCard.BeginDragEvent.AddListener(BeginDrag);
        parentCard.EndDragEvent.AddListener(EndDrag);
        parentCard.PointerDownEvent.AddListener(PointerDown);
        parentCard.PointerUpEvent.AddListener(PointerUp);
        parentCard.SelectEvent.AddListener(Select);

        initalize = true;  // �����ɳ�ʼ��
    }

    // ���²㼶����
    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        HandPositioning();  // ����λ�ü���
        SmoothFollow();     // ƽ����������
        FollowRotation();   // ������ת����
        CardTilt();         // ������б����
    }

    // ����������ߵ����Ʋ���λ��
    private void HandPositioning()
    {
        // ʹ�����߼���Y��ƫ���������ǲ���Ӱ�죩
        curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
        curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;  // ��������ʱ����ƫ��
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());  // ������ת����ֵ
    }

    // ƽ���������忨��λ��
    private void SmoothFollow()
    {
        // ���㴹ֱƫ�ƣ���קʱ��������ƫ�ƣ�
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        // ��ֵ����λ��
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
    }

    // �����Ƹ�����ת�߼�
    private void FollowRotation()
    {
        // �����ƶ���ֵ
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        // ������ת������קʱʹ�ò�ֵ������ֱ��ʹ���ƶ�����
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        // ƽ����ת�仯
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        // Ӧ��Z����ת���������Ƕȣ�
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    // ��������бЧ��
    private void CardTilt()
    {
        // ���浱ǰ��������קʱ�����������䣩
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();

        // ʹ�����Ǻ������ɲ���Ч��
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        // �������λ��ƫ����
        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // ���������б��
        float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        // ƽ�����ɸ���Ƕ�
        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    // ����ѡ��/ȡ��ѡ��״̬
    private void Select(Card card, bool state)
    {
        DOTween.Kill(2, true);  // ֹ֮ͣǰ�Ķ���
        float dir = state ? 1 : 0;
        // ���ų������
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20, 1).SetId(2);

        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    // ������������
    public void Swap(float dir = 1)
    {
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1).SetId(3);
    }

    // ��ʼ��ק�¼�����
    private void BeginDrag(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        canvas.overrideSorting = true;  // ȷ���ö���ʾ
    }

    // ������ק�¼�����
    private void EndDrag(Card card)
    {
        canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    // �������¼�����
    private void PointerEnter(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    // ����뿪�¼�����
    private void PointerExit(Card card)
    {
        if (!parentCard.wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    // ���̧���¼�����
    private void PointerUp(Card card, bool longPress)
    {
        if (scaleAnimations)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        canvas.overrideSorting = false;

        visualShadow.localPosition = shadowDistance;  // ������Ӱλ��
        shadowCanvas.overrideSorting = true;
    }

    // ��갴���¼�����
    private void PointerDown(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += (-Vector3.up * shadowOffset);  // ������Ӱ
        shadowCanvas.overrideSorting = false;
    }
}
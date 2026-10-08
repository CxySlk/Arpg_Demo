using DG.Tweening;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包技能槽：显示一个已学技能，可拖拽装备到热栏槽；点击由背包窗口处理看详情。
/// 拖拽为"克隆跟手 + 只传 sprite"模型：拖时生成临时克隆跟随鼠标（本槽图标不动），
/// 放到热栏槽后只把 <see cref="SkillIcon"/>（sprite）与技能 id 交给热栏槽，热栏槽用自有 Image 显示。
/// </summary>
public class UI_BagSkillSlot : MonoBehaviour
{
    [LabelText("悬停放大比例"), SerializeField] private float _hoverScale = 1.1f;
    [LabelText("动画时长(秒)"), SerializeField] private float _scaleDuration = 0.15f;

    /// <summary>当前缩放手势动画（进入/离开共用，先 Kill 旧的再开新的，避免连点冲突）</summary>
    private Tween _scaleTween;

    /// <summary>拖拽时的临时跟随图标（克隆）</summary>
    private Image _dragClone;

    /// <summary>本槽的技能 id（-1 = 无效/空）</summary>
    public int SkillId { get; private set; } = -1;

    /// <summary>是否已装备到热栏（已装备 → 禁止拖拽并置灰提示）</summary>
    [LabelText("是否装备"), ShowInInspector, ReadOnly]
    public bool IsEquipped { get; private set; }

    [LabelText("技能图标"), SerializeField] private Image _imgIcon;
    public Image ImgIcon => _imgIcon;
    public Sprite SkillIcon => _imgIcon != null ? _imgIcon.sprite : null;

    [LabelText("图标边框"), SerializeField] private Image _imgBorder;
    [LabelText("技能按钮"), SerializeField] private Button _btnSlot;
    public Button BtnSlot => _btnSlot;

    private void Awake()
    {
        // 悬停放大（带音效）
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnMouseEnter, eventData => OnMouseEnter());
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnMouseExit, eventData => OnMouseExit());
        // 拖拽（装备到热栏）
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnBeginDrag, eventData => OnBeginDrag(eventData));
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnDrag, eventData => OnDrag(eventData));
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnEndDrag, eventData => OnEndDrag());
    }

    public void SetSkillId(int id) => SkillId = id;

    public void SetIcon(Sprite icon)
    {
        if (_imgIcon == null) return;
        _imgIcon.sprite = icon;
        _imgIcon.enabled = icon != null;
    }

    public void SetInfo(int id, Sprite icon)
    {
        SkillId = id;
        SetIcon(icon);
    }

    /// <summary>设置装备状态：已装备 → 图标置灰（视觉提示"已给到热栏"）并禁止拖拽；未装备 → 恢复原色。</summary>
    public void SetEquipped(bool equipped)
    {
        if (IsEquipped == equipped) return;
        IsEquipped = equipped;
        if (_imgIcon != null)
            _imgIcon.color = equipped ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }

    #region 悬停缩放

    private void OnMouseEnter()
    {
        // 播放音效
        AudioSystem.PlayEffectMusic("ButtonClick");

        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(Vector3.one * _hoverScale, _scaleDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void OnMouseExit()
    {
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(Vector3.one, _scaleDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    #endregion

    #region 拖拽（克隆跟手，落点只接收 sprite）

    private void OnBeginDrag(PointerEventData eventData)
    {
        // 已装备（UI 状态或装备数据任一命中即拦截，权威判断防重复拖）或没有图标 → 不拖
        if (IsEquipped || SkillEquipManager.FindSlotBySkill(SkillId) >= 0 ||
            SkillIcon == null || _imgIcon == null) return;
        CreateDragClone(eventData);
    }

    private void CreateDragClone(PointerEventData eventData)
    {
        Transform parent = UISystem.Instance != null ? UISystem.Instance.DragLayer : null;
        if (parent == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            parent = canvas.transform;
        }

        _dragClone = new GameObject("SkillDragIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _dragClone.raycastTarget = false;   // 不挡落点射线
        _dragClone.sprite = SkillIcon;
        _dragClone.color = Color.white;

        RectTransform rt = (RectTransform)_dragClone.transform;
        rt.SetParent(parent, false);
        rt.SetAsLastSibling();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.sizeDelta = _imgIcon.rectTransform.rect.size;
        FollowDrag(eventData);
    }

    private void OnDrag(PointerEventData eventData)
    {
        FollowDrag(eventData);
    }

    private void FollowDrag(PointerEventData eventData)
    {
        if (_dragClone == null) return;
        Canvas canvas = _dragClone.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform rt = _dragClone.rectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            rt.anchoredPosition = local;
        }
    }

    private void OnEndDrag()
    {
        if (_dragClone != null)
        {
            Destroy(_dragClone.gameObject);
            _dragClone = null;
        }
    }

    #endregion

    private void OnDestroy()
    {
        _scaleTween?.Kill();
        if (_dragClone != null) Destroy(_dragClone.gameObject);
    }
}

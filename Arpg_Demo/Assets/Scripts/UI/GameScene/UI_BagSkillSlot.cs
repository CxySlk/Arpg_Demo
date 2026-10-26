using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包技能槽：显示一个技能，负责图标、装备状态和拖拽表现。
/// 拖拽只传递 sprite + 技能 id；是否允许拖拽由窗口设置的 <see cref="IsLearned"/> 决定。
/// </summary>
public class UI_BagSkillSlot : MonoBehaviour
{
    /// <summary>拖拽时的临时跟随图标（克隆）</summary>
    private Image _dragClone;

    /// <summary>本槽的技能 id（-1 = 无效/空）</summary>
    public int SkillId { get; private set; } = -1;

    /// <summary>是否已学习（未学习 → 不允许拖拽）</summary>
    [LabelText("是否已学习"), ShowInInspector, ReadOnly]
    public bool IsLearned { get; private set; }

    /// <summary>是否已装备到热栏（已装备 → 禁止拖拽并置灰提示）</summary>
    [LabelText("是否装备"), ShowInInspector, ReadOnly]
    public bool IsEquipped { get; private set; }

    /// <summary>只有已学习、未装备且存在图标时才允许拖拽。</summary>
    public bool CanDrag => IsLearned && !IsEquipped && SkillIcon != null;

    [LabelText("技能图标"), SerializeField] private Image _imgIcon;
    public Sprite SkillIcon => _imgIcon.sprite;

    [LabelText("图标边框"), SerializeField] private Image _imgBorder;
    [LabelText("技能按钮"), SerializeField] private Button _btnSlot;
    public Button BtnSlot => _btnSlot;

    private void Awake()
    {
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnBeginDrag, eventData => OnBeginDrag(eventData));
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnDrag, eventData => OnDrag(eventData));
        _imgBorder.AddEventListener<PointerEventData>(EventSystemType.OnEndDrag, _ => OnEndDrag());
    }

    public void SetInfo(int id, Sprite icon)
    {
        SkillId = id;
        SetIcon(icon);
    }

    public void SetIcon(Sprite icon)
    {
        _imgIcon.sprite = icon;
        _imgIcon.enabled = icon != null && !IsEquipped;
    }

    public void SetLearned(bool learned) => IsLearned = learned;

    /// <summary>设置装备状态：已装备 → 隐藏图标并禁止拖拽；未装备 → 恢复显示。</summary>
    public void SetEquipped(bool equipped)
    {
        if (IsEquipped == equipped) return;

        IsEquipped = equipped;
        _imgIcon.enabled = !equipped && _imgIcon.sprite != null;
        _imgIcon.color = equipped ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }

    #region 拖拽

    private void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag) return;
        CreateDragClone(eventData);
    }

    private void CreateDragClone(PointerEventData eventData)
    {
        if (_imgIcon.sprite == null) return;

        Transform parent = UISystem.Instance != null ? UISystem.Instance.DragLayer : null;
        if (parent == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            parent = canvas.transform;
        }

        _dragClone = new GameObject("SkillDragIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _dragClone.raycastTarget = false;
        _dragClone.sprite = _imgIcon.sprite;
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
        if (!CanDrag || _dragClone == null) return;
        FollowDrag(eventData);
    }

    private void FollowDrag(PointerEventData eventData)
    {
        if (_dragClone == null) return;

        Canvas canvas = _dragClone.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            _dragClone.rectTransform.anchoredPosition = local;
        }
    }

    private void OnEndDrag()
    {
        if (_dragClone == null) return;

        Destroy(_dragClone.gameObject);
        _dragClone = null;
    }

    #endregion

    private void OnDestroy()
    {
        if (_dragClone != null) Destroy(_dragClone.gameObject);
    }
}

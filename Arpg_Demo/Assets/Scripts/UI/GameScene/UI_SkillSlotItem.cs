using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 热栏技能槽：接收背包槽拖来的技能、显示技能图标与 CD 遮罩。
/// </summary>
public class UI_SkillSlotItem : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [LabelText("遮罩(CD)"), SerializeField] public Image imgMask;
    [LabelText("技能图标"), SerializeField] public Image imgIcon;

    [LabelText("槽位下标"), ShowInInspector, ReadOnly]
    public int SlotIndex { get; private set; } = -1;

    [LabelText("技能索引"), ShowInInspector, ReadOnly]
    private int skillID = -1;

    public int SkillID => skillID;

    private void Awake()
    {
        imgMask.fillAmount = 0f;
        imgMask.gameObject.SetActive(false);
    }

    public void BindSlotIndex(int index) => SlotIndex = index;

    public void SetIcon(Sprite skillIcon)
    {
        imgIcon.sprite = skillIcon;
        imgIcon.enabled = skillIcon;
    }

    public void EquipSkill(int newSkillId, Sprite icon)
    {
        skillID = newSkillId;
        SetIcon(icon);
        RefreshMask();
    }

    public void ClearSkill()
    {
        skillID = -1;
        SetIcon(null);
        RefreshMask();
    }

    /// <summary>重置遮罩显示：Awake 会关掉遮罩物体，CD 事件到来时再由 SetMaskFillAmount 打开。</summary>
    private void RefreshMask()
    {
        imgMask.fillAmount = 0f;
        imgMask.enabled = false;
        imgMask.gameObject.SetActive(false);
    }

    public void SetMaskFillAmount(float remainCd, float cdTime)
    {
        bool show = skillID >= 0 && cdTime > 0f && remainCd > 0f;

        // Awake 里把遮罩物体 SetActive(false) 了，这里必须同时恢复物体激活，否则只改 enabled 不会显示
        if (imgMask.gameObject.activeSelf != show)
            imgMask.gameObject.SetActive(show);

        imgMask.enabled = show;
        imgMask.fillAmount = show ? Mathf.Clamp01(remainCd / cdTime) : 0f;
    }

    public void OnDrop(PointerEventData eventData)
    {
        UI_BagSkillSlot source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponentInParent<UI_BagSkillSlot>()
            : null;

        if (source == null || source.IsEquipped || !source.CanDrag) return;

        if (SkillEquipManager.TryEquip(source.SkillId, SlotIndex, out _))
            EquipSkill(source.SkillId, source.SkillIcon);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillID < 0 || UISystem.GetWindow<UI_SkillBagWindow>() is not { IsShow: true }) return;

        SkillEquipManager.Unequip(SlotIndex);
        ClearSkill();
    }
}

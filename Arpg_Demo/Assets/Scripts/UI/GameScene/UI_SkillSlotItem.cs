using LLKFrame;
using Sirenix.OdinInspector;
using Skill;
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

    private void OnEnable()
    {
        EventCenter.Global.Subscribe(SkillEventKey.SkillCdTimerKey, OnSkillCdChanged);
    }

    private void OnDisable()
    {
        EventCenter.Global.UnSubscribe(SkillEventKey.SkillCdTimerKey, OnSkillCdChanged);
    }

    public void BindSlotIndex(int index) => SlotIndex = index;

    public void SetIcon(Sprite skillIcon)
    {
        imgIcon.sprite = skillIcon;
        imgIcon.enabled = skillIcon != null;
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

    private void RefreshMask()
    {
        imgMask.fillAmount = 0f;
        imgMask.gameObject.SetActive(skillID >= 0);
    }

    private void OnSkillCdChanged(int changedSkillId, float remainCd, float skillCd)
    {
        if (changedSkillId == skillID)
            SetMaskFillAmount(remainCd, skillCd);
    }

    public void SetMaskFillAmount(float remainCd, float cdTime)
    {
        imgMask.fillAmount = cdTime > 0f ? remainCd / cdTime : 0f;
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

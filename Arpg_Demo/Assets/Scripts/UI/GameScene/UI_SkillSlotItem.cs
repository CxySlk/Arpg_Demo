using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 热栏技能槽（装备栏）：**自带技能图标 Image**，接收背包槽拖来的技能只传 sprite + 技能 id；
/// 点击本槽可把技能返还背包。
/// </summary>
/// <remarks>
/// - 图标显示 = 本槽自己的 <see cref="imgIcon"/>（由拖拽落点或存档刷新赋值 sprite）；
/// - 遮罩（imgMask）作为技能 CD 覆盖层：**有技能才启用、无技能失活**，初始填充 0；
/// - 接收拖拽 = 装备：<see cref="SkillEquipManager.TryEquip"/> 记录数据 + 本槽显示来源技能 sprite；
/// - 点击本槽：若有技能则解除装备（清空图标、skillID 置 -1），数据交给 <see cref="SkillEquipManager"/>；
/// - 空槽 skillID = -1。
/// 注意：空槽要能接收 Drop/点击，需要槽上有常驻的射线目标（如背景图 raycastTarget），遮罩不是命中区。
/// </remarks>
public class UI_SkillSlotItem : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [LabelText("遮罩(CD)")] [SerializeField] public Image imgMask;
    [LabelText("技能图标")] [SerializeField] public Image imgIcon;

    /// <summary>本槽在热栏中的下标（由 UI_SkillSlots 初始化，写装备数据用）</summary>
    [LabelText("槽位下标")]
    [ShowInInspector, ReadOnly]
    public int SlotIndex { get; private set; } = -1;

    /// <summary>当前装备的技能 id（-1 = 空槽）</summary>
    [LabelText("技能索引")] [ShowInInspector] [ReadOnly]
    private int skillID = -1;

    /// <summary>当前装备的技能 id（-1 = 空槽），供外部读取</summary>
    public int SkillID => skillID;

    private void Awake()
    {
        // 遮罩初始：填充 0；无技能 → 失活（有技能时由 RefreshMask 启用）
        if (imgMask != null)
        {
            imgMask.fillAmount = 0f;
            imgMask.gameObject.SetActive(false);
        }
    }

    /// <summary>绑定本槽在热栏中的下标。</summary>
    public void BindSlotIndex(int index) => SlotIndex = index;

    /// <summary>设置本槽图标 sprite（null = 空槽不显示）。</summary>
    public void SetIcon(Sprite skillIcon)
    {
        if (imgIcon == null) return;
        imgIcon.sprite = skillIcon;
        imgIcon.enabled = skillIcon != null;
    }

    /// <summary>按技能状态刷新遮罩：有技能 → 启用（填充 0，等待 CD 覆盖）；无技能 → 失活。</summary>
    private void RefreshMask()
    {
        if (imgMask == null) return;
        imgMask.fillAmount = 0f;
        imgMask.gameObject.SetActive(skillID >= 0);
    }

    /// <summary>装备技能到本槽（数据 + 图标 + 遮罩）。</summary>
    public void ApplySkill(int newSkillId, Sprite icon)
    {
        skillID = newSkillId;
        SetIcon(icon);
        RefreshMask();
    }

    /// <summary>清空本槽（数据回 -1、清图标、遮罩失活）。</summary>
    public void ClearSkill()
    {
        skillID = -1;
        SetIcon(null);
        RefreshMask();
    }

    public void SetMaskFillAmount(float remainCD, float cdTime)
    {
        if (imgMask == null) return;
        imgMask.fillAmount = cdTime > 0 ? remainCD / cdTime : 0f;
    }

    #region 接收拖拽（背包技能装备上来，只传 sprite）

    public void OnDrop(PointerEventData eventData)
    {
        // 拖拽源必须是背包技能槽
        UI_BagSkillSlot source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponentInParent<UI_BagSkillSlot>()
            : null;
        if (source == null || source.IsEquipped) return;

        // 规则校验 + 记录（顶替时 SkillEquipManager.EquipChanged 会让背包刷新旧技能的置灰状态）
        if (SkillEquipManager.TryEquip(source.SkillId, SlotIndex, out _))
        {
            ApplySkill(source.SkillId, source.SkillIcon);
        }
    }

    #endregion

    #region 点击返还（解除装备）

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillID < 0 || UISystem.GetWindow<UI_SkillBagWindow>() is not { IsShow: true }) return; // 空槽点击无操作

        SkillEquipManager.Unequip(SlotIndex); // 解除装备数据（触发 EquipChanged → 背包/热栏刷新）
        ClearSkill(); // 清空本槽显示
    }

    #endregion
}
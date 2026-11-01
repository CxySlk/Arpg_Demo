using System.Collections;
using Data;
using LLKFrame;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;

/// <summary>
/// 热栏容器：绑定槽位下标、按存档恢复装备显示，并订阅装备变化保持同步。
/// </summary>
public class UI_SkillSlots : MonoBehaviour
{
    [LabelText("技能槽")] public UI_SkillSlotItem[] slotItems;

    private readonly SkillIconCache _iconCache = new();

    /// <summary>玩家技能事件中心（CD 事件发布在 SkillBrain.SkillEventCenter，不是 EventCenter.Global）</summary>
    private EventCenter _skillEvents;

    private void OnEnable()
    {
        for (int i = 0; i < slotItems.Length; i++)
            slotItems[i].BindSlotIndex(i);

        SkillEquipManager.EquipChanged += OnEquipChanged;
        BindSkillEvents();
        RefreshFromData();
    }

    private void OnDisable()
    {
        SkillEquipManager.EquipChanged -= OnEquipChanged;
        UnbindSkillEvents();
    }

    /// <summary>绑定玩家技能事件中心。重复调用安全：先解绑再绑定。</summary>
    public void BindSkillEvents()
    {
        UnbindSkillEvents();

        _skillEvents = PlayerController.Instance?.SkillBrain?.SkillEventCenter;
        if (_skillEvents == null) return;

        _skillEvents.Subscribe(SkillEventKey.SkillCdTimerKey, OnSkillCdChanged);
    }

    private void UnbindSkillEvents()
    {
        if (_skillEvents == null) return;

        _skillEvents.UnSubscribe(SkillEventKey.SkillCdTimerKey, OnSkillCdChanged);
        _skillEvents = null;
    }

    private void OnSkillCdChanged(int changedSkillId, float remainCd, float totalCd)
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            UI_SkillSlotItem item = slotItems[i];
            if (item != null && item.SkillID == changedSkillId)
                item.SetMaskFillAmount(remainCd, totalCd);
        }
    }

    private void OnDestroy()
    {
        _iconCache.ReleaseAll();
    }

    /// <summary>延迟一帧再刷新一次：场景启动早期 PlayerData 可能还没加载完。</summary>
    private IEnumerator Start()
    {
        yield return null;
        RefreshFromData();
    }

    private void OnEquipChanged(int slotIndex, int oldSkillId, int newSkillId)
    {
        RefreshFromData();
    }

    /// <summary>按存档装备数据刷新所有热栏槽。</summary>
    public void RefreshFromData()
    {
        if (DataManager.PlayerConfig == null || DataManager.PlayerData == null) return;

        for (int i = 0; i < slotItems.Length; i++)
        {
            UI_SkillSlotItem item = slotItems[i];
            int skillId = SkillEquipManager.GetSlotSkill(i);

            if (skillId >= 0 && DataManager.PlayerData.skillLearnData.IsLearnSkill(skillId))
                item.EquipSkill(skillId, _iconCache.Get(skillId));
            else
                item.ClearSkill();
        }
    }
}

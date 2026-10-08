using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 热栏（技能栏）容器：持有固定数量的 <see cref="UI_SkillSlotItem"/>，
/// 负责给每个槽绑定下标、按存档恢复装备显示，并订阅 <see cref="SkillEquipManager.EquipChanged"/> 保持同步。
/// </summary>
public class UI_SkillSlots : MonoBehaviour
{
    [LabelText("技能槽")] public UI_SkillSlotItem[] slotItems;

    private void OnEnable()
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null) continue;
            slotItems[i].BindSlotIndex(i);
        }
        SkillEquipManager.EquipChanged += OnEquipChanged;
        RefreshFromData();
    }

    private void OnDisable()
    {
        SkillEquipManager.EquipChanged -= OnEquipChanged;
    }

    /// <summary>延迟一帧再刷新一次：场景启动早期 PlayerData 可能还没加载完</summary>
    private IEnumerator Start()
    {
        yield return null;
        RefreshFromData();
    }
    
    private void OnEquipChanged(int slotIndex, int oldSkillId, int newSkillId)
    {
        RefreshFromData();
    }

    /// <summary>按存档装备数据刷新所有热栏槽：有技能 → 显示对应技能图标 + 记录 id；空槽 → 清空。</summary>
    public void RefreshFromData()
    {
        if (DataManager.PlayerConfig == null || DataManager.PlayerData == null) return;

        for (int i = 0; i < slotItems.Length; i++)
        {
            UI_SkillSlotItem item = slotItems[i];
            if (item == null) continue;

            int skillId = SkillEquipManager.GetSlotSkill(i);
            if (skillId >= 0 && skillId < DataManager.PlayerConfig.SkillConfigList.Count)
            {
                Sprite icon = DataManager.PlayerConfig.SkillConfigList[skillId].SkillIcon;
                item.ApplySkill(skillId, icon);
            }
            else
            {
                item.ClearSkill();
            }
        }
    }
}

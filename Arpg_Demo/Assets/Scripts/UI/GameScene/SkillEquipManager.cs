using System;
using System.Collections.Generic;

/// <summary>
/// 技能装备管理器（静态）：技能背包 ↔ 热栏槽的唯一数据入口。
/// 负责规则、持久化与变更广播。
/// </summary>
public static class SkillEquipManager
{
    /// <summary>装备关系变化：(slotIndex, 旧技能id(-1=原本为空), 新技能id(-1=被清空))</summary>
    public static event Action<int, int, int> EquipChanged;

    private static Dictionary<int, int> EquipDic =>
        DataManager.PlayerData != null ? DataManager.PlayerData.equipSlotDic : null;

    public static int GetSlotSkill(int slotIndex)
    {
        Dictionary<int, int> dic = EquipDic;
        return dic != null && dic.TryGetValue(slotIndex, out int skillId) ? skillId : -1;
    }

    /// <summary>查询某技能当前装备在哪个热栏槽（-1 = 未装备）。</summary>
    public static int FindSlotBySkill(int skillId)
    {
        if (skillId < 0) return -1;

        Dictionary<int, int> dic = EquipDic;
        if (dic == null) return -1;

        foreach (KeyValuePair<int, int> kv in dic)
        {
            if (kv.Value == skillId) return kv.Key;
        }

        return -1;
    }

    /// <summary>尝试把已学习的技能装备到指定热栏槽。</summary>
    public static bool TryEquip(int skillId, int slotIndex, out int replacedSkill)
    {
        replacedSkill = -1;
        if (skillId < 0 || slotIndex < 0) return false;

        if (DataManager.PlayerData == null ||
            !DataManager.PlayerData.skillLearnData.IsLearnSkill(skillId))
            return false;

        // 一个技能只能装备到一个热栏槽
        int existedSlot = FindSlotBySkill(skillId);
        if (existedSlot >= 0 && existedSlot != slotIndex) return false;

        Dictionary<int, int> dic = EquipDic;
        if (dic == null) return false;

        int old = -1;
        dic.TryGetValue(slotIndex, out old);
        if (old == skillId) return true;

        if (old >= 0) replacedSkill = old;
        dic[slotIndex] = skillId;
        SaveAndNotify(slotIndex, old, skillId);
        return true;
    }

    /// <summary>解除热栏槽装备，返回被移除的技能 id（-1 = 空槽）。</summary>
    public static int Unequip(int slotIndex)
    {
        Dictionary<int, int> dic = EquipDic;
        if (dic == null || !dic.TryGetValue(slotIndex, out int skillId)) return -1;

        dic.Remove(slotIndex);
        SaveAndNotify(slotIndex, skillId, -1);
        return skillId;
    }

    private static void SaveAndNotify(int slotIndex, int oldSkillId, int newSkillId)
    {
        DataManager.SavePlayerData();
        EquipChanged?.Invoke(slotIndex, oldSkillId, newSkillId);
    }
}

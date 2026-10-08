using System;
using System.Collections.Generic;

/// <summary>
/// 技能装备管理器（静态）：技能背包技能 ↔ 热栏槽之间"装备关系"的唯一入口。
/// 负责规则（一个技能只能装备到一个热栏槽）、数据持久化（PlayerData.equipSlotDic）与变更广播。
/// </summary>
public static class SkillEquipManager
{
    /// <summary>装备关系变化：(slotIndex, 旧技能id(-1=原本为空), 新技能id(-1=被清空))</summary>
    public static event Action<int, int, int> EquipChanged;

    /// <summary>当前玩家装备字典（槽下标 → 技能id），数据未就绪返回 null。</summary>
    private static Dictionary<int, int> EquipDic
    {
        get
        {
            if (DataManager.PlayerData == null) return null;
            if (DataManager.PlayerData.equipSlotDic == null)
                DataManager.PlayerData.equipSlotDic = new Dictionary<int, int>();
            return DataManager.PlayerData.equipSlotDic;
        }
    }

    /// <summary>读取某个热栏槽装备的技能 id（-1 = 空槽）。</summary>
    public static int GetSlotSkill(int slotIndex)
    {
        Dictionary<int, int> dic = EquipDic;
        if (dic == null || !dic.TryGetValue(slotIndex, out int skillId)) return -1;
        return skillId;
    }

    /// <summary>查询某技能当前装备在哪个热栏槽（-1 = 未装备）。</summary>
    public static int FindSlotBySkill(int skillId)
    {
        if (skillId < 0) return -1;
        Dictionary<int, int> dic = EquipDic;
        if (dic == null) return -1;
        foreach (KeyValuePair<int, int> kv in dic)
            if (kv.Value == skillId)
                return kv.Key;
        return -1;
    }

    /// <summary>
    /// 尝试把技能装备到指定热栏槽。
    /// </summary>
    /// <param name="skillId">要装备的技能 id。</param>
    /// <param name="slotIndex">目标热栏槽下标。</param>
    /// <param name="replacedSkill">目标槽原先的技能 id（-1 = 原本为空槽）。</param>
    /// <returns>true=装备成功；false=技能已装备到其它槽、或数据不可用。</returns>
    public static bool TryEquip(int skillId, int slotIndex, out int replacedSkill)
    {
        replacedSkill = -1;
        if (skillId < 0 || slotIndex < 0) return false;

        // 规则：一个技能只能装备到一个热栏槽
        int existedSlot = FindSlotBySkill(skillId);
        if (existedSlot >= 0 && existedSlot != slotIndex) return false;

        Dictionary<int, int> dic = EquipDic;
        if (dic == null) return false;

        // 同一技能拖回自己槽 = 无操作
        if (dic.TryGetValue(slotIndex, out int old) && old == skillId)
            return true; 

        if (old >= 0) replacedSkill = old; // 目标槽被占用 → 旧技能待退回
        dic[slotIndex] = skillId;
        SaveAndNotify(slotIndex, old, skillId);
        return true;
    }

    /// <summary>解除热栏槽装备（把技能拿下来放回背包）。返回被移除的技能 id（-1 = 空槽，无操作）。</summary>
    public static int Unequip(int slotIndex)
    {
        Dictionary<int, int> dic = EquipDic;
        if (dic == null || !dic.TryGetValue(slotIndex, out int skillId)) return -1;
        dic.Remove(slotIndex);
        SaveAndNotify(slotIndex, skillId, -1);
        return skillId;
    }

    /// <summary>持久化并广播装备变化（存档不可用时不落盘）。</summary>
    private static void SaveAndNotify(int slotIndex, int oldSkillId, int newSkillId)
    {
        if (DataManager.HasArchive) DataManager.SavePlayerData();
        EquipChanged?.Invoke(slotIndex, oldSkillId, newSkillId);
    }
}
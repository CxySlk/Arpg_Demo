using System.Collections.Generic;
using MessagePack;

namespace Skill.Data
{
    /// <summary>
    /// 技能学习数据（存档用）。记录玩家已学会的技能 ID 集合。
    /// </summary>
    /// <remarks>
    /// 通过 MessagePack 序列化持久化到存档；技能 ID 为 Luban 技能表(<c>cfg.TbSkill</c>) 里的技能 id。
    /// 运行时由 SkillBrain 据此初始化/加载已学习技能。
    /// </remarks>
    [MessagePackObject]
    public class SkillLearnData
    {
        /// <summary>已学习技能 ID 集合（技能 ID = Luban 技能表里的技能 id）。</summary>
        [Key(0)] public HashSet<int> skillIdSet = new();

        /// <summary>
        /// 学习一个技能。
        /// </summary>
        /// <param name="skillId">技能 id（对应 Luban 技能表 cfg.TbSkill）。</param>
        /// <remarks>仅将技能 ID 加入集合；不包含重复判断，调用方可自行用返回值/Contains 判断。</remarks>
        public void LearnSkill(int skillId)
        {
            skillIdSet.Add(skillId);
        }

        /// <summary>
        /// 是否学习过该技能
        /// </summary>
        /// <param name="skillId"></param>
        /// <returns></returns>
        public bool IsLearnSkill(int skillId)
        {
            return skillIdSet.Contains(skillId);
        }
    }
}
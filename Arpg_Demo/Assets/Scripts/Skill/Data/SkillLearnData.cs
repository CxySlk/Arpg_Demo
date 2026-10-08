using System.Collections.Generic;
using MessagePack;

namespace Skill.Data
{
    /// <summary>
    /// 技能学习数据（存档用）。记录玩家已学会的技能 ID 集合。
    /// </summary>
    /// <remarks>
    /// 通过 MessagePack 序列化持久化到存档；技能 ID 用其在 <c>SkillConfigList</c> 配置列表中的索引表示。
    /// 运行时由 SkillBrain 据此初始化/加载已学习技能。
    /// </remarks>
    [MessagePackObject]
    public class SkillLearnData
    {
        /// <summary>已学习技能 ID 集合（技能 ID = 在技能配置列表中的索引）。</summary>
        [Key(0)] public HashSet<int> skillIdSet = new();

        /// <summary>
        /// 学习一个技能。
        /// </summary>
        /// <param name="skillIndex">技能在 SkillConfigList 中的索引（即技能 ID）。</param>
        /// <remarks>仅将技能 ID 加入集合；不包含重复判断，调用方可自行用返回值/Contains 判断。</remarks>
        public void LearnSkill(int skillIndex)
        {
            skillIdSet.Add(skillIndex);
        }
    }
}
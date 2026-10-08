using System.Collections.Generic;
using LLKFrame;
using Skill.Data;

namespace Skill
{
    public class Enemy_SkillBrain : SkillBrainBase
    {
        public void InitForConfig(List<SkillConfig> skillConfigs)
        {
            // 重复初始化(重开一局/对象复用)时先清掉旧行为,避免旧行为继续逐帧驱动、以及字典重复键异常
            ClearBehaviors();

            skillEventCenter = new EventCenter();
            skillEventCenter.ClearAll();

            this.skillConfigs = skillConfigs;

            for (int skillID = 0; skillID < skillConfigs.Count; skillID++)
            {
                SkillBehaviorBase skillBehavior = skillConfigs[skillID].SkillBehavior.DeepCopy();
                skillBehavior.Init(this, skillConfigs[skillID], skillID);
                skillBehavior.Start();
                behaviorDic.Add(skillID, skillBehavior);
            }
        }
    }
}
using System.Collections.Generic;
using LLKFrame;
using Skill.Data;

namespace Skill
{
    public class Player_SkillBrain : SkillBrainBase
    {
        protected SkillLearnData learnSkillData;

        public void InitForConfig(List<SkillConfig> skillConfigs, SkillLearnData learnSkills)
        {
            // 重复初始化(换角色/重开一局)时先清掉旧行为,避免旧行为继续逐帧驱动、以及字典重复键异常
            ClearBehaviors();

            skillEventCenter = new EventCenter();
            skillEventCenter.ClearAll();

            this.skillConfigs = skillConfigs;
            learnSkillData = learnSkills;

            foreach (int skillID in learnSkills.skillIdSet)
            {
                SkillBehaviorBase skillBehavior = skillConfigs[skillID].SkillBehavior.DeepCopy();
                skillBehavior.Init(this, skillConfigs[skillID], skillID);
                skillBehavior.Start();
                behaviorDic.Add(skillID, skillBehavior);
            }
        }

        public void LearnSkill(int skillID)
        {
            if (learnSkillData.skillIdSet.Add(skillID))
            {
                SkillBehaviorBase skillBehavior = skillConfigs[skillID].SkillBehavior.DeepCopy();
                skillBehavior.Init(this, skillConfigs[skillID], skillID);
                skillBehavior.Start();
                behaviorDic.Add(skillID, skillBehavior);
            }

            DataManager.SavePlayerData();
        }
    }
}
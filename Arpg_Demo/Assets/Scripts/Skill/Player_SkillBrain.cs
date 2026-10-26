using System.Collections.Generic;
using LLKFrame;
using Skill.Data;

namespace Skill
{
    public class Player_SkillBrain : SkillBrainBase
    {
        protected SkillLearnData learnSkillData;

        /// <summary>玩家拥有的技能 id（可能需等级解锁；默认技能初始化即学）。</summary>
        protected IReadOnlyList<int> ownedSkills;

        /// <summary>按玩家配置 + 存档里的已学技能 id 装配技能行为。</summary>
        public void InitForConfig(cfg.PlayerConfig config, SkillLearnData learnSkills)
        {
            ClearBehaviors();

            skillEventCenter = new EventCenter();
            skillEventCenter.ClearAll();

            learnSkillData = learnSkills;
            ownedSkills = config.OwnedSkills;

            foreach (int skillId in learnSkills.skillIdSet)
                AddLearnedSkill(skillId);
        }

        /// <summary>学习技能：同时写入存档并确保运行时行为已注册。</summary>
        public bool LearnSkill(int skillId)
        {
            if (learnSkillData == null) return false;

            learnSkillData.skillIdSet.Add(skillId);
            if (!behaviorDic.ContainsKey(skillId))
                AddLearnedSkill(skillId);

            DataManager.SavePlayerData();
            return behaviorDic.ContainsKey(skillId);
        }

        private void AddLearnedSkill(int skillId)
        {
            SkillBehaviorBase behavior = CreateBehavior(skillId);
            if (behavior != null)
                behaviorDic[skillId] = behavior;
        }
    }
}

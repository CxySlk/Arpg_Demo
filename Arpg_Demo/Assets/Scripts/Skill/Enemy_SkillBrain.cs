using LLKFrame;

namespace Skill
{
    public class Enemy_SkillBrain : SkillBrainBase
    {
        /// <summary>按敌人配置"拥有的技能 id"装配技能行为（初始化即全部学会）。</summary>
        public void InitForConfig(cfg.EnemyConfig config)
        {
            // 重复初始化(重开一局/对象复用)时先清掉旧行为,避免旧行为继续逐帧驱动、以及字典重复键异常
            ClearBehaviors();

            skillEventCenter = new EventCenter();
            skillEventCenter.ClearAll();

            foreach (int skillId in config.OwnedSkills)
            {
                SkillBehaviorBase behavior = CreateBehavior(skillId);
                if (behavior != null)
                    behaviorDic[skillId] = behavior;
            }
        }
    }
}

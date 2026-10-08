namespace Skill
{
    public class NormalAttackBehavior : SkillBehaviorBase
    {
        public override SkillBehaviorBase DeepCopy()
        {
            return new NormalAttackBehavior();
        }
        
        public override void PlaySkill()
        {
            base.PlaySkill();
            isAbilityInterval = false;
        }
    }
}


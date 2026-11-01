namespace Skill
{
    public class SkillBehavior : SkillBehaviorBase
    {
        public override SkillBehaviorBase DeepCopy()
        {
            return new SkillBehavior();
        }
    }
}
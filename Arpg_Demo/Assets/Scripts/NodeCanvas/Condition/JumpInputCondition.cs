using NodeCanvas.Framework;

public class JumpInputCondition : ConditionTask
{
    protected override bool OnCheck()
    {
        if (!InputManager.JumpInput) return false;

        // 技能在播时：只有在技能行为解锁"输入打断"后才允许跳跃打断（前摇/霸体期不跳）
        PlayerController playerController = agent.GetComponent<PlayerController>();
        return playerController == null
            || playerController.SkillBrain == null
            || playerController.SkillBrain.CanInputInterruptCurrentSkill;
    }
}

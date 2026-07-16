using NodeCanvas.Framework;

public class JumpInputCondition : ConditionTask
{
    protected override bool OnCheck()
    {
        return InputManager.JumpInput;
    }
}

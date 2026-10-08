using NodeCanvas.Framework;

public class IsGroundedCondition : ConditionTask
{
    private IGroundCondition  _groundCondition;

    protected override string OnInit()
    {
        _groundCondition = agent.GetComponent<IGroundCondition>();
        return null;
    }

    protected override bool OnCheck()
    {
        return _groundCondition.IsGrounded;
    }
}

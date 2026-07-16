using NodeCanvas.Framework;

public class MoveInputCondition : ConditionTask
{
    protected override bool OnCheck()
    {
        return InputManager.MoveInput != UnityEngine.Vector2.zero;
    }
}

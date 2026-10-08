using NodeCanvas.Framework;
using UnityEngine;

public class MoveInputCondition : ConditionTask
{
    protected override bool OnCheck()
    {
        return InputManager.MoveInput != Vector2.zero;
    }
}

using ParadoxNotion.Design;

[Category("Player/GroundState")]
public class Player_MoveState : Player_GroundState
{
    protected override void OnEnter()
    {
        PlayerController.PlaySingleAnimation("Move", OnRootMotion, PlayerConfig.MoveSpeed);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // 1.计算目标旋转值
        PlayerController.Rotate(MoveInput);
    }
}

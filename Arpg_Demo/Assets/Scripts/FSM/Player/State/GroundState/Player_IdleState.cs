using ParadoxNotion.Design;

[Category("Player/GroundState")]
public class Player_IdleState : Player_GroundState
{
    protected override void OnEnter() => PlayerController.PlaySingleAnimation("Idle", null);
}

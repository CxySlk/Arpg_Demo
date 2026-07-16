using ParadoxNotion.Design;
using UnityEngine;

public class Player_JumpAbilityState : PlayerAbilityStateBase
{
    [Name("跳跃动画名")]
    public string jumpName;

    private float jumpStartTime;
    private float minJumpDuration = 0.966f;

    protected override void OnEnter()
    {
        jumpStartTime = Time.time;
        PlayerController.PlaySingleAnimation(jumpName, OnRootMotion);
    }

    protected override void OnUpdate()
    {
        if (MoveInput != Vector2.zero)
        {
            PlayerController.Move(MoveInput, PlayerConfig.InAirMoveSpeed);
            PlayerController.Rotate(MoveInput);
        }

        if (Time.time - jumpStartTime > minJumpDuration && CharacterController.velocity.y <= 0)
            Finish();
    }
}

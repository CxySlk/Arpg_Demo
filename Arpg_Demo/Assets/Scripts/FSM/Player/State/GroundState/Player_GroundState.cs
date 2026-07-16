using NodeCanvas.StateMachines;
using ParadoxNotion.Design;
using UnityEngine;

[Category("Player/GroundState")]
public class Player_GroundState : PlayerStateBase
{
    private float ungroundedTimer;

    protected override void OnEnter()
    {
        ungroundedTimer = 0;
    }

    protected override void OnUpdate()
    {
        // 模拟重力
        CharacterController.Move(Vector3.up * -9.8f * Time.deltaTime);

        if (!IsGrounded)
        {
            ungroundedTimer += Time.deltaTime;
            if (ungroundedTimer > 0.1f)
            {
                ungroundedTimer = 0;
                (graph as FSM).EnterState(Player_InAirState, FSM.TransitionCallMode.Normal);
            }
            return;
        }
        else if (JumpInput) (graph as FSM).EnterState(Player_JumpAbilityState, FSM.TransitionCallMode.Normal);

        ungroundedTimer = 0;
    }
}

using Animancer;
using GAS;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using ParadoxNotion.Design;
using UnityEngine;

public class Player_JumpAbilityState : PlayerAbilityStateBase
{
    [Name("跳跃速度"), RequiredField] public BBParameter<float> jumpSpeed = new(1f);
    [Name("空中移动速度倍率"), RequiredField] public BBParameter<float> inAirSpeed = new(1f);

    private Attribute _moveSpeedAttr;
    private AnimancerState _jumpState;

    protected override void OnInit()
    {
        base.OnInit();
        Asc.TryGetAttrValue(AttributeType.MoveSpeed, out _moveSpeedAttr);
    }

    protected override void OnEnter()
    {
        playerController.SetRootMotion(OnRootMotion);
        _jumpState = playerController.Play("Jump");
        _jumpState.OwnedEvents.OnEnd = Finish;
    }

    protected override void OnExit()
    {
        base.OnExit();
        if (_jumpState != null && _jumpState.HasOwnedEvents) _jumpState.OwnedEvents.OnEnd = null;
        _jumpState = null;
    }

    protected override void OnUpdate()
    {
        if (InputManager.MoveInput != Vector2.zero)
            playerController.Move(InputManager.MoveInput, _moveSpeedAttr.CurValue * inAirSpeed.value);
    }

    private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        => playerController.Move(deltaPos * jumpSpeed.value);
}
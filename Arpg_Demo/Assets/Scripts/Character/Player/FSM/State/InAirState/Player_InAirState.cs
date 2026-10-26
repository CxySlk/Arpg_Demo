using Animancer;
using cfg;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("Player")]
public class Player_InAirState : PlayerStateBase
{
    private enum InAirState
    {
        Fall, // 下落
        End // 落地
    }
    [Name("空中的移动速度倍率"), RequiredField] public BBParameter<float> inAirMoveSpeed = new(1f);
    [Name("下落速度"), RequiredField] public BBParameter<float> fallSpeed = new(10f);

    private Attribute _moveSpeedAttr;

    private InAirState _inAirState = InAirState.Fall;
    private AnimancerState _landState;

    protected override void OnInit()
    {
        base.OnInit();
        Asc.TryGetAttrValue(EAttributeType.MoveSpeed, out _moveSpeedAttr);
    }

    protected override void OnEnter()
    {
        // 下落动画播放
        playerController.Play("InAir_Loop");
        _inAirState = InAirState.Fall;
    }

    protected override void OnUpdate()
    {
        if (_inAirState != InAirState.Fall) return;

        // 接地了，播放落地动画，动画结束节点结束（Finish结束后会切换到GroundState，Graph有切换条件）
        if (playerController.IsGrounded)
        {
            _inAirState = InAirState.End;
            _landState = playerController.Play("InAir_End");
            if (_landState != null) _landState.OwnedEvents.OnEnd = Finish;
        }

        // 空中位移
        if (InputManager.MoveInput != Vector2.zero)
        {
            playerController.Move(InputManager.MoveInput, inAirMoveSpeed.value * _moveSpeedAttr.CurValue);
        }

        // 重力
        playerController.Move(Vector3.down * (Time.deltaTime * fallSpeed.value));
    }

    protected override void OnExit()
    {
        base.OnExit();
        if (_landState != null && _landState.HasOwnedEvents) _landState.OwnedEvents.OnEnd = null;
        _landState = null;
    }
}

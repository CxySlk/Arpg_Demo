using System.Collections.Generic;
using Animancer;
using cfg;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("Player/GroundState")]
public class Player_MoveState : Player_GroundState
{
    private Attribute _moveSpeedAttr;
    private bool _isGround;

    public List<AnimationAudioEntry> audioEntries = new();

    private LinearMixerState _state;

    private bool isAccelerate;

    protected override void OnInit()
    {
        base.OnInit();
        Asc.TryGetAttrValue(EAttributeType.MoveSpeed, out _moveSpeedAttr);
    }

    protected override void OnEnter()
    {
        _state = playerController.Play("Move") as LinearMixerState;
        AnimationAudioHelper.Bind(_state, audioEntries, graphAgent.transform);
        playerController.SetRootMotion(OnRootMotion);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (isAccelerate && !InputManager.AccelerateInput)
        {
            isAccelerate = false;
            _state.Parameter = 1;
        }
        else if (!isAccelerate && InputManager.AccelerateInput)
        {
            isAccelerate = true;
            _state.Parameter = 2;
        }

        // 1.计算目标旋转值
        playerController.Rotate(InputManager.MoveInput);
    }

    private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        playerController.Move(deltaPos * _moveSpeedAttr.CurValue);
    }
}
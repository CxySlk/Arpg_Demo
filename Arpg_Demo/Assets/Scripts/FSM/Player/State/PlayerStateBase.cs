using LLKFrame;
using NodeCanvas.StateMachines;
using System.Linq;
using UnityEngine;

public abstract class PlayerStateBase : FSMState
{
    #region 组件相关
    protected static AnimationController AnimController { get; private set; }
    protected static PlayerController PlayerController { get; private set; }
    protected static CharacterController CharacterController { get; private set; }
    #endregion
    #region 配置相关
    protected static PlayerConfig PlayerConfig => PlayerController.PlayerConfig;
    #endregion
    #region 输入相关
    protected static Vector2 MoveInput => InputManager.MoveInput;
    protected static bool JumpInput => InputManager.JumpInput;
    protected static bool LeftAttackInput => InputManager.LeftAttackInput;
    protected static bool RightAttackInput => InputManager.RightAttackInput;
    #endregion
    #region 状态相关
    protected bool IsRunning => status == NodeCanvas.Framework.Status.Running;
    protected static bool IsGrounded => CharacterController.isGrounded;
    protected static Player_InAirState Player_InAirState { get; private set; }
    protected static Player_GroundState PlayerGroundState { get; private set; }
    protected static Player_JumpAbilityState Player_JumpAbilityState { get; private set; }
    #endregion
    protected override void OnInit()
    {
        if (AnimController == null) AnimController = graph.agent.GetComponent<AnimationController>();
        if (PlayerController == null) PlayerController = graph.agent.GetComponent<PlayerController>();
        if (CharacterController == null) CharacterController = graph.agent.GetComponent<CharacterController>();
        if (Player_InAirState == null) Player_InAirState = graph.GetAllNodesOfType<Player_InAirState>().FirstOrDefault(node => node.GetType() == typeof(Player_InAirState));
        if (PlayerGroundState == null) PlayerGroundState = graph.GetAllNodesOfType<Player_GroundState>().FirstOrDefault(node => node.GetType() == typeof(Player_GroundState));
        if (Player_JumpAbilityState == null) Player_JumpAbilityState = graph.GetAllNodesOfType<Player_JumpAbilityState>().FirstOrDefault(node => node.GetType() == typeof(Player_JumpAbilityState));
    }

    protected static void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        CharacterController.Move(deltaPos);
        CharacterController.transform.Rotate(deltaPos);
    }

    protected void TransitionState(FSMState state, FSM.TransitionCallMode mode = FSM.TransitionCallMode.Normal) => (graph as FSM).EnterState(state, mode);
}

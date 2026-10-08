using NodeCanvas.StateMachines;
using GAS;

public abstract class PlayerStateBase : FSMState
{
    protected PlayerController playerController;
    protected AbilitySystemComponent Asc => playerController.Asc;
    protected PlayerConfig PlayerConfig => playerController.PlayerConfig;

    protected override void OnInit()
    {
        playerController = graph.agent.GetComponent<PlayerController>();
    }

    protected override void OnExit()
    {
        // 退出状态统一清掉根运动回调:避免下一状态(技能/受控等不播动画的状态)还残留旧回调
        playerController.SetRootMotion(null);
    }
}
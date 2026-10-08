using ParadoxNotion.Design;
using Skill;
using UnityEngine;

/// <summary>
/// 技能播放状态（通用，每段技能独立进出）。
/// </summary>
/// <remarks>
/// 技能段播放期间锁在技能状态内，移动输入不会切到 MoveState（从而不再顶掉技能动画）；
/// 本状态在 OnUpdate 里轮询当前技能行为的段级标志：
/// 1. 前摇已结束（身上没有前摇标签）且玩家按移动 → 打断当前段并回地面；
/// 2. 段自然播完（AbilityPlayer 帧走完） → 回地面。
/// 两种退出都走 OnFinish 连接回 GroundState，由 Ground/Move/Idle 的既有移动条件自动路由。
/// 进入方式：PlayerController 成功释放技能后调用 Fsm.TriggerState(StateName)。
/// </remarks>
[Category("Player/AbilityState")]
[Name(nameof(Player_SkillState))]
public class Player_SkillState : PlayerStateBase
{
    /// <summary>FSM 图内该状态的节点名（= 类名），供 PlayerController 代码切入。</summary>
    public const string StateName = nameof(Player_SkillState);

    protected override void OnUpdate() 
    {
        // 当前没有播放中的技能行为（已取消/被清空）→ 直接退出
        SkillBehaviorBase behavior = playerController.SkillBrain.CurrentBehavior;
        if (behavior == null)
        {
            Finish();
            return;
        }

        // 当前段结束（自然播完 / 被输入打断）→ 回地面。
        // 输入打断的判定与执行都归技能行为，状态只关注段结束。
        if (behavior.IsCurrentAbilityDone)
        {
            Finish();
        }
    }
}

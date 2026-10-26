public partial class PlayerController
{
    #region 受控状态（眩晕/受击等"失去控制"的表现：由动画 Cue 驱动 → 状态机切到受控状态）

    /// <summary>受控开始:意外打断当前技能片段、停技能输入。</summary>
    protected override void OnControlledEnter()
    {
        skillBrain.UnexpectedInterruptAll(); // 受控=意外打断(片段级),不整套结束;后续连段由技能行为决定
        StopSkillInputCheck(); // 也不再响应技能输入(否则会切进技能状态)
    }

    /// <summary>受控结束:恢复技能输入。</summary>
    protected override void OnControlledExit() => StartSkillInputCheck();

    /// <summary>切到状态机的受控状态。已在受控状态则不重复切,避免 EnterState 复位受控。</summary>
    protected override void TriggerControlledState()
    {
        if (fsm.currentRootStateName != Player_ControlledState.StateName)
            fsm.TriggerState(Player_ControlledState.StateName);
    }

    #endregion
}

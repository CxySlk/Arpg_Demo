using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy/State")]
[Name(nameof(Enemy_ControlledState))]
[Description("受控状态（眩晕/受击等\"失去控制\"的表现）：停移动、打断技能；时长由动画 Cue 驱动（等于动画长度），重复进入只延长时间。外部打断 = Fsm.TriggerState(\"Enemy_ControlledState\")")]
public class Enemy_ControlledState : EnemyStateBase
{
    /// <summary>状态名（= 类名），供外部 TriggerState 使用。</summary>
    public const string StateName = nameof(Enemy_ControlledState);

    [LabelText("兜底受控时长(秒)"), Tooltip("没有动画驱动时（例如手工 TriggerState）的受控时长")]
    public float fallbackTime = 0.3f;

    protected override void OnEnter()
    {
        // 正常路径：动画 Cue 已经在控制器里按动画长度置位了受控与时间，这里只做兜底
        if (!enemyController.IsControlled)
            enemyController.EnterControlled(fallbackTime);
    }

    protected override void OnUpdate()
    {
        // 受控时间到点（控制器自动退出）或死亡 → 结束本状态，由连线回战斗状态
        if (enemyController.IsDead || !enemyController.IsControlled)
            Finish();
    }

    protected override void OnExit()
    {
        enemyController.ExitControlled();
    }
}

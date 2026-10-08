using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("受控表现")]
[Description("【已被外层状态机的 Enemy_ControlledState 取代,保留备用】让敌人进入受控状态(眩晕/受击等失去控制),控制器时长到点自动退出")]
public class Enemy_Controlled : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("受控时长(秒)"), Tooltip("<= 0 时用下面的兜底时长;动画驱动的受控由动画 Cue 负责,不走这个节点")]
    public BBParameter<float> duration = new(0f);

    [LabelText("兜底时长(秒)")]
    public float fallbackTime = 0.3f;

    protected override void OnExecute()
    {
        EnemyController controller = enemyController.value;
        if (controller == null)
        {
            EndAction(false);
            return;
        }

        controller.EnterControlled(duration.value > 0f ? duration.value : fallbackTime);

        // 被"不允许受控"的规则挡下(例如技能前摇霸体) → 这个节点直接失败
        if (!controller.IsControlled) EndAction(false);
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;

        // 受控时长由控制器自己管(再次受击只会延长时间),这里只等它结束
        if (controller == null || controller.IsDead || !controller.IsControlled)
            EndAction(true);
    }

    /// <summary>正常结束 or 被更高优先级分支打断,都要退出受控,否则敌人会被永久锁在受控状态。</summary>
    protected override void OnStop(bool interrupted)
    {
        enemyController.value?.ExitControlled();
    }
}

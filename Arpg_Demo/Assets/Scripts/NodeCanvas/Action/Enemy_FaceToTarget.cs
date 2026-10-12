using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("转向目标")]
[Description("持续转向目标(站住不动、不切动画):朝向进入容差后返回 Success;超时或没有目标返回 Failure。常放在“放技能”前,保证出手时已经面向玩家")]
public class Enemy_FaceToTarget : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("朝向容差(度)"), Tooltip("自身前方与目标方向的夹角小于它就视为已面向目标")]
    public BBParameter<float> maxAngle = new(45f);

    [LabelText("最长转向时间(秒)"), Tooltip("超过这么久还没转到位就按成功结束,避免卡住")]
    public BBParameter<float> maxTime = new(1f);

    private float _startTime;

    protected override void OnExecute()
    {
        _startTime = Time.time;
        // 纯转向:朝向由代码控制
        EnemyController controller = enemyController.value;
        if (controller != null) controller.NavMeshAgent.updateRotation = false;
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;
        if (!controller || !controller.Target) { EndAction(false); return; }

        controller.FaceTargetOnly(controller.Target);

        if (controller.GetTargetAngle() <= maxAngle.value)
        {
            EndAction(true);
            return;
        }

        // 超时还转不到位就判失败，交由上层决定(别再"超时也算成功"导致没面向就出招)
        if (Time.time - _startTime >= maxTime.value)
            EndAction(false);
    }
}

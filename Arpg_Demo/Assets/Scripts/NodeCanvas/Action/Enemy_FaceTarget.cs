using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("盯着目标")]
[Description("站着不动、持续朝向目标、播待机动画,持续 duration 秒后返回 Success(被打断则立即停止移动)")]
public class Enemy_FaceTarget : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("持续时间(秒)")]
    public BBParameter<float> duration = new(1.5f);

    private float _endTime;

    protected override void OnExecute()
    {
        EnemyController controller = enemyController.value;
        if (controller == null) { EndAction(false); return; }

        // 站定盯人:朝向由代码控制
        controller.NavMeshAgent.updateRotation = false;
        _endTime = Time.time + duration.value;
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;
        if (controller == null || !controller.Target) { EndAction(false); return; }

        controller.StandAndFace(controller.Target);

        if (Time.time >= _endTime) EndAction(true);
    }

    protected override void OnStop()
    {
        EnemyController controller = enemyController.value;
        if (controller != null) controller.StopChase();
    }
}

using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("环绕目标")]
[Description("以目标为圆心保持 radius 距离绕圈走(顺/逆时针随机),持续 duration 秒后返回 Success(被打断则立即停止移动)")]
public class Enemy_CircleTarget : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("环绕半径")]
    public BBParameter<float> radius = new (3f);

    [LabelText("环绕速度")]
    public BBParameter<float> speed = new(2.5f);

    [LabelText("持续时间(秒)")]
    public BBParameter<float> duration = new (3f);

    private float _endTime;
    private int _direction;

    protected override void OnExecute()
    {
        // 每次环绕随机方向,避免一直朝同一边转
        _direction = Random.value < 0.5f ? 1 : -1;
        _endTime = Time.time + duration.value;
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;
        if (!controller.Target) { EndAction(false); return; }

        controller.CircleTarget(controller.Target, radius.value, speed.value, _direction);

        if (Time.time >= _endTime) EndAction(true);
    }
}

using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;

[Category("Enemy")]
[Name("原地待机")]
[Description("停止移动并播放待机动画(持续 Running)")]
public class Enemy_Idle : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    protected override void OnExecute()
    {
        EnemyController controller = enemyController.value;
        if (controller == null)
        {
            EndAction(false);
            return;
        }

        controller.Idle();
    }
}

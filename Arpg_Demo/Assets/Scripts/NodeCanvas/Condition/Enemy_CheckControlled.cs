using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;

[Category("Enemy")]
[Name("检查受控")]
[Description("敌人是否处于受控状态(眩晕/受击等失去控制;由动画 Cue 驱动的受控状态置位)")]
public class Enemy_CheckControlled : ConditionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    protected override bool OnCheck()
    {
        EnemyController controller = enemyController.value;
        return controller.IsControlled;
    }
}

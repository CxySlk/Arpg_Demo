using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("释放技能")]
[Description("让控制器释放技能并等它播完:当前技能片段播完返回 Success(多段技能只看当前段,不等整套连段)")]
public class Enemy_CastSkill : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("技能ID"), Tooltip("对应 EnemyConfig.SkillConfigList 里的下标")]
    public BBParameter<int> skillID = new(0);

    protected override void OnExecute()
    {
        EnemyController controller = enemyController.value;
        // 施法:朝向由代码控制(朝目标)
        controller.NavMeshAgent.updateRotation = false;
        controller.CastSkill(skillID.value);
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;
        // 只看"当前片段"是否播完(不是整套连段):多段技能不会因为等整套而卡住
        if (!controller.AbilityIsPlaying(skillID.value))
            EndAction(true);
    }
}

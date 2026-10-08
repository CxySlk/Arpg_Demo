using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("释放技能")]
[Description("让控制器释放技能并等它播完:技能播完返回 Success,超时(maxDuration)兜底结束;目标丢失或控制器为空返回 Failure")]
public class Enemy_CastSkill : ActionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new();

    [LabelText("技能ID"), Tooltip("对应 EnemyConfig.SkillConfigList 里的下标")]
    public BBParameter<int> skillID = new(0);

    protected override void OnExecute()
    {
        EnemyController controller = enemyController.value;
        controller.CastSkill(skillID.value);
    }

    protected override void OnUpdate()
    {
        EnemyController controller = enemyController.value;
        // 技能播完(或超时)就结束这一段
        if (!controller.SkillIsPlaying(skillID.value))
            EndAction(true);
    }
}

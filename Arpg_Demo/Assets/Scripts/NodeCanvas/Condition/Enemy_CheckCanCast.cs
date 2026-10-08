using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy")]
[Name("检查技能就绪")]
[Description("只检查指定技能现在能不能释放(CD/消耗/允许释放标记)。距离请用「检查目标距离」节点,释放节奏请用 Cooldown 装饰器,随机性请用「概率判定」节点")]
public class Enemy_CheckCanCast : ConditionTask
{
    [LabelText("敌人控制器"), RequiredField]
    public BBParameter<EnemyController> enemyController = new ();

    [LabelText("技能ID"), Tooltip("对应 EnemyConfig.SkillConfigList 里的下标")]
    public BBParameter<int> skillID = new (0);

    protected override bool OnCheck()
    {
        EnemyController controller = enemyController.value;

        return controller.CanCastSkill(skillID.value);
    }
}

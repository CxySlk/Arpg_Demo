using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// 通用角度条件节点：比较"自己"前方与黑板目标(Transform)方向的夹角。
/// <para>不依赖具体角色类型，谁是目标由黑板参数决定，玩家/敌人都能用。</para>
/// <para>LessThanOrEqualTo = 已面向目标(夹角 &lt;= 阈值)；GreaterThan = 需要转向(夹角 &gt; 阈值)。
/// 常和「检查目标距离」配合：距离够但角度不达标 → 走转身分支，角度达标 → 出招。</para>
/// </summary>
[Category("通用")]
[Name("检查目标角度")]
[Description("比较自己前方与目标方向的夹角：LessThanOrEqualTo(已面向) / GreaterThan(需要转向)")]
public class CheckTargetAngle : ConditionTask
{
    public enum CheckType
    {
        LessThanOrEqualTo,
        GreaterThan,
    }

    [Name("判定方式")] public CheckType checkType = CheckType.LessThanOrEqualTo;

    [Name("角色控制器"), RequiredField] public BBParameter<CharacterControllerBase> characterController = new();

    [Name("角度阈值(度)")] public BBParameter<float> maxAngle = new(45f);

    protected override bool OnCheck()
    {
        CharacterControllerBase controller = characterController.value;
        if (!controller.Target) return false;

        Vector3 direction = controller.Target.position - controller.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return true;

        float angle = Vector3.Angle(controller.transform.forward, direction);

        switch (checkType)
        {
            case CheckType.LessThanOrEqualTo: return angle <= maxAngle.value;
            case CheckType.GreaterThan: return angle > maxAngle.value;
            default: return false;
        }
    }
}

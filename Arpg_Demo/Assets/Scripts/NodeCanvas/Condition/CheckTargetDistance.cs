using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// 通用距离条件节点：比较"自己"与黑板目标(Transform)的距离。
/// <para>不依赖具体角色类型，谁是目标由黑板参数决定，玩家/敌人都能用。</para>
/// <para>LessThan / LessThanOrEqualTo 只看最大距离；GreaterThan / GreaterThanOrEqualTo 只看最小距离；
/// Between 需要同时落在 最小距离 ~ 最大距离 之间。两个阈值配合使用即可替代"迟滞"：
/// 例如"距离小于 maxDistance 才追" + "距离大于 minDistance 才追"分给不同节点，就不会在边界抖动。</para>
/// </summary>
[Category("通用")]
[Name("检查目标距离")]
[Description("比较自己与目标的距离：LessThan/GreaterThan 系列只用单边阈值，Between 需要落在 最小距离~最大距离 之间")]
public class CheckTargetDistance : ConditionTask
{
    public enum CheckType
    {
        LessThan,
        GreaterThan,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,
        Between,
    }

    [Name("判定方式")] public CheckType checkType = CheckType.LessThan;

    [Name("目标"), RequiredField] public BBParameter<Transform> target;

    [Name("最大距离"), ShowIf("ShowMaxDistance", 1)]
    public BBParameter<float> maxDistance = new(3f);

    [Name("最小距离"), ShowIf("ShowMinDistance", 1)]
    public BBParameter<float> minDistance = new(1f);

    // NodeCanvas 的 ShowIf 只支持"某字段/属性 == 某个定值"，不支持表达式，
    // 所以用这两个只读属性把条件先算好（只给节点面板用，不参与序列化）。
    public bool ShowMaxDistance => checkType != CheckType.GreaterThan;
    public bool ShowMinDistance => checkType != CheckType.LessThan;

    protected override bool OnCheck()
    {
        Transform targetTransform = target.value;
        float sqrDistance = (agent.transform.position - targetTransform.position).sqrMagnitude;

        switch (checkType)
        {
            case CheckType.LessThan: return sqrDistance < Square(maxDistance.value);
            case CheckType.GreaterThan: return sqrDistance > Square(minDistance.value);
            case CheckType.LessThanOrEqualTo: return sqrDistance <= Square(maxDistance.value);
            case CheckType.GreaterThanOrEqualTo: return sqrDistance >= Square(minDistance.value);
            case CheckType.Between:
                return sqrDistance >= Square(minDistance.value) && sqrDistance <= Square(maxDistance.value);
            default: return false;
        }
    }

    private float Square(float value) => value * value;
}
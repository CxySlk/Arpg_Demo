using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("通用")]
[Name("尝试发现目标")]
public class CircleFindTargetCondition : ConditionTask
{
    [Name("检测半径")] public BBParameter<float> checkRadius = 3;

    [Name("目标变量(黑板)")]
    public BBParameter<Transform> target;

    [Name("检测层级")] public BBParameter<LayerMask> checkLayerMask;

    private Collider[] _colliders = new Collider[10];

    protected override bool OnCheck()
    {
        Physics.OverlapSphereNonAlloc(agent.transform.position, checkRadius.value, _colliders, checkLayerMask.value,
            QueryTriggerInteraction.Ignore);
        // 获取最近的目标
        Collider res = GetClosestCollider(_colliders);
        target.SetValue(res?.transform);
        return res;
    }

    private Collider GetClosestCollider(Collider[] colliders)
    {
        if (!colliders[0]) return null;
        Collider res = colliders[0];
        Vector3 selfPos = agent.transform.position;
        float minSqrDis = Vector3.SqrMagnitude(colliders[0].transform.position - selfPos);
        float curSqrDis;
        for (int i = 1; i < colliders.Length && colliders[i]; i++)
        {
            curSqrDis = Vector3.SqrMagnitude(colliders[i].transform.position - selfPos);
            if (curSqrDis < minSqrDis)
            {
                minSqrDis = curSqrDis;
                res = colliders[i];
            }
        }

        return res;
    }
}
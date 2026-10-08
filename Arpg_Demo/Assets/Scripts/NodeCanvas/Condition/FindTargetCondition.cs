using Character;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

[Category("Enemy")]
[Name("检测玩家")]
[Description("用 EnemyController 在发现距离内查找玩家:找到则写入 target 并返回 Success,否则清空 target 并返回 Failure")]
public class FindTargetCondition : ConditionTask
{
    // 扇形检测的内半径与高度(塞进 TargetSectorCatcher 的值,Gizmos 也用这两个,保持一致)
    // Gizmos 颜色:轮廓亮、填充暗
    private static readonly Color SectorOutlineColor = new(0.2f, 0.85f, 1f);
    private static readonly Color SectorFillColor = new(0.06f, 0.3f, 0.4f);

    [Name("敌人控制器"), BlackboardOnly, RequiredField]
    public BBParameter<EnemyController> enemyController;

    [Name("目标变量(黑板)"), Tooltip("得到的目标会直接返回给黑板上面"), BlackboardOnly]
    public BBParameter<Transform> target;

    [Name("目标捕捉器"), BlackboardOnly] public BBParameter<TargetSectorCatcher> targetCatcher;
    [Name("眼睛偏移位置"), BlackboardOnly] public BBParameter<Vector3> eyeOffset;
    [Name("遮挡物层级"), BlackboardOnly] public BBParameter<LayerMask> obstacleLayer;

    protected override bool OnCheck()
    {
        Collider[] targets = targetCatcher.value.GetTargets(agent.transform);
        if (targets[0] && HasLineOfSight(targets[0]))
        {
            enemyController.value.LockTarget(true);
            enemyController.value.SetTarget(targets[0].transform);
            return true;
        }
        enemyController.value.SetTarget(null);
        enemyController.value.LockTarget(false);
        return false;
    }

    private bool HasLineOfSight(Collider collider)
    {
        Vector3 eyePos = agent.transform.TransformPoint(eyeOffset.value);
        return !Physics.Linecast(
            eyePos,
            collider.transform.position,
            obstacleLayer.value,
            QueryTriggerInteraction.Ignore);
    }

    #region Editor

#if UNITY_EDITOR

    private TargetSectorCatcher EditorCatcher => targetCatcher.value;
    /// <summary>
    /// 画出扇形检测区域。
    /// 触发条件(NodeCanvas 的转发机制):场景里选中敌人(GraphOwner 所在的物体)+ 图窗口里选中这个条件所在节点。
    /// </summary>
    public override void OnDrawGizmosSelected()
    {
        // 和 TargetSectorCatcher.GetTargets 里的算法保持一致:
        // 中心点 = 检测者本地偏移 → 世界;朝向 = 检测者旋转 × 视角旋转偏移
        Vector3 center = agent.transform.TransformPoint(EditorCatcher.centerPos);
        Quaternion rotation = agent.transform.rotation * Quaternion.Euler(EditorCatcher.Rotation);
        Vector3 forward = rotation * Vector3.forward;
        Vector3 up = rotation * Vector3.up;

        float outsideRadius = EditorCatcher.OutsideRadius;
        float halfAngle = EditorCatcher.Angle * 0.5f;
        float halfHeight = EditorCatcher.Height * 0.5f;

        Vector3 bottomCenter = center - up * halfHeight;
        Vector3 topCenter = center + up * halfHeight;

        // 弧线分段:每 5 度一段
        int segmentCount = Mathf.Max(2, Mathf.CeilToInt(EditorCatcher.Angle / 5f));
        Vector3 bottomStart = bottomCenter + Quaternion.AngleAxis(-halfAngle, up) * forward * outsideRadius;
        Vector3 topStart = topCenter + Quaternion.AngleAxis(-halfAngle, up) * forward * outsideRadius;

        // 两条起始半径边 + 顶点竖线 + 起始竖棱
        Gizmos.color = SectorOutlineColor;
        Gizmos.DrawLine(bottomCenter, bottomStart);
        Gizmos.DrawLine(topCenter, topStart);
        Gizmos.DrawLine(bottomCenter, topCenter);
        Gizmos.DrawLine(bottomStart, topStart);

        Vector3 previousBottom = bottomStart;
        Vector3 previousTop = topStart;

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segmentCount);
            Vector3 direction = Quaternion.AngleAxis(angle, up) * forward;
            Vector3 currentBottom = bottomCenter + direction * outsideRadius;
            Vector3 currentTop = topCenter + direction * outsideRadius;

            Gizmos.color = SectorOutlineColor;
            Gizmos.DrawLine(previousBottom, currentBottom); // 下弧
            Gizmos.DrawLine(previousTop, currentTop); // 上弧

            Gizmos.color = SectorFillColor;
            Gizmos.DrawLine(bottomCenter, currentBottom); // 放射线(把扇形填出一个面的感觉)

            previousBottom = currentBottom;
            previousTop = currentTop;
        }

        // 收尾:结束半径边 + 结束竖棱
        Gizmos.color = SectorOutlineColor;
        Gizmos.DrawLine(bottomCenter, previousBottom);
        Gizmos.DrawLine(topCenter, previousTop);
        Gizmos.DrawLine(previousBottom, previousTop);
    }
#endif

    #endregion
}
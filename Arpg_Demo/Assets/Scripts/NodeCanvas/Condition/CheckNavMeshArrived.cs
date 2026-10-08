using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 通用条件：导航代理的寻路是否"走完了"。
/// 判定口径和 NodeCanvas 自带的移动任务保持一致：路径不再计算 且 剩余距离 &lt;= stoppingDistance + 容差。
/// <para>用法：放在移动动作**之前**，用一个勾了 dynamic 的 Selector 兜住 ——
/// 条件成立就切去播停步动画，顺便把还在 Running 的移动动作打断。
/// （放在 Sequencer 里当"后置条件"是没用的：NodeCanvas 不会回头重测已通过的子节点，
/// 正在 Running 的动作只有自己结束才会让位。）</para>
/// </summary>
[Category("通用")]
[Name("寻路是否到达")]
[Description("路径算完且剩余距离进入 stoppingDistance(+容差) 时返回 true。刚进分支、还没下过目的地时返回 false")]
public class CheckNavMeshArrived : ConditionTask
{
    [Name("导航代理"), Tooltip("直接绑 NavMeshAgent 类型的黑板变量；黑板里没有这个类型就留空，改用下面的敌人控制器")]
    public BBParameter<NavMeshAgent> navMeshAgent;

    [Name("敌人控制器"), Tooltip("绑 enemyController，从它身上取 NavMeshAgent")]
    public BBParameter<EnemyController> enemyController;

    [Name("容差(米)"), Tooltip("在 stoppingDistance 之外再多算这么点距离就算到达")]
    public BBParameter<float> tolerance = new(0.05f);

    /// <summary>本次是否真的走动过(下过目的地并产生过路径)。避免"刚进分支、还没寻路"被误判成已到达。</summary>
    private bool _hadPath;

    protected override void OnEnable()
    {
        _hadPath = false;
    }

    protected override bool OnCheck()
    {
        NavMeshAgent _agent = navMeshAgent.value;

        // 路径还在计算：不能算到达
        if (_agent.pathPending) return false;

        if (_agent.hasPath)
        {
            _hadPath = true;
            return _agent.remainingDistance <= _agent.stoppingDistance + Mathf.Max(0f, tolerance.value);
        }

        // 已经没有路径了：之前走过才算"到达"(停下后 StopChase 会 ResetPath，这时应当算到达)；
        // 从来没下过目的地则算没到达，否则第一次 Tick 就会误判、直接跳过移动。
        return _hadPath;
    }
}

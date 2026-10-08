using NodeCanvas.BehaviourTrees;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Category("Enemy/State")]
[Name(nameof(Enemy_CombatState))]
[Description("【已被 NodeCanvas 内置的 NestedBTState(子行为树) 取代,保留备用】手动启动/停止一个 BehaviourTreeOwner 组件的战斗状态")]
public class Enemy_CombatState : EnemyStateBase
{
    /// <summary>状态名（= 类名），供外部 TriggerState 使用。</summary>
    public const string StateName = nameof(Enemy_CombatState);

    private BehaviourTreeOwner _behaviourTreeOwner;

    protected override void OnInit()
    {
        base.OnInit();
        _behaviourTreeOwner = enemyController.GetComponent<BehaviourTreeOwner>();
    }

    protected override void OnEnter()
    {
        // 启动战斗行为树
        _behaviourTreeOwner.StartBehaviour();
    }

    protected override void OnExit()
    {
        // 离开战斗状态（受击/死亡）→ 停掉行为树：节点会被 Reset，回来时重新决策
        _behaviourTreeOwner.StopBehaviour();
    }
}

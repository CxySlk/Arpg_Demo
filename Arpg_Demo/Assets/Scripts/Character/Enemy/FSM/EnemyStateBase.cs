using Character;
using NodeCanvas.StateMachines;

/// <summary>
/// 敌人状态机状态基类：统一拿到敌人控制器。
/// 结构约定：**状态机在外层**（管战斗/受击这类"硬状态"、也是外部打断入口），
/// 行为树只在"战斗状态"内部运行（管待机/追击/攻击这些软决策）。
/// </summary>
public abstract class EnemyStateBase : FSMState
{
    protected EnemyController enemyController;

    protected override void OnInit()
    {
        enemyController = graph.agent.GetComponent<EnemyController>();
    }
}

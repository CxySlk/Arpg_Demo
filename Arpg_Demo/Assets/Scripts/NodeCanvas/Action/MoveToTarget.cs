using System.Collections.Generic;
using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

[Category("Enemy")]
[Name("追击目标")]
[Description("始终朝向 target,超出停止距离时用 NavMesh 追上去(持续 Running);播放移动动画与音效")]
public class MoveToTarget : ActionTask
{
    [Name("敌人控制器"), BlackboardOnly, RequiredField]
    public BBParameter<EnemyController> enemyController;

    [Name("目标变量(黑板)"), BlackboardOnly, RequiredField]
    public BBParameter<Transform> target;

    [Name("追击停住距离"), BlackboardOnly, RequiredField]
    public BBParameter<float> stopChaseDis;

    [Name("移动动画名"), RequiredField] public string moveClipName;
    [Name("停步动画名"), RequiredField] public string stopClipName;

    [Name("启用音效")] public bool enableAudio;
    [Name("音效表"), ShowIf("enableAudio", 1)] public List<AnimationAudioEntry> audioEntries = new();

    /// <summary>本节点播放的移动动画 state:被受控等打断后用来判断并补播。</summary>
    private AnimancerState _moveState;

    protected override void OnExecute()
    {
        enemyController.value.SetStopChaseDis(stopChaseDis.value);
        // 追击:朝向交给寻路组件,沿路径转向(绕路时朝前进方向)
        enemyController.value.NavMeshAgent.updateRotation = true;
        PlayMove();
        Chase();
    }

    protected override void OnUpdate()
    {
        // 受控等打断会顶掉移动动画;节点被暂停后恢复不会重跑 OnExecute,这里补播
        if (_moveState is { IsPlaying: false })
            PlayMove();

        Chase();
    }

    private void PlayMove()
    {
        _moveState = enemyController.value.Play(moveClipName);
        enemyController.value.SetRootMotion(null);
        if (enableAudio)
            AnimationAudioHelper.Bind(_moveState, audioEntries, enemyController.value.transform);
    }

    protected override void OnStop(bool interrupted)
    {
        enemyController.value.StopChase();
        // 只有正常结束(追到点/失败)才播收步动画；被打断(受控/受击)不播，避免盖掉受击表现
        if (!interrupted)
            enemyController.value.Play(stopClipName);
    }

    /// <summary>agent 到达判定的小容差:避免"路径剩余距离已 &lt;= stoppingDistance、但直线距离略大"导致的永久卡住。</summary>
    private const float ArriveEpsilon = 0.1f;

    private void Chase()
    {
        EnemyController controller = enemyController.value;
        Transform targetTrans = target.value;

        if (!targetTrans)
        {
            EndAction(false);
            return;
        }

        // 到达判定:直线距离在停止距离内，或 agent 按路径剩余距离判定已到达。
        // 只用直线距离会卡住:NVAgent 停在 stoppingDistance 上(remaining<=stoppingDistance)时,
        // 直线距离可能略大于停住阈值(StopChaseDis) → 判定永不成立 → 节点一直 Running(播放动画却不移动)。
        if (controller.IsTargetInStopDis() || IsArrivedByAgent(controller.NavMeshAgent))
        {
            EndAction(true);
            return;
        }

        controller.ChaseTarget(targetTrans);
    }

    /// <summary>agent 口径的到达判定:路径算完、有路径、剩余距离已进入停止距离(含小容差)。</summary>
    private static bool IsArrivedByAgent(NavMeshAgent agent)
        => !agent.pathPending
           && agent.hasPath
           && agent.remainingDistance <= agent.stoppingDistance + ArriveEpsilon;
}

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
    [Name("播放速度")] public float playSpeed = 1f;

    [Name("启用音效")] public bool enableAudio;
    [Name("音效表"), ShowIf("enableAudio", 1)] public List<AnimationAudioEntry> audioEntries = new();

    protected override void OnExecute()
    {
        enemyController.value.SetStopChaseDis(stopChaseDis.value);

        AnimancerState state = enemyController.value.Play(moveClipName, playSpeed);
        if (enableAudio)
            AnimationAudioHelper.Bind(state, audioEntries, enemyController.value.transform);

        Chase();
    }

    protected override void OnUpdate() => Chase();

    protected override void OnStop() => enemyController.value.StopChase();

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

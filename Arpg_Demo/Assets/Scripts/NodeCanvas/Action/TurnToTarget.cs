using System.Collections.Generic;
using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>
/// 通用转身节点：让角色转向黑板目标。
/// <para>角度小于「最小角度」时不播转身动画，直接快速摆正(无根运动)。</para>
/// <para>角度较大时按左/右、90/180 选一段转身动画播放：动画自带旋转、开根运动由它驱动 transform 旋转，
/// 节点本身不写 rotation，避免与根运动冲突；转到朝向进入容差(或符号翻转/超时)即清根运动结束。</para>
/// </summary>
[Category("通用")]
[Name("转身")]
[Description("让角色转身面向目标：小角度直接摆正；大角度按左/右播转身动画(动画根运动驱动旋转)，面向后返回 Success")]
public class TurnToTarget : ActionTask
{
    [Name("角色控制器"), RequiredField] public BBParameter<CharacterControllerBase> characterController = new();

    [Name("左转90")] public BBParameter<string> turn90L = new("Turn_90_L");
    [Name("右转90")] public BBParameter<string> turn90R = new("Turn_90_R");
    [Name("左转180")] public BBParameter<string> turn180L = new("Turn_180_L");
    [Name("右转180")] public BBParameter<string> turn180R = new("Turn_180_R");

    [Name("朝向容差(度)")] public BBParameter<float> angleTolerance = new(8f);

    [Name("最小角度(度)"), Tooltip("小于它不播转身动画，直接快速摆正")]
    public BBParameter<float> minAngle = new(30f);

    [Name("最长转向时间(秒)")] public BBParameter<float> maxTime = new(2f);

    [Name("启用音效")] public bool enableAudio;
    [Name("音效表"), ShowIf("enableAudio", 1)] public List<AnimationAudioEntry> audioEntries = new();

    private enum TurnMode
    {
        None,
        Slerp,
        Animation,
    }

    private TurnMode _mode;
    private float _startTime;
    private AnimancerState _state;

    protected override void OnExecute()
    {
        CharacterControllerBase controller = characterController.value;
        if (!controller.Target)
        {
            EndAction(false);
            return;
        }

        _startTime = Time.time;

        float signedAngle = GetSignedAngle(controller);
        if (Mathf.Abs(signedAngle) <= angleTolerance.value)
        {
            EndAction(true);
            return;
        }

        // 小角度：摆正就行，不播转身动画
        if (Mathf.Abs(signedAngle) < minAngle.value)
        {
            _mode = TurnMode.Slerp;
            return;
        }

        // 大角度：播转身动画，旋转交给根运动。动画按名字从角色的标准动画配置解析。
        CharacterView view = controller.CharacterView;
        TransitionAsset transition = view.GetTransition(PickName(signedAngle));
        AnimancerComponent animancer = view.Animancer;
        if (transition == null || !transition.IsValid)
        {
            _mode = TurnMode.Slerp;
            return;
        }

        _mode = TurnMode.Animation;
        controller.SetRootMotion(OnRootMotion);
        _state = animancer.Play(transition);
        if (_state != null)
        {
            _state.Time = 0; // 从头播：避免复用同一 state 时接着上次继续
            if (enableAudio)
                AnimationAudioHelper.Bind(_state, audioEntries, controller.transform);
        }
    }

    protected override void OnUpdate()
    {
        CharacterControllerBase controller = characterController.value;
        if (!controller.Target)
        {
            Cleanup(controller);
            EndAction(false);
            return;
        }

        if (Mathf.Abs(GetSignedAngle(controller)) <= angleTolerance.value)
        {
            Cleanup(controller);
            EndAction(true);
            return;
        }

        if (_mode == TurnMode.Slerp)
            FacingToTarget(controller);

        // 兜底：太久还没转到位就别一直挂着
        if (Time.time - _startTime >= maxTime.value)
        {
            Cleanup(controller);
            EndAction(true);
        }
    }

    protected override void OnStop(bool interrupted)
    {
        Cleanup(characterController.value);
    }

    /// <summary>清根运动回调(关键:不清的话循环转身动画会一直推着 transform 转)。</summary>
    private void Cleanup(CharacterControllerBase controller)
    {
        controller.SetRootMotion(null);

        _state = null;
        _mode = TurnMode.None;
    }

    /// <summary>小角度直接快速摆正(不播转身动画、无根运动)。</summary>
    private void FacingToTarget(CharacterControllerBase controller)
    {
        Vector3 direction = controller.Target.position - controller.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
        controller.transform.rotation = Quaternion.RotateTowards(
            controller.transform.rotation, lookRotation, 720f * Time.deltaTime);
    }

    /// <summary>动画根运动回调:转身是原地 pivot，只用旋转，忽略位移。</summary>
    private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        characterController.value.Rotate(deltaRot);
    }

    /// <summary>按方向(左/右)与幅度(90/180)选一个转身动画名字。</summary>
    private string PickName(float signedAngle)
    {
        bool turnRight = signedAngle > 0f; // SignedAngle 为正 = 目标在右
        bool use180 = Mathf.Abs(signedAngle) > 90f;

        if (use180) return (turnRight ? turn180R : turn180L).value;
        return (turnRight ? turn90R : turn90L).value;
    }

    /// <summary>自身前方到目标方向的有符号水平夹角:正 = 目标在右，负 = 目标在左。</summary>
    private static float GetSignedAngle(CharacterControllerBase controller)
    {
        Vector3 direction = controller.Target.position - controller.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return 0f;

        return Vector3.SignedAngle(controller.transform.forward, direction, Vector3.up);
    }
}
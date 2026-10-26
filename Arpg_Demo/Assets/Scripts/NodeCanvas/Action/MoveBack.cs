using System.Collections.Generic;
using Animancer;
using cfg;
using Character;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using Attribute = GAS.Attribute;

namespace NodeCanvas.BehaviourTrees
{
    [Name("后退")]
    [Category("动画/Action")]
    [Description("太近时后退拉开距离：按 MoveSpeed 反向位移 + 播放后退动画，拉开到最小距离即成功")]
    public class MoveBack : ActionTask
    {
        [Name("角色控制器"), RequiredField] public BBParameter<CharacterControllerBase> controller;
        [Name("目标"), RequiredField] public BBParameter<Transform> target;
        [Name("后退动画名"), RequiredField] public string moveBackClipName;
        [Name("停步动画名"), RequiredField] public string stopClipName;
        [Name("后退速度比例")] public float moveBackSpeedPercent = .5f;
        [Name("最小距离"), RequiredField] public BBParameter<float> minDistance;

        [Name("保持面向目标")] public bool faceTarget = true;

        [Name("转向速度(度/秒)"), ShowIf("faceTarget", 1)]
        public float turnSpeed = 360f;

    [Name("启用音效")] public bool enableAudio;

    [Name("音效表"), ShowIf("enableAudio", 1)]
    public List<AnimationAudioEntry> audioEntries = new();

    /// <summary>本节点播放的后退动画 state:被受控等打断后用来判断并补播。</summary>
    private AnimancerState _moveState;

    protected override void OnExecute()
    {
        PlayMove();
    }

    protected override void OnUpdate()
    {
        // 受控等打断会顶掉后退动画;节点被暂停后恢复不会重跑 OnExecute,这里补播
        if (_moveState != null && !_moveState.IsPlaying)
            PlayMove();

        if (HorizontalDistance() >= minDistance.value)
        {
            EndAction(true);
            return;
        }

        if (faceTarget)
            FaceTarget();

        controller.value.Asc.TryGetAttrValue(EAttributeType.MoveSpeed, out Attribute moveSpeed);
        Vector3 away = controller.value.transform.position - target.value.position;
        away.y = 0f;
        controller.value.Move(away.normalized * (moveSpeed.CurValue * Time.deltaTime * moveBackSpeedPercent));
    }

    private void PlayMove()
    {
        _moveState = controller.value.Play(moveBackClipName);
        controller.value.SetRootMotion(null); // 后退由代码位移,不需要动画根运动
        if (enableAudio)
            AnimationAudioHelper.Bind(_moveState, audioEntries, controller.value.transform);
    }

    protected override void OnStop(bool interrupted)
    {
        // 只有正常结束才播收步动画；被打断(受控/受击)不播
        if (!interrupted)
            controller.value.Play(stopClipName);
    }

        private float HorizontalDistance()
        {
            Vector3 offset = target.value.position - controller.value.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void FaceTarget()
        {
            Vector3 direction = target.value.position - controller.value.transform.position;
            direction.y = 0f;
            controller.value.transform.rotation = Quaternion.RotateTowards(
                controller.value.transform.rotation,
                Quaternion.LookRotation(direction),
                turnSpeed * Time.deltaTime);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Animancer;
using GAS;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Action
{
    [Name("顺序播放动画")]
    [Category("动画/Action")]
    public class SequentialPlayback : ActionTask
    {
        [Name("动画列表")] public BBParameter<List<ClipTransition>> animationClips;
        [Name("动画组件"), RequiredField] public BBParameter<AnimancerComponent> animancer;
        [Name("角色控制器")] public BBParameter<ICharacterController> characterController;
        [Name("是否开启根运动")] public bool rootMotion = true;
        [Name("退出时停止播放动画")] public bool exitQuit = false;
        [Name("过渡(淡入)时长")] public float fade = .25f;

        [Name("启用音效")] public bool enableAudio;
        [Name("音效表"), ShowIf("enableAudio", 1)] public List<AnimationAudioEntry> audioEntries = new();

        private Coroutine _coroutine;
        private AnimancerState _currentState;

        protected override void OnExecute()
        {
            _coroutine = StartCoroutine(SequentialPlaybackCoroutine());
        }

        protected override void OnStop()
        {
            // 协程可能已经播完(或抛异常退出)并把句柄清空了，这时不能再 StopCoroutine:
            // Unity 的 StopCoroutine(null) 会抛 "routine is null"
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            // 退出时清掉根运动回调，否则动作结束后角色还会被动画的根运动推着滑一段
            characterController.value?.SetRootMotion(null);

            // 退出时停止播放:冻结当前姿势
            if (exitQuit)
                FreezeAnimation();
        }

        private IEnumerator SequentialPlaybackCoroutine()
        {
            // 黑板参数没配全就先判失败，别让协程在里面 NRE、节点永远挂在 Running
            if (animationClips.value == null)
            {
                _coroutine = null;
                EndAction(false);
                yield break;
            }

            AnimancerComponent _animancer = animancer.value;

            foreach (ClipTransition clip in animationClips.value)
            {
                if (clip == null) continue;

                characterController.value?.SetRootMotion(rootMotion ? OnRootMotion : null);
                _currentState = _animancer.Play(clip, fade);
                if (enableAudio)
                    AnimationAudioHelper.Bind(_currentState, audioEntries, _animancer.transform);
                yield return _currentState;
            }

            _coroutine = null; // 播完先清句柄:OnStop 就不会去停一个已经结束的协程
            EndAction(true);
        }

        /// <summary>停止当前动画:清除完成事件、把当前姿势冻结(速度置 0)，Animator 保持最后一次求值的姿势。</summary>
        private void FreezeAnimation()
        {
            if (_currentState != null)
            {
                _currentState.Speed = 0f;
                if (_currentState.HasOwnedEvents)
                    _currentState.OwnedEvents.OnEnd = null;
            }

            _currentState = null;
        }

        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            if (characterController.value == null) return;

            characterController.value.Move(deltaPos);
            characterController.value.Rotate(deltaRot);
        }
    }
}
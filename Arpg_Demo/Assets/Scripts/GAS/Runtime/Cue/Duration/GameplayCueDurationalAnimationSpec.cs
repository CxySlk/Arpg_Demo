using Animancer;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续动画 Cue 的 Spec。通过 ICharacterController 播放动画，达到 DurationSeconds 秒后回收。
    /// </summary>
    public class GameplayCueDurationalAnimationSpec : GameplayCueDurationalSpec<GameplayCueDurationalAnimation>
    {
        private ICharacterController _controller;

        public override void Apply()
        {
            base.Apply();
            _controller = target.GetComponent<ICharacterController>();
            _controller.SetRootMotion(OnRootMotion);

            AnimancerState state = _controller.Animancer.Play(DurationalCue.AnimationClip, .25f, FadeMode.FromStart);
            if (state != null)
                state.Speed = Mathf.Max(0.01f, DurationalCue.PlaySpeed);
        }

        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            // Spec 被回收后 cue/_controller 会被清空，但动画控制器上的 RootMotion 回调未随之清除，
            // 动画收尾/混合期间 OnAnimatorMove 仍可能触发这里，需判空防止空引用。
            if (DurationalCue is { RootMotion: true })
            {
                _controller.Move(deltaPos);
                _controller.Rotate(deltaRot);
            }
        }

        protected override void Destroy()
        {
            base.Destroy();
            _controller = null;
        }
    }
}
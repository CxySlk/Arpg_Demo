using System.Collections;
using Animancer;
using UnityEngine;

namespace GAS
{
    public class GameplayCueInstantAnimationSpec : GameplayCueInstantSpec<GameplayCueInstantAnimation>
    {
        private ICharacterController _controller;

        public override void Init(GameplayCue cue, AbilitySystemComponent target)
        {
            base.Init(cue, target);
            _controller = target!.GetComponent<ICharacterController>();
        }

        public override void Apply()
        {
            // 纯播动画:是否进入受控由受控效果决定,这里只负责播目标动画 + 按配置挂根运动回调
            _controller.SetRootMotion(InstanceCue.rootMotion ? OnRootMotion : null);
            _controller.Animancer.Play(InstanceCue.AnimationClip, .25f, FadeMode.FromStart);

            // 播放完动画自动回收
            MonoSystem.Start_Coroutine(WaitTimeRecycle(InstanceCue.AnimationClip.length));
        }

        private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            // Spec 被回收后 _controller 已置空,但动画收尾/混合期间 OnAnimatorMove 仍可能触发,
            // 需判空防止空引用(与 GameplayCueDurationalAnimationSpec 同款防御)。
            if (_controller == null) return;
            _controller.Move(deltaPos);
            _controller.Rotate(deltaRot);
        }

        protected override void Destroy()
        {
            base.Destroy();
            _controller = null;
        }

        private IEnumerator WaitTimeRecycle(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            Destroy();
        }
    }
}
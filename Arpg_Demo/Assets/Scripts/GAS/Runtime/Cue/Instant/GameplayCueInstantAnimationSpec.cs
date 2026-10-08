using System.Collections;
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
            // 受控表现：动画交给角色控制器 —— 它按动画长度进入/延长"受控状态"，
            // 并把当前动画压栈，等这段播完从暂停处继续(眩晕被受击打断、播完受击再接着播眩晕)。
            // 目标不允许受控(例如技能前摇霸体)时按普通方式播。
            _controller.SetRootMotion(OnRootMotion);
            if (!_controller.TryPlayControlledAnimation(InstanceCue.AnimationClip))
                _controller.Animancer.Play(InstanceCue.AnimationClip);

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
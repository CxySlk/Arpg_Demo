using System;
using Animancer;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 角色控制器契约。由角色层（Assembly-CSharp 的 CharacterControllerBase 等）实现，
    /// GAS 的 Cue Spec / 效果通过此接口驱动动画播放、位移与受控状态，避免反向依赖角色层。
    /// </summary>
    public interface ICharacterController
    {
        public abstract AnimancerComponent Animancer { get; }

        /// <summary>设置/清空根运动回调(Animator 同物体,转发 deltaPosition/deltaRotation)。</summary>
        public void SetRootMotion(Action<Vector3, Quaternion> onRootMotion);

        /// <summary>
        /// 尝试用"受控表现"播放这段动画：目标进入/延长受控状态(时长 = 动画长度)，并把当前动画压栈，
        /// 等这段播完自动从暂停处继续(眩晕被受击打断、播完受击再接着播眩晕)。
        /// 返回 false 表示目标当前不允许受控(例如技能前摇霸体)，调用方自行退化处理。
        /// </summary>
        public bool TryPlayControlledAnimation(AnimationClip clip);

        /// <summary>
        /// 进入/延长受控状态（时长按秒给，例如没有动画的定身、眩晕）。
        /// 已经受控时只取更晚的结束时间，不会把剩余时间重置掉。
        /// </summary>
        public void EnterControlled(float duration);

        public void Move(Vector3 deltaPos);

        public void Rotate(Quaternion deltaRot);
    }
}
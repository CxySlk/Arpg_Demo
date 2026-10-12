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
        public bool IsDead { get; }

        /// <summary>设置/清空根运动回调(Animator 同物体,转发 deltaPosition/deltaRotation)。</summary>
        public void SetRootMotion(Action<Vector3, Quaternion> onRootMotion);

        /// <summary>
        /// 进入/延长受控状态（时长按秒给，例如没有动画的定身、眩晕）。
        /// 已经受控时只取更晚的结束时间，不会把剩余时间重置掉。
        /// </summary>
        public void EnterControlled(float duration);

        public void Move(Vector3 deltaPos);

        public void Rotate(Quaternion deltaRot);
    }
}
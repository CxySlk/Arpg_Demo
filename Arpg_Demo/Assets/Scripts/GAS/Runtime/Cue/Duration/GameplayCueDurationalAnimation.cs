using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续动画 Cue 模板。配置动画片段，触发时播放动画并持续 DurationSeconds 秒。
    /// </summary>
    [System.Serializable]
    public class GameplayCueDurationalAnimation : GameplayCueDurational
    {
        [LabelText("动画片段"), OnValueChanged("CalculateDuration")]
        public AnimationClip AnimationClip;

        [LabelText("RootMotion")] public bool RootMotion;

        [LabelText("播放速度"), MinValue(0.01f), OnValueChanged("CalculateDuration")]
        public float PlaySpeed = 1f;

        protected override GameplayCueSpec CreateSpec() =>
            PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAnimationSpec>();

        public override GameplayCueDurational DeepCopy()
        {
            GameplayCueDurationalAnimation animCue = PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAnimation>();
            animCue.DurationSeconds = DurationSeconds;
            animCue.AnimationClip = AnimationClip;
            animCue.RootMotion = RootMotion;
            animCue.PlaySpeed = PlaySpeed;
            return animCue;
        }

#if UNITY_EDITOR

        /// <summary>持续时长 = 动画长度（秒）/ 播放速度</summary>
        public override void CalculateDuration()
        {
            float speed = Mathf.Max(0.01f, PlaySpeed);
            DurationSeconds = AnimationClip ? AnimationClip.length / speed : 0f;
        }
#endif
    }
}
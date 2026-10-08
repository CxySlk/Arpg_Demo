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
        [LabelText("动画片段"), OnValueChanged(nameof(CalculateDuration))]
        public AnimationClip AnimationClip;

        [LabelText("RootMotion")] public bool RootMotion;

        protected override GameplayCueSpec CreateSpec() =>
            PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAnimationSpec>();

        public override GameplayCueDurational DeepCopy()
        {
            GameplayCueDurationalAnimation animCue = PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAnimation>();
            animCue.DurationSeconds = DurationSeconds;
            animCue.AnimationClip = AnimationClip;
            animCue.RootMotion = RootMotion;
            return animCue;
        }

#if UNITY_EDITOR

        /// <summary>持续时长 = 动画长度（秒）</summary>
        public override void CalculateDuration() => DurationSeconds = AnimationClip ? AnimationClip.length : 0f;
#endif
    }
}
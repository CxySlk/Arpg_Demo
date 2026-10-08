using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续音效 Cue 模板。配置音频片段，触发时播放并持续 DurationSeconds 秒。
    /// </summary>
    [System.Serializable]
    public class GameplayCueDurationalAudio : GameplayCueDurational
    {
        [LabelText("音频片段"),OnValueChanged(nameof(CalculateDuration))]
        public AudioClip AudioClip;

        [LabelText("音量倍率")]
        public float VolumeMultiplier = 1;

        [LabelText("是否3D音效")]
        public bool Is3D;

        protected override GameplayCueSpec CreateSpec() => PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAudioSpec>();
        
        public override GameplayCueDurational DeepCopy()
        {
            GameplayCueDurationalAudio audioCue = PoolSystem.Global.GetOrNewObject<GameplayCueDurationalAudio>();
            audioCue.DurationSeconds = DurationSeconds;
            audioCue.AudioClip = AudioClip;
            audioCue.VolumeMultiplier = VolumeMultiplier;
            audioCue.Is3D = Is3D;
            return audioCue;
        }
        
#if UNITY_EDITOR

        /// <summary>持续时长 = 音频长度（秒）</summary>
        public override void CalculateDuration() => DurationSeconds = AudioClip ? AudioClip.length : 0f;
#endif
    }
}

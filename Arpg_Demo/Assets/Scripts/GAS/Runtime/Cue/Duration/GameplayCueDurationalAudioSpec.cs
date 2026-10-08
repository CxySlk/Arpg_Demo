using LLKFrame;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续音效 Cue 的 Spec。播放一次音频即回收（持续 Cue 的持续帧仅用于时间轴预览）。
    /// </summary>
    public class GameplayCueDurationalAudioSpec : GameplayCueDurationalSpec<GameplayCueDurationalAudio>
    {
        public override void Apply()
        {
            base.Apply();
            AudioSystem.PlayEffectMusic(DurationalCue.AudioClip, (target as Component)!.transform.position,
                DurationalCue.VolumeMultiplier, DurationalCue.Is3D);
            DisApply();
        }
    }
}

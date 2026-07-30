using LLKFrame;

namespace GAS
{
    /// <summary>
    /// 瞬时音效 Cue 的运行时 Spec。通过 AudioSystem 播放音效，
    /// 自动处理 AudioSource 对象池获取和回收。
    /// </summary>
    public class GameplayCueAudioInstantSpec :
        GameplayCueInstantSpec<GameplayCueAudioInstant>
    {
        public override void Trigger()
        {
            if (Owner == null || Cue == null || InstanceCue.AudioClip == null) return;
            AudioSystem.PlayEffectMusic(InstanceCue.AudioClip, Owner.transform.position, InstanceCue.VolumeMultiplier, InstanceCue.Is3D);

            Destroy();
        }
    }
}

using LLKFrame;
using UnityEngine;

namespace GAS
{
    public class GameplayCueInstantAudioSpec : GameplayCueInstantSpec<GameplayCueInstantAudio>
    {
        public override void Apply()
        {
            // 播放完就可以销毁了
            AudioSystem.PlayEffectMusic(InstanceCue.audioClip,(target as Component)!.transform.position);
            Destroy();
        }
    }
}
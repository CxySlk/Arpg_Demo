using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueInstantAudio :GameplayCueInstant
    {
        [LabelText("音效")]public AudioClip audioClip;

        [LabelText("音量倍率")]
        public float VolumeMultiplier = 1;
        
        protected override GameplayCueSpec CreateSpec()
        {
            return PoolSystem.Global.GetOrNewObject<GameplayCueInstantAudioSpec>();
        }
    }
}
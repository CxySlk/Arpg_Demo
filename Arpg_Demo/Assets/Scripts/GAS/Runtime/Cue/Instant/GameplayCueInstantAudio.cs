using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueInstantAudio :GameplayCueInstant
    {
        [LabelText("音效")]public AudioClip audioClip;
        
        protected override GameplayCueSpec CreateSpec()
        {
            return PoolSystem.Global.GetOrNewObject<GameplayCueInstantAudioSpec>();
        }
    }
}
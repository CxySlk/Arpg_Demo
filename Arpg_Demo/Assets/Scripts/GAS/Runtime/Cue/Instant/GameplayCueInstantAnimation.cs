using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueInstantAnimation:GameplayCueInstant
    {
        [LabelText("动画")]public AnimationClip  AnimationClip;
        
        protected override GameplayCueSpec CreateSpec()
        {
           return PoolSystem.Global.GetOrNewObject<GameplayCueInstantAnimationSpec>();
        }
    }
}
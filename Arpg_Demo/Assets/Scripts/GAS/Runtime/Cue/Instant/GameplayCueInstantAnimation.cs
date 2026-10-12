using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueInstantAnimation : GameplayCueInstant
    {
        [LabelText("动画")] public AnimationClip AnimationClip;
        [LabelText("开启根运动")] public bool rootMotion = true;


        protected override GameplayCueSpec CreateSpec()
        {
            return PoolSystem.Global.GetOrNewObject<GameplayCueInstantAnimationSpec>();
        }
    }
}
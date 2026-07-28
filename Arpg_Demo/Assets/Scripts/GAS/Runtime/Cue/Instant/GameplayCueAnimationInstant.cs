using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(fileName = "Cue_Animation", menuName = "GAS/Instant动画")]
    public class GameplayCueAnimationInstant : GameplayCueInstant<GameplayCueAnimationInstantSpec>
    {
        [LabelText("动画片段"), SerializeField]
        private AnimationClip _animationClip;
        public AnimationClip AnimationClip => _animationClip;
        public override GameplayCueAnimationInstantSpec CreateSpec() => PoolSystem.GetOrNewObject<GameplayCueAnimationInstantSpec>();
    }
}

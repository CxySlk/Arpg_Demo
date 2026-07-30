using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 瞬时动画 Cue 资产。配置一个 AnimationClip，触发时在所有者 ASC 的 AnimationController 上播放。
    /// </summary>
    [CreateAssetMenu(fileName = "Cue_Animation", menuName = "GAS/即时动画Cue")]
    public class GameplayCueAnimationInstant : GameplayCueInstant
    {
        [LabelText("动画片段"), SerializeField]
        private AnimationClip _animationClip;
        public AnimationClip AnimationClip => _animationClip;

        public override GameplayCueInstantSpec CreateSpec() => PoolSystem.GetOrNewObject<GameplayCueAnimationInstantSpec>();
    }
}

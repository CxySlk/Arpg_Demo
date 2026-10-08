using Animancer;

namespace GAS
{
    /// <summary>
    /// 列表Cue顺序播放：动画 Cue 按列表顺序塞进一个 <see cref="SequenceState"/>，
    /// 播完一个再播下一个；音效/粒子等非动画 Cue 不占位，直接播放。
    /// 受控时长 = 所有动画 Cue 的动画长度之和。
    /// </summary>
    public class ControlListCueEffect : ControlCueEffect
    {
        protected override void ApplyEffect()
        {
            AbilitySystemComponent asc = isTarget ? target : source;
            ICharacterController controller = asc.GetComponent<ICharacterController>();

            SequenceState sequence = new SequenceState();
            bool hasAnimation = false;
            foreach (GameplayCue cue in CueList)
            {
                if (cue is GameplayCueInstantAnimation animationCue)
                {
                    // 顺序播放：动画按列表顺序进序列，后面的等前面播完
                    if (asc.CheckCanRelease(animationCue) && animationCue.AnimationClip != null)
                    {
                        sequence.Add(animationCue.AnimationClip, AnimancerGraph.DefaultFadeDuration);
                        hasAnimation = true;
                    }
                }
                else
                {
                    cue.Apply(asc); // 音效/粒子等不占位，直接播
                }
            }

            if (hasAnimation)
                controller.Animancer.Play(sequence);

            if (controlledTime <= 0)
            {
                float calcTime = CalculateControllerTime();
                if (calcTime > 0) controller.EnterControlled(calcTime);
            }
            else controller.EnterControlled(controlledTime);
        }

        // 只计算动画Cue的长度
        protected override float CalculateControllerTime()
        {
            float res = 0;
            foreach (GameplayCue cue in CueList)
            {
                if (cue is GameplayCueInstantAnimation animCue)
                    res += animCue.AnimationClip.length;
            }

            return res;
        }
    }
}

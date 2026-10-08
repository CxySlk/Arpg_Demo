using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 受控表现效果：把 Cue 播到目标(或来源)身上，并可选择按配置时长进入/延长"受控状态"。
    /// <para>与 <see cref="GameplayCueEffect"/> 的区别：多了"作用于谁"和"显式受控时长"
    /// —— 没有动画也能控住（例如定身、纯眩晕）；动画 Cue 依旧会按动画长度自动延长受控状态。</para>
    /// </summary>
    [LabelText("被控效果")]
    public class ControlCueEffect : GameplayCueEffect
    {
        [LabelText("受控时长(秒)"), Tooltip("> 0 时先按这个时长进入/延长受控状态；<= 0 表示不额外控制，时长交给动画 Cue")]
        public float controlledTime = -1f;

        protected override void ApplyEffect()
        {
            // 获取作用对象
            AbilitySystemComponent asc = isTarget ? target : source;
            foreach (GameplayCue cue in CueList)
                cue.Apply(asc);

            ICharacterController controller = asc.GetComponent<ICharacterController>();
            if (controlledTime <= 0)
            {
                float calcTime = CalculateControllerTime();
                if (calcTime > 0) controller.EnterControlled(calcTime);
            }
            else controller.EnterControlled(controlledTime);
        }

        protected virtual float CalculateControllerTime()
        {
            foreach (GameplayCue cue in CueList)
            {
                if (cue is GameplayCueInstantAnimation animCue)
                    return animCue.AnimationClip.length;
            }
            return 0;
        }
    }
}
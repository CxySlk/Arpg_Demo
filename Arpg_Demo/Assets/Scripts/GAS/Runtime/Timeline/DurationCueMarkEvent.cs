using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续 Cue 标记事件。在指定帧触发一组持续 Cue（动画/音效/粒子）。
    /// </summary>
    public class DurationCueMarkEvent : MarkEventBase
    {
        [LabelText("持续Cue模板"), SerializeReference, Required]
        public GameplayCueDurational DurationCue;

        /// <summary>在指定 ASC 上触发所有持续 Cue</summary>
        public void Execute(AbilitySystemComponent target)
        {
            if (target == null || DurationCue == null) return;
            DurationCue.Apply(target);
        }
    }
}
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using LLKFrame;
#endif
namespace GAS
{
    /// <summary>
    /// 释放 EffectBehavior 标记事件。在指定帧对 TargetCatcher 捕捉到的所有目标应用效果。
    /// </summary>
    public class ReleaseGEMarkEvent : MarkEventBase
    {
        [LabelText("目标捕捉器")] public TargetCatcherBase targetCatcher;

        [LabelText("Effect"), DictionaryDrawerSettings(KeyLabel = "效果", ValueLabel = "层数")]
        public Dictionary<GameplayEffect, int> Effects = new();

        [LabelText("持续帧数"), MinValue(1), Tooltip("事件作用的帧数。<=1 时退化为单帧（旧行为）")]
        public int durationFrames = 3;

        [LabelText("可重复作用"), Tooltip("关闭：同一个目标在整段内只作用一次（离开再进入也不再作用）。开启：可按间隔重复作用")]
        public bool canRepeat = false;

        [LabelText("作用间隔(帧)"), ShowIf("canRepeat"), MinValue(0), Tooltip("同一目标重复作用的最小间隔帧数。0 或 1 表示每帧都可作用")]
        public int hitIntervalFrames = 0;

        /// <summary>运行时实际使用的起止帧（结束帧不含）：durationFrames 归一为至少 1。</summary>
        public int GetDurationFrames() => Mathf.Max(1, durationFrames);

        /// <summary>
        /// 对捕捉到的所有目标应用效果（无目标捕捉器或效果时直接返回）
        /// </summary>
        public void Execute(AbilitySystemComponent source)
        {
            if (Effects == null || Effects.Count == 0) return;

            switch (targetCatcher)
            {
                // 为空默认作用自己
                case null:
                    ApplyEffects(source, source);
                    break;
                // 武器检测通过触发器来执行，因此帧数不能小于等于1，所以根本不会走该路径
#if UNITY_EDITOR
                case WeaponCatcher:
                    LogSystem.Log("开启武器检测不走该路径，而是走DurationGEEventRuntime" +
                                           LogSystem.ErrorColor("（错误原因：帧数设置需要大于1）"));
                    break;
#endif
                // 剩余的是形状检测
                default:
                    Collider[] targets = targetCatcher.GetTargets(source.transform);
                    for (int i = 0; i < targets.Length && targets[i]; i++)
                    {
                        Collider col = targets[i];
                        AbilitySystemComponent target = col.GetComponentInParent<AbilitySystemComponent>();
                        if (!target) continue;

                        ApplyEffects(source, target);
                    }

                    break;
            }
        }

        internal void ApplyEffects(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (Effects == null || Effects.Count == 0) return;

            foreach (KeyValuePair<GameplayEffect, int> kv in Effects)
            {
                target.ApplyEffect(kv.Key, source, kv.Value);
            }
        }
    }
}
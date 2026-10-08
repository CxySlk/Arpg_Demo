using System;
using System.Collections.Generic;
using GAS;
using LLKFrame;
using UnityEngine;

namespace Skill
{
    /// <summary>
    /// 能力播放器：负责按"帧"驱动一个 <see cref="AbilityAsset"/> 的时间轴帧事件。
    /// </summary>
    /// <remarks>
    /// 职责单一：只负责"帧驱动与分发"，不负责具体逻辑——具体的 Effect/Cue/帧事件处理全部转交给
    /// <see cref="SkillBehaviorBase"/>（通过 Before/AfterHandle 钩子）。
    /// 通过全局时间系统 <see cref="TimeSystem"/> 订阅逐帧回调，帧号从播放起始帧开始累计，
    /// 到达 <see cref="AbilityAsset.totalFrames"/> 后自动退订并通知技能行为播放完毕。
    /// </remarks>
    public class AbilityPlayer
    {
        /// <summary>所属技能行为（帧事件/效果/Cue 的实际处理者）。</summary>
        private SkillBehaviorBase _skillBehavior;

        #region 资源相关

        /// <summary>当前播放的能力资产（含三个按帧组织的事件字典）。</summary>
        private AbilityAsset _abilityAsset;

        /// <summary>帧 → 释放 GE 事件（同一帧最多一个 GE）。</summary>
        private Dictionary<int, ReleaseGEMarkEvent> EffectDic => _abilityAsset.effectDic;

        /// <summary>帧 → 持续 Cue 事件列表。</summary>
        private Dictionary<int, List<DurationCueMarkEvent>> CueDic => _abilityAsset.cueDic;

        /// <summary>帧 → 自定义帧事件列表。</summary>
        private Dictionary<int, List<FrameEvent>> FrameEventDic => _abilityAsset.eventDic;

        /// <summary>当前片段内处于激活期的持续 GE 事件运行时（每段开始时构建，段结束清空）。</summary>
        private readonly List<DurationGEEventRuntime> _durationRuntimes = new();

        /// <summary>能力播放完毕回调（由技能行为注入）。</summary>
        private Action _onAbilityPlayEnd;

        #endregion

        /// <summary>播放起始帧（用于把全局帧号折算成本能力的相对帧号）。</summary>
        private int _startFrame; // 播放起始帧

        /// <summary>
        /// 初始化能力播放器。
        /// </summary>
        /// <param name="skillBehavior">所属技能行为（帧事件处理者）。</param>
        /// <param name="onAbilityPlayEnd">播放完毕回调。</param>
        public void Init(SkillBehaviorBase skillBehavior, Action onAbilityPlayEnd)
        {
            _skillBehavior = skillBehavior;
            _onAbilityPlayEnd = onAbilityPlayEnd;
        }


        /// <summary>
        /// 开始播放一个能力资产。
        /// </summary>
        /// <param name="abilityAsset">要播放的能力资产。</param>
        /// <remarks>防重复订阅：先退订上次未播完的回调，再以当前全局帧号作为起始帧重新订阅。</remarks>
        public void PlayAbility(AbilityAsset abilityAsset)
        {
            _abilityAsset = abilityAsset;

            // 段与段之间隔离：先清掉上一段残留的持续事件运行时，再按新资产重建
            ClearDurationRuntimes();
            BuildDurationRuntimes();

            // 防重复订阅：先退订上次未播完的，再订阅
            TimeSystem.Global.UnSubscribe(TickAbility);
            _startFrame = TimeSystem.Global.NextFrame;
            TimeSystem.Global.Subscribe(TickAbility);
        }

        /// <summary>能力播放到最后一帧：退订帧驱动、清空持续事件记录并通知技能行为播放完毕。</summary>
        public void AbilityFrameEnd()
        {
            TimeSystem.Global.UnSubscribe(TickAbility);
            ClearDurationRuntimes();
            // 告知技能行为已经播放完毕
            _onAbilityPlayEnd.Invoke();
        }

        /// <summary>停止驱动（意外死亡等外部清理时调用，取消所有帧事件订阅）。</summary>
        public void Stop()
        {
            TimeSystem.Global.UnSubscribe(TickAbility);
            ClearDurationRuntimes();
        }

        /// <summary>为当前资产里所有持续帧数大于 1 的 GE 事件各建一个运行时。</summary>
        private void BuildDurationRuntimes()
        {
            foreach (ReleaseGEMarkEvent geEvent in EffectDic.Values)
            {
                if (geEvent == null || geEvent.GetDurationFrames() <= 1) continue;
                _durationRuntimes.Add(new DurationGEEventRuntime(geEvent));
            }
        }

        /// <summary>清空并释放当前所有持续事件运行时（命中记录一并丢弃）。</summary>
        private void ClearDurationRuntimes()
        {
            foreach (DurationGEEventRuntime runtime in _durationRuntimes)
                runtime.Dispose();
            _durationRuntimes.Clear();
        }

        #region 帧驱动相关（AbilityPlayer只负责帧的驱动，不负责具体逻辑，具体的处理逻辑扔给Behavior）

        /// <summary>每帧回调：把全局帧号折算为相对帧号，并依次驱动效果/Cue/帧事件，播完自动结束。</summary>
        private void TickAbility(int curFrame)
        {
            int frame = curFrame - _startFrame;

            TickEffects(frame);
            TickDurationEffects(frame);
            TickCue(frame);
            TickFrameEvent(frame);

            // 结束就可以取消帧驱动监听了
            if (frame >= _abilityAsset.totalFrames)
            {
                AbilityFrameEnd();
            }
        }

        /// <summary>驱动指定帧的 GE 效果：空捕捉器视为命中自身（施法者），否则按捕捉器捕获目标，逐个应用效果层数。</summary>
        private void TickEffects(int frame)
        {
            if (!EffectDic.TryGetValue(frame, out ReleaseGEMarkEvent geMarkEvent)) return;

            // 持续事件由 DurationGEEventRuntime 逐帧驱动，这里跳过，避免起始帧被重复命中
            if (geMarkEvent.GetDurationFrames() > 1) return;

            ReleaseGEMarkEvent _geMarkEvent = _skillBehavior.BeforeHandleReleaseGe(geMarkEvent);
            _skillBehavior.AfterHandleReleaseGe(_geMarkEvent);
        }

        /// <summary>驱动持续 GE 事件：对处于持续范围内的运行时逐帧 Tick，越过末尾的释放并移除。</summary>
        private void TickDurationEffects(int frame)
        {
            for (int i = _durationRuntimes.Count - 1; i >= 0; i--)
            {
                DurationGEEventRuntime runtime = _durationRuntimes[i];

                if (runtime.IsFinished(frame))
                {
                    runtime.Dispose();
                    _durationRuntimes.RemoveAt(i);
                    continue;
                }

                if (runtime.IsActive(frame))
                {
                    runtime.Enter(_skillBehavior.Owner);
                    runtime.Tick(frame);
                }
            }
        }

        /// <summary>驱动指定帧的持续 Cue：把 Cue 模板交给技能行为应用（应用与否由行为与标签条件决定）。</summary>
        private void TickCue(int frame)
        {
            if (!CueDic.TryGetValue(frame, out List<DurationCueMarkEvent> cueList)) return;

            foreach (DurationCueMarkEvent _cue in cueList)
            {
                if (_cue == null || _cue.DurationCue == null) continue;
                GameplayCueDurational cue = _skillBehavior.BeforeHandleCue(_cue.DurationCue);
                _skillBehavior.AfterHandleCue(cue);
            }
        }

        /// <summary>驱动指定帧的自定义帧事件：深拷贝后交给技能行为处理并销毁拷贝。</summary>
        /// <param name="frame">相对帧号。</param>
        private void TickFrameEvent(int frame)
        {
            if (!FrameEventDic.TryGetValue(frame, out List<FrameEvent> frameEventList)) return;

            foreach (FrameEvent _frameEvent in frameEventList)
            {
                if (_frameEvent == null) continue;

                // 先深拷贝出运行时副本再处理/销毁，避免就地破坏 AbilityAsset 里的共享事件对象（清空字段 + 入对象池）
                FrameEvent template = _skillBehavior.BeforeHandleFrameEvent(_frameEvent);
                if (template == null) continue;
                FrameEvent frameEvent = template.DeepCopy();
                _skillBehavior.AfterHandleFrameEvent(frameEvent);
                frameEvent.Destroy();
            }
        }

        #endregion
    }
}
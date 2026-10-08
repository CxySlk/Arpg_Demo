using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 持续释放 GE 事件的运行时实例。在 [startFrame, lastFrame]（含两端）内每帧监测目标，
    /// 按"只作用一次 / 可重复+间隔"规则决定是否对该目标应用效果。
    /// </summary>
    /// <remarks>
    /// 每帧命中的运行时状态（目标 → 上次作用帧）只存在这里，不写回共享的
    /// <see cref="ReleaseGEMarkEvent"/> 资产；每个施法者的 <c>AbilityPlayer</c> 各持有一份，
    /// 因此多个角色共用同一个事件资产时互不干扰。段结束时由持有者 <see cref="Dispose"/> 清空。
    /// </remarks>
    public sealed class DurationGEEventRuntime
    {
        private readonly ReleaseGEMarkEvent _event;
        private readonly int _startFrame;
        private readonly int _lastFrame; // 含（闭区间最后一帧）
        private readonly bool _isWeaponMode; // 武器模式：不逐帧采集，只在 Enter/Dispose 开/关窗口

        /// <summary>目标 → 上一次被作用的帧号。</summary>
        private readonly Dictionary<AbilitySystemComponent, int> _lastHitFrame = new();

        /// <summary>施法者（Enter 时保存，Tick/TryApply 复用）。</summary>
        private AbilitySystemComponent _source;

        /// <summary>是否已进入激活区间（保证 Enter 只执行一次）。</summary>
        private bool _entered;

        /// <summary>最近一次 Tick 的相对帧号（武器命中回调用它做间隔判定）。</summary>
        private int _currentFrame;

        public DurationGEEventRuntime(ReleaseGEMarkEvent markEvent)
        {
            _event = markEvent;
            _startFrame = markEvent.StartFrame;
            _lastFrame = _startFrame + markEvent.GetDurationFrames() - 1;
            _isWeaponMode = markEvent.targetCatcher is WeaponCatcher;
        }

        /// <summary>当前帧是否在本事件的持续范围内。</summary>
        public bool IsActive(int frame) => frame >= _startFrame && frame <= _lastFrame;

        /// <summary>当前帧是否已越过持续范围末尾。</summary>
        public bool IsFinished(int frame) => frame > _lastFrame;

        /// <summary>进入激活区间（首次调用生效）：保存施法者；武器模式下发布"开启武器碰撞"事件。</summary>
        public void Enter(AbilitySystemComponent source)
        {
            if (_entered) return;
            _entered = true;
            _source = source;

            if (_isWeaponMode)
            {
                WeaponCatcher weaponCatcher = _event.targetCatcher as WeaponCatcher;
                source.GasEventCenter.Publish(GASEventCenterKeys.StartWeaponColliderKey,
                    weaponCatcher.weaponKeys, weaponCatcher.LayerMask);
                source.GasEventCenter.Subscribe(GASEventCenterKeys.WeaponHitTarget, WeaponHitTarget);
            }
        }

        /// <summary>驱动一帧：捕捉范围内的目标并按规则应用效果。武器模式不逐帧采集。</summary>
        public void Tick(int frame)
        {
            _currentFrame = frame;

            // 武器模式跳过
            if (_isWeaponMode) return;
            
            switch (_event.targetCatcher)
            {
                case null:
                    TryApply(_source, frame);
                    break;
                default:
                    Collider[] targets = _event.targetCatcher.GetTargets(_source.transform);
                    for (int i = 0; i < targets.Length && targets[i]; i++)
                    {
                        AbilitySystemComponent target = targets[i].GetComponentInParent<AbilitySystemComponent>();
                        if (!target) continue;

                        TryApply(target, frame);
                    }
                    break;
            }
        }

        /// <summary>武器命中回调（武器触发器发布）：取目标 ASC 后按规则结算。</summary>
        private void WeaponHitTarget(Collider target)
        {
            // 命中盒常在子物体上、ASC 在根物体：统一用 GetComponentInParent
            AbilitySystemComponent targetAsc = target.GetComponentInParent<AbilitySystemComponent>();
            if (!targetAsc) return;

            TryApply(targetAsc, _currentFrame);
        }

        /// <summary>按"只作用一次 / 可重复+间隔"规则决定是否对该目标应用效果。</summary>
        private void TryApply(AbilitySystemComponent target, int frame)
        {
            if (_lastHitFrame.TryGetValue(target, out int lastFrame))
            {
                // 只作用一次：整段内已命中过，直接跳过
                if (!_event.canRepeat) return;

                // 间隔帧数 0 或 1 都等价于"每帧作用一次"
                if (frame - lastFrame < Mathf.Max(1, _event.hitIntervalFrames)) return;
            }

            _event.ApplyEffects(_source, target);
            _lastHitFrame[target] = frame;
        }

        /// <summary>清空命中记录（段结束 / 被打断 / 停止驱动时调用）。仅进过 Enter 且武器模式才发关闭事件。</summary>
        public void Dispose()
        {
            // 关闭武器 + 清理订阅（仅进过 Enter 且武器模式才做过，避免未进入时 _source 为空）
            if (_entered && _isWeaponMode)
            {
                _source.GasEventCenter.Publish(GASEventCenterKeys.StopAllWeaponColliderKey);
                _source.GasEventCenter.UnSubscribe(GASEventCenterKeys.WeaponHitTarget, WeaponHitTarget);
            }

            _entered = false;
            _source = null;
            _lastHitFrame.Clear();
        }
    }
}
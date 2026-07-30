using LLKFrame;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 效果运行时实例（EffectSpec）。每个激活的持续/永久效果对应一个 Spec，
    /// 负责 Tick 驱动（周期触发、到期移除）、属性的 Application/Undo、叠层管理。
    /// </summary>
    public class GameplayEffectSpec
    {
        private AbilitySystemComponent _owner;
        private GameplayEffect _effect;
        /// <summary>效果起始帧</summary>
        private int _startFrameIndex;
        /// <summary>上次周期触发的帧</summary>
        private int _lastPeriodFrame;
        /// <summary>当前叠层数</summary>
        private int _layer;

        /// <summary>效果当前是否处于激活状态（受 TagIsDirty 影响）</summary>
        private bool _isActive;
        public bool IsActive => _isActive;

        /// <summary>效果标识符</summary>
        public EntityId AssetEntityID => _effect.AssetEntityID;

        #region 标签 / Cue 代理

        public GameplayTagSet RequiredTags => _effect.RequiredTags;
        public GameplayTagSet BlockedTags => _effect.BlockedTags;
        public GameplayTagSet GrantTags => _effect.GrantTags;
        public GameplayTagSet RemovedTags => _effect.RemovedTags;

        public GameplayCue[] OnExcuteCues => _effect.OnExcuteCues;
        public GameplayCue[] OnAddCues => _effect.OnAddCues;

        #endregion

        #region 生命周期

        /// <summary>从对象池获取或新建实例</summary>
        public static GameplayEffectSpec Create() => PoolSystem.GetOrNewObject<GameplayEffectSpec>();

        /// <summary>初始化 Spec，关联所有者 ASC 和底层 Effect 数据</summary>
        public void Init(AbilitySystemComponent owner, GameplayEffect effect, int currentFrame)
        {
            _owner = owner;
            _effect = effect;
            _startFrameIndex = currentFrame;
            _lastPeriodFrame = _startFrameIndex;
            _layer = 1;
        }

        /// <summary>
        /// 每帧 Tick：检查周期触发条件 → 应用属性修改 + 触发 OnExcuteCues；
        /// 检查持续时间到期 → 移除或降层。
        /// </summary>
        public void Tick(int currentFrame)
        {
            // 周期应用属性修改
            if (_isActive && _effect.PeriodicFrame > 0 && currentFrame - _lastPeriodFrame >= _effect.PeriodicFrame)
            {
                if (_effect.AttrModifiers != null)
                    _owner.ApplyAttrModifiers(_effect.AttrModifiers, false);
                _lastPeriodFrame = currentFrame;

                if (OnExcuteCues != null)
                    for (int i = 0; i < OnExcuteCues.Length; i++)
                        OnExcuteCues[i].ApplyFrom(_owner);
            }

            // 持续效果到期处理
            if (_effect.DurationType == EffectDurationType.Durational &&
                currentFrame - _startFrameIndex >= _effect.DurationFrame)
            {
                switch (_effect.StackData.DecreaseType)
                {
                    case EffectStackDecreaseType.Clear:
                        _owner.RemoveEffectSpec(this);
                        break;
                    case EffectStackDecreaseType.Refresh:
                        _layer--;
                        if (_layer <= 0) _owner.RemoveEffectSpec(this);
                        else _startFrameIndex = currentFrame;
                        break;
                }
            }
        }

        /// <summary>销毁 Spec，释放底层 Effect 并回收到对象池</summary>
        public void Destroy()
        {
            _owner = null;
            _effect.Destroy();
            _effect = null;
            this.PushObject();
        }

        #endregion

        /// <summary>切换激活状态</summary>
        public void SetActive(bool active) => _isActive = active;

        /// <summary>撤销所有标记了 ExperiedAutoRestore 的属性修改</summary>
        public void UndoAttributeModifiers()
        {
            if (_effect.AttrModifiers == null) return;
            for (int i = 0; i < _effect.AttrModifiers.Length; i++)
            {
                var modify = _effect.AttrModifiers[i];
                if (modify.ExperiedAutoRestore)
                    _owner.UndoAttributeModifiers(modify);
            }
        }

        /// <summary>重新应用属性修改（配合 ApplyStack 使用）</summary>
        public void ApplyAttrModifiers()
        {
            if (_effect.AttrModifiers != null)
                _owner.ApplyAttrModifiers(_effect.AttrModifiers, false);
        }

        /// <summary>
        /// 尝试叠层。达到最大叠层数时返回 false，否则层数 +1。
        /// StackAndRefresh 模式下重置持续时间。
        /// </summary>
        public bool ApplyStack()
        {
            int max = _effect.StackData.MaxStackCount;
            if (_layer >= max) return false;
            _layer = Mathf.Min(max, _layer + 1);
            switch (_effect.StackData.AggregateType)
            {
                case EffectStackAggregateType.StackAndRefresh:
                    _startFrameIndex = GASHost.Instance.Timer.CurrentFrameCount;
                    break;
            }
            return true;
        }
    }
}

using LLKFrame;
using UnityEngine;

namespace GAS
{
    public class GameplayEffectSpec
    {
        private AbilitySystemComponent _owner;
        private GameplayEffect _effect;
        private int _startFrameIndex;   // 起始帧
        private int _lastPeriodFrame;   // 上一次触发周期的帧
        private int _layer;

        private bool _isActive;
        public bool IsActive => _isActive;
        public EntityId AssetEntityID => _effect.AssetEntityID;
        #region 标签相关
        public GameplayTagSet RequiredTags => _effect.RequiredTags;
        public GameplayTagSet BlockedTags => _effect.BlockedTags;
        public GameplayTagSet GrantTags => _effect.GrantTags;
        public GameplayTagSet RemovedTags => _effect.RemovedTags;
        #endregion
        #region Cue表现层相关
        public GameplayCue[] OnExcuteCues => _effect.OnExcuteCues;
        public GameplayCue[] OnAddCues => _effect.OnAddCues;
        #endregion
        #region 生命周期相关
        public static GameplayEffectSpec Create() => PoolSystem.GetOrNewObject<GameplayEffectSpec>();
        public void Init(AbilitySystemComponent owner, GameplayEffect effect, int currentFrame)
        {
            _owner = owner;
            _effect = effect;
            _startFrameIndex = currentFrame;
            _lastPeriodFrame = _startFrameIndex;
            _layer = 1;
        }

        public void Tick(int currentFrame)
        {
            // 周期应用属性修改，PeriodicFrame只有大于0才会生效，小于等于0不会周期性的执行效果
            if (_isActive && _effect.PeriodicFrame > 0 && currentFrame - _lastPeriodFrame >= _effect.PeriodicFrame)
            {
                if (_effect.AttrModifiers != null)
                    _owner.ApplyAttrModifiers(_effect.AttrModifiers, false);
                _lastPeriodFrame = currentFrame;

                if (OnExcuteCues != null)
                    for (int i = 0; i < OnExcuteCues.Length; i++)
                        OnExcuteCues[i].ApplyFrom(_owner);
            }

            // 减少层数
            if (_effect.DurationType == EffectDurationType.Durational &&
                currentFrame - _startFrameIndex >= _effect.DurationFrame)
            {
                switch (_effect.StackData.DecreaseType)
                {
                    // 时间到了直接清空移除
                    case EffectStackDecreaseType.Clear:
                        _owner.RemoveEffectSpec(this);
                        break;
                    // 时间到了会降低层数，层数为0移除
                    case EffectStackDecreaseType.Refresh:
                        _layer--;
                        if (_layer <= 0) _owner.RemoveEffectSpec(this);
                        else _startFrameIndex = currentFrame;
                        break;
                }
            }
        }

        public void Destroy()
        {
            _owner = null;
            _effect.Destroy();
            _effect = null;
            this.PushObject();
        }
        #endregion
        public void SetActive(bool active) => _isActive = active;

        /// <summary>撤销属性修改</summary>
        public void UndoAttributeModifiers()
        {
            if (_effect.AttrModifiers == null) return;
            for (int i = 0; i < _effect.AttrModifiers.Length; i++)
            {
                GameplayEffectAttrModifier modify = _effect.AttrModifiers[i];
                // 只有过期自动恢复的才会如此
                if (modify.ExperiedAutoRestore)
                    _owner.UndoAttributeModifiers(modify);
            }
        }

        public void ApplyAttrModifiers()
        {
            if (_effect.AttrModifiers != null)
                _owner.ApplyAttrModifiers(_effect.AttrModifiers, false);
        }

        public bool ApplyStack()
        {
            int max = _effect.StackData.MaxStackCount;
            if (_layer >= max) return false;
            _layer = Mathf.Min(max, _layer + 1);
            switch (_effect.StackData.AggregateType)
            {
                case EffectStackAggregateType.StackAndRefresh:
                    _startFrameIndex = GASTimer.CurrentFrameCount;
                    break;
            }
            return true;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 效果应用/移除/生命周期管理。由 AbilitySystemComponent 持有。
    /// </summary>
    public partial class AbilitySystemComponent
    {
        /// <summary>当前激活的效果列表（EntityId, EffectSpec）</summary>
        private readonly List<(EntityId id, GameplayEffectSpec effectSpec)> _activeEffects = new List<(EntityId, GameplayEffectSpec)>();
        /// <summary>Tick 时使用的缓存列表，避免在迭代中修改原列表</summary>
        private readonly List<(EntityId id, GameplayEffectSpec effectSpec)> _cacheEffects = new List<(EntityId, GameplayEffectSpec)>();
        /// <summary>标签是否已变脏，需要重新检查效果激活状态</summary>
        private bool _tagIsDirty = true;

        #region Effect 应用/移除

        /// <summary>
        /// 应用一个 GameplayEffect。
        /// 根据效果的 DurationType 决定是立即生效(Instance)还是持续生效(Infinite/Durational)。
        /// 持续效果会创建 EffectSpec 进行跟踪，到期后自动移除。
        /// </summary>
        /// <param name="effectAsset">效果资产</param>
        public void ApplyEffect(GameplayEffectAsset effectAsset)
        {
            if (effectAsset == null) return;

            // 检查 RequiredTags：所有者必须具备所有必需标签
            if (effectAsset.RequiredTags != null && !effectAsset.RequiredTags.Empty
                && !_tagAggregator.CheckAllTags(effectAsset.RequiredTags)) return;

            // 检查 BlockedTags：所有者不能有任何阻塞标签
            if (effectAsset.BlockedTags != null && !effectAsset.BlockedTags.Empty
                && _tagAggregator.CheckAnyTags(effectAsset.BlockedTags)) return;

            // 效果已存在 → 尝试叠层
            if (TryGetEffectSpec(effectAsset.AssetEntityID, out var effectSpec))
            {
                if (effectSpec.ApplyStack())
                    effectSpec.ApplyAttrModifiers();
                return;
            }

            // 新效果 → 应用
            ApplyNewEffect(effectAsset);
        }

        /// <summary>
        /// 首次应用一个效果（效果尚不存在于 _activeEffects 中）。
        /// 处理标签授予/移除、Cue 触发、并根据 DurationType 分派。
        /// </summary>
        private void ApplyNewEffect(GameplayEffectAsset effectAsset)
        {
            // 授予标签
            if (effectAsset.GrantTags != null && !effectAsset.GrantTags.Empty)
                AddDynamicTags(effectAsset.GrantTags);
            // 移除标签
            if (effectAsset.RemovedTags != null && !effectAsset.RemovedTags.Empty)
                RemoveDynamicTags(effectAsset.RemovedTags);

            var effect = GameplayEffect.Create();
            effect.Init(effectAsset);

            // 触发执行时 Cue（所有类型效果都触发）
            TriggerCues(effect.OnExcuteCues);

            switch (effect.DurationType)
            {
                case EffectDurationType.Instance:
                    // 立即效果：应用属性修改后立即销毁
                    ApplyAttrModifiers(effect);
                    effect.Destroy();
                    break;
                case EffectDurationType.Infinite:
                case EffectDurationType.Durational:
                    // 持续/永久效果：创建 Spec 跟踪，触发添加时 Cue
                    var spec = GameplayEffectSpec.Create();
                    spec.Init(this, effect, CurrentFrame);
                    _activeEffects.Add((spec.AssetEntityID, spec));
                    spec.ApplyAttrModifiers();
                    TriggerCues(effect.OnAddCues);
                    break;
            }
        }

        /// <summary>
        /// 通过效果资产移除一个效果。找到对应的 EffectSpec 后调用 RemoveEffectSpec。
        /// </summary>
        /// <param name="effectAsset">效果资产</param>
        public void DisApplyEffect(GameplayEffectAsset effectAsset)
        {
            if (effectAsset == null) return;
            if (TryGetEffectSpec(effectAsset.AssetEntityID, out var spec))
                RemoveEffectSpec(spec);
        }

        /// <summary>
        /// 移除一个 EffectSpec：撤销属性修改、回收标签、销毁 Spec。
        /// </summary>
        public void RemoveEffectSpec(GameplayEffectSpec effectSpec)
        {
            effectSpec.UndoAttributeModifiers();

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].id == effectSpec.AssetEntityID)
                {
                    _activeEffects.RemoveAt(i);
                    break;
                }
            }

            // 通过 _tagIsDirty 旗标抑制中间状态的 TagIsDirty 触发
            _tagIsDirty = false;
            AddDynamicTags(effectSpec.RemovedTags);
            _tagIsDirty = true;
            RemoveDynamicTags(effectSpec.GrantTags);

            effectSpec.Destroy();
        }

        /// <summary>
        /// 根据 EntityId 查找已激活的效果 Spec。
        /// </summary>
        private bool TryGetEffectSpec(EntityId id, out GameplayEffectSpec spec)
        {
            foreach (var (effectId, effectSpec) in _activeEffects)
            {
                if (id == effectId)
                {
                    spec = effectSpec;
                    return true;
                }
            }
            spec = null;
            return false;
        }

        /// <summary>
        /// 触发一组 Cue 的 ApplyFrom（若数组非空）。
        /// </summary>
        private void TriggerCues(GameplayCue[] cues)
        {
            if (cues == null) return;
            for (int i = 0; i < cues.Length; i++)
                cues[i].ApplyFrom(this);
        }

        #endregion

        #region Tick

        /// <summary>
        /// 每游戏帧驱动所有激活 EffectSpec 的 Tick。
        /// </summary>
        private void TickEffects(int curFrame)
        {
            _cacheEffects.Clear();
            _cacheEffects.AddRange(_activeEffects);
            for (int i = 0; i < _cacheEffects.Count; i++)
                _cacheEffects[i].effectSpec.Tick(curFrame);
        }

        #endregion

        #region TagIsDirty

        /// <summary>
        /// 标签发生变化时重新检查所有效果的激活状态。
        /// 不满足条件的 EffectSpec 被失活（撤销属性修改），
        /// 恢复条件的 EffectSpec 被重新激活。
        /// </summary>
        public void TagIsDirty()
        {
            if (!_tagIsDirty) return;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var (_, effectSpec) = _activeEffects[i];
                var blocked = CheckAnyTags(effectSpec.BlockedTags);
                var missing = !CheckAllTags(effectSpec.RequiredTags);

                if (blocked || missing)
                {
                    if (effectSpec.IsActive)
                    {
                        effectSpec.SetActive(false);
                        effectSpec.UndoAttributeModifiers();
                    }
                }
                else
                {
                    if (!effectSpec.IsActive)
                        effectSpec.SetActive(true);
                }
            }
        }

        #endregion

        #region IEffectTarget

        /// <summary>
        /// 带来源参数的效果应用。来源 ASC 当前仅预留，后续用于伤害来源追踪。
        /// </summary>
        void IEffectTarget.ApplyEffect(GameplayEffectAsset effectAsset, AbilitySystemComponent source)
        {
            ApplyEffect(effectAsset);
        }

        void IEffectTarget.DisApplyEffect(GameplayEffectAsset effectAsset, AbilitySystemComponent source)
        {
            DisApplyEffect(effectAsset);
        }

        #endregion
    }
}

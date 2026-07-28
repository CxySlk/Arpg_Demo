using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public class AbilitySystemComponent : SerializedMonoBehaviour
    {
        /// <summary>List<(效果EntityID，效果实例)></summary>
        [ShowInInspector, ReadOnly] private readonly List<(EntityId id, GameplayEffectSpec effectSpec)> _activeEffects = new List<(EntityId id, GameplayEffectSpec effectSpec)>();
        [SerializeField] private AttributeSet _attrDic;
        [SerializeField] private GameplayTagAggregator _tagAggregator;
        [SerializeField] private AbilitySystemComponentPreset _preset;

        private bool _tagIsDirty = true;
        public int CurrentFrame { get; private set; }

        private List<(EntityId id, GameplayEffectSpec effectSpec)> _cacheEffects = new List<(EntityId id, GameplayEffectSpec effectSpec)>();
        #region 生命周期
        private void Awake()
        {
            if (_preset != null)
                InitForPreset();
        }
        private void OnEnable() => GameplayAbilitySystom.RegisterActivateASC(this);
        private void OnDisable() => GameplayAbilitySystom.UnRegisterActivateASC(this);
        private void OnDestroy()
        {
            //_ownerTag.Destroy();
            //foreach (var kv in _attrContainers)
            //    kv.Value.Destroy();
            //_attrContainers.Clear();
        }
        #endregion
        #region 初始化
        public void InitForPreset()
        {
            _attrDic = AttributeSet.Create();
            foreach (Attribute attr in _preset.Attributes)
                _attrDic.AttrDic.Add(attr.HashCode, attr);
            _tagAggregator = GameplayTagAggregator.Create();
            _tagAggregator.Init(this, _preset.TagSet);
        }
        #endregion
        #region 每帧 Tick（由 GameplayAbilitySystom 驱动）
        /// <summary>
        /// 游戏帧驱动：
        /// 1. 检查过期效果 → 回收标签 + 回退属性 + 添加移除标签
        /// 2. 触发到期效果的重算周期
        /// </summary>
        public void Tick(int curFrame)
        {
            CurrentFrame = curFrame;
            _cacheEffects.Clear();
            _cacheEffects.AddRange(_activeEffects);
            for (int i = 0; i < _cacheEffects.Count; i++)
                _cacheEffects[i].effectSpec.Tick(curFrame);
        }
        #endregion
        #region Effect相关
        private bool TryGetEffectSpec(EntityId id, out GameplayEffectSpec spec)
        {
            spec = null;
            foreach ((EntityId _id, GameplayEffectSpec _spec) in _activeEffects)
            {
                if (id == _id)
                {
                    spec = _spec;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 应用效果
        /// </summary>
        /// <param name="effectAsset"></param>
        public void ApplyEffect(GameplayEffectAsset effectAsset)
        {
            if (effectAsset == null) return;
            // 没有Effect需要的标签
            if (effectAsset.RequiredTags != null && !effectAsset.RequiredTags.Empty && !_tagAggregator.CheckAllTags(effectAsset.RequiredTags)) return;
            // 有阻塞标签
            if (effectAsset.BlockedTags != null && !effectAsset.BlockedTags.Empty && _tagAggregator.CheckAnyTags(effectAsset.BlockedTags)) return;

            // 效果已经存在
            if (TryGetEffectSpec(effectAsset.AssetEntityID, out GameplayEffectSpec effectSpec))
            {
                if (effectSpec.ApplyStack())
                    effectSpec.ApplyAttrModifiers();
            }
            // 效果未存在
            else
            {
                // 授予标签
                if (effectAsset.GrantTags != null && !effectAsset.GrantTags.Empty)
                    AddDynamicTags(effectAsset.GrantTags);
                // 移除标签
                if (effectAsset.RemovedTags != null && !effectAsset.RemovedTags.Empty)
                    RemoveDynamicTags(effectAsset.RemovedTags);

                GameplayEffect effect = GameplayEffect.Create();
                effect.Init(effectAsset);

                if (effect.OnExcuteCues != null)
                    for (int i = 0; i < effect.OnExcuteCues.Length; i++)
                        effect.OnExcuteCues[i].ApplyFrom(this);

                switch (effect.DurationType)
                {
                    case EffectDurationType.Instance:
                        ApplyAttrModifiers(effect);
                        effect.Destroy();
                        break;
                    case EffectDurationType.Infinite:
                    case EffectDurationType.Durational:
                        effectSpec = GameplayEffectSpec.Create();
                        effectSpec.Init(this, effect, CurrentFrame);
                        _activeEffects.Add((effectSpec.AssetEntityID, effectSpec));
                        effectSpec.ApplyAttrModifiers();
                        // AddCue
                        if (effect.OnAddCues != null)
                            for (int i = 0; i < effect.OnAddCues.Length; i++)
                                effect.OnAddCues[i].ApplyFrom(this);
                        break;
                    default:
                        break;
                }

            }
        }
        /// <summary>
        /// 移除效果
        /// </summary>
        /// <param name="effectAsset"></param>
        public void DisApplyEffect(GameplayEffectAsset effectAsset)
        {
            if (effectAsset == null) return;
            // 效果已经存在
            if (TryGetEffectSpec(effectAsset.AssetEntityID, out GameplayEffectSpec effectSpec))
                RemoveEffectSpec(effectSpec);
        }

        /// <summary>
        /// 移除效果Spec
        /// </summary>
        public void RemoveEffectSpec(GameplayEffectSpec effectSpec)
        {
            // 撤销属性修改
            effectSpec.UndoAttributeModifiers();
            // 回收EffectSpec
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].id == effectSpec.AssetEntityID)
                {
                    _activeEffects.RemoveAt(i);
                    break;
                }
            }
            _tagIsDirty = false;
            AddDynamicTags(effectSpec.RemovedTags);
            _tagIsDirty = true;
            RemoveDynamicTags(effectSpec.GrantTags);

            effectSpec.Destroy();
        }

        //private 
        #endregion
        #region 标签相关
        public void AddFixedTags(GameplayTagSet tagSet) => _tagAggregator.AddFixedTags(tagSet);
        public void RemoveFixedTags(GameplayTagSet tagSet) => _tagAggregator.RemoveFixedTags(tagSet);
        public void AddDynamicTags(GameplayTagSet tagSet) => _tagAggregator.AddDynamicTags(tagSet);
        public void RemoveDynamicTags(GameplayTagSet tagSet) => _tagAggregator.RemoveDynamicTags(tagSet);

        public bool CheckAnyTags(GameplayTagSet tagSet) => _tagAggregator.CheckAnyTags(tagSet);
        public bool CheckAllTags(GameplayTagSet tagSet) => _tagAggregator.CheckAllTags(tagSet);

        public void TagIsDirty()
        {
            // 只有标签脏了，才会下一步
            if (!_tagIsDirty) return;

            // TODO：重新计算Attr
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                (EntityId id, GameplayEffectSpec effectSpec) = _activeEffects[i];
                // 该EffectSpec应该被失活
                if (!CheckAllTags(effectSpec.RequiredTags) || CheckAnyTags(effectSpec.BlockedTags))
                {
                    // 本应该失活，但是仍然是激活状态，所以需要撤销属性修改
                    if (effectSpec.IsActive)
                    {
                        effectSpec.SetActive(false);
                        effectSpec.UndoAttributeModifiers();
                    }
                }
                // 该EffectSpec应该被激活
                else
                {
                    // 激活该Spec
                    if (!effectSpec.IsActive)
                        effectSpec.SetActive(true);
                }
            }
        }
        #endregion
        #region 属性
        /// <summary>
        /// 应用属性修改
        /// </summary>
        /// <param name="attrModifiers"></param>
        public void ApplyAttrModifiers(GameplayEffectAttrModifier[] attrModifiers, bool instantModify)
        {
            for (int i = 0; i < attrModifiers.Length; i++)
            {
                int attrHashCode = attrModifiers[i].AttrHashCode;
                if (!_attrDic.HasAttr(attrHashCode)) continue;

                // 应用属性修改器
                attrModifiers[i].ApplyModify(_attrDic.AttrDic[attrHashCode], instantModify);
            }
        }
        /// <summary>
        /// 应用属性修改
        /// </summary>
        /// <param name="attrModifiers"></param>
        /// <param name="assetEntityID"></param>
        public void ApplyAttrModifiers(GameplayEffect effect)
        {
            if (effect.AttrModifiers != null)
                ApplyAttrModifiers(effect.AttrModifiers, effect.DurationType == EffectDurationType.Instance);
        }

        /// <summary>
        /// 撤销属性修改
        /// </summary>
        public void UndoAttributeModifiers(GameplayEffectAttrModifier modify)
        {
            if (!_attrDic.AttrDic.TryGetValue(modify.AttrHashCode, out Attribute attr)) return;

            switch (modify.ModifierTarget)
            {
                case AttributeModifyTarget.CurrentValue:
                    attr.AddCurrentValue(-modify.RealModifyValue, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MinValue:
                    attr.AddMinValue(-modify.RealModifyValue, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MaxValue:
                    attr.AddMaxValue(-modify.RealModifyValue, AttributeDataType.CurrentValue);
                    break;
            }
            modify.RealModifyValue = 0;
        }
        #endregion
    }
}

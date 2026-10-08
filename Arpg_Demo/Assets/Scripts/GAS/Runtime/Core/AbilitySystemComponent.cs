using System;
using System.Collections.Generic;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GAS 核心组件，挂载在角色 GameObject 上。
    /// 管理属性的初始化、标签系统接入、以及每帧 Tick 驱动。
    /// 具体的 Effect 应用/移除 和 属性修改 分别在 partial 文件中实现。
    /// </summary>
    public class AbilitySystemComponent : SerializedMonoBehaviour
    {
        [FoldoutGroup("运行时数据", expanded: false)]
        [GUIColor(0.25f, 0.56f, 1f)]
        [DictionaryDrawerSettings(KeyLabel = "属性类型", ValueLabel = "属性")]
        [LabelText("属性集")]
        [ShowInInspector, ReadOnly]
        private Dictionary<AttributeType, Attribute> _attrDic = new();

        [FoldoutGroup("运行时数据")] [GUIColor(0.30f, 0.80f, 0.45f)] [LabelText("标签集"), ShowInInspector, ReadOnly]
        private GameplayTagAggregator _tagAggregator = new();

        [FoldoutGroup("运行时数据")]
        [GUIColor(1f, 0.62f, 0.20f)]
        [DictionaryDrawerSettings(KeyLabel = "效果标签", ValueLabel = "效果")]
        [LabelText("效果集"), ShowInInspector, ReadOnly]
        private Dictionary<GameplayTag, GameplayDurationalEffect> _effectDic = new();

        /// <summary>驱动 OnUpdate 时用的缓存列表（复用一个，避免每帧分配）</summary>
        private readonly List<GameplayDurationalEffect> _effectUpdateCache = new();

        public Dictionary<AttributeType, Attribute> AttrDic => _attrDic;
        public GameplayTagAggregator TagAggregator => _tagAggregator;
        private readonly EventCenter _gasEventCenter = new();
        public EventCenter GasEventCenter => _gasEventCenter;

        #region 生命周期

        public void InitForPreset(AbilitySystemComponentPreset ascPreset)
        {
            // 属性初始化
            foreach (KeyValuePair<AttributeType, Attribute> _attr in ascPreset.AttrDic)
            {
                Attribute attr = new(_attr.Value);
                _attrDic.Add(_attr.Key, attr);
                _gasEventCenter.Publish(GASEventCenterKeys.AddAttributeKey, attr);
            }

            // 标签初始化
            _tagAggregator.AddFixedTags(ascPreset.TagSet);

            // 应用默认的效果
            foreach (GameplayEffect effect in ascPreset.Effects)
                ApplyEffect(effect, this);
        }

        private void OnEnable()
        {
            // 遍历副本，避免 Enter/Exit 过程中修改 _effectDic 导致"集合被修改"异常
            List<GameplayDurationalEffect> effects = new (_effectDic.Values);
            foreach (GameplayDurationalEffect effect in effects)
                effect.Enter();
        }

        private void OnDisable()
        {
            List<GameplayDurationalEffect> effects = new (_effectDic.Values);
            foreach (GameplayDurationalEffect effect in effects)
                effect.Exit();
        }

        /// <summary>
        /// 驱动身上所有持续效果的 OnUpdate。效果自己用秒计时，不再订阅帧事件；
        /// 暂停/时间缩放由 TimeSystem 改 Time.timeScale 实现，所以暂停时 deltaTime 就是 0（自然不推进）。
        /// </summary>
        private void Update()
        {
            if (_effectDic.Count == 0) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            // 遍历副本：OnUpdate 里可能叠层、到期移除，直接遍历 _effectDic 会抛"集合被修改"
            _effectUpdateCache.Clear();
            _effectUpdateCache.AddRange(_effectDic.Values);
            foreach (GameplayDurationalEffect effect in _effectUpdateCache)
                effect.OnUpdate(deltaTime);
        }

        #endregion

        #region 属性相关

        /// <summary>根据属性类型检查是否存在</summary>
        public bool HasAttr(AttributeType attrType) => _attrDic.ContainsKey(attrType);

        /// <summary>尝试获取属性</summary>
        public bool TryGetAttrValue(AttributeType attrType, out Attribute attr) =>
            _attrDic.TryGetValue(attrType, out attr);

        /// <summary>设置属性值</summary>
        public void SetAttrValue(AttributeType attrType, AttributeValueType valueType, float attrValue)
        {
            if (_attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.SetCurrentValue(attrValue);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.SetMinValue(attrValue);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.SetMaxValue(attrValue);
                        break;
                }
            }
        }

        /// <summary>
        /// 属性值增加，返回**实际改动量**（被上下限截断之后真正变了多少）。
        /// 例：满血时回血返回 0，溢出的伤害返回被截断后的值 —— 飘字/结算用它才不会显示假数字。
        /// </summary>
        public float AddAttrValue(AttributeType attrType, AttributeValueType valueType, float addValue)
        {
            if (!_attrDic.TryGetValue(attrType, out Attribute attribute)) return 0f;

            switch (valueType)
            {
                case AttributeValueType.CurValue:
                    float curBefore = attribute.CurValue;
                    attribute.AddCurrentValue(addValue);
                    return attribute.CurValue - curBefore;

                case AttributeValueType.MinValue:
                    float minBefore = attribute.MinValue;
                    attribute.AddMinValue(addValue);
                    return attribute.MinValue - minBefore;

                case AttributeValueType.MaxValue:
                    float maxBefore = attribute.MaxValue;
                    attribute.AddMaxValue(addValue);
                    return attribute.MaxValue - maxBefore;

                default:
                    return 0f;
            }
        }

        public void AddAttrValueChangedCallBack(AttributeType attrType, AttributeValueType valueType,
            Action<float> callBack)
        {
            if (_attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.AddCurValueChanged(callBack);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.AddMinValueChanged(callBack);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.AddMaxValueChanged(callBack);
                        break;
                    case AttributeValueType.NormalizeValue:
                        attribute.AddNormalizeValueChanged(callBack);
                        break;
                }
            }
        }

        public void SetAttrValueChangedCallBack(AttributeType attrType, AttributeValueType valueType,
            Action<float> callBack)
        {
            if (_attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.SetCurValueChanged(callBack);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.SetMinValueChanged(callBack);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.SetMaxValueChanged(callBack);
                        break;
                    case AttributeValueType.NormalizeValue:
                        attribute.SetNormalizeValueChanged(callBack);
                        break;
                }
            }
        }

        public void RemoveAttrValueChangedCallBack(AttributeType attrType, AttributeValueType valueType,
            Action<float> callBack)
        {
            if (_attrDic.TryGetValue(attrType, out Attribute attribute))
            {
                switch (valueType)
                {
                    case AttributeValueType.CurValue:
                        attribute.RemoveCurValueChanged(callBack);
                        break;
                    case AttributeValueType.MinValue:
                        attribute.RemoveMinValueChanged(callBack);
                        break;
                    case AttributeValueType.MaxValue:
                        attribute.RemoveMaxValueChanged(callBack);
                        break;
                    case AttributeValueType.NormalizeValue:
                        attribute.RemoveNormalizeValueChanged(callBack);
                        break;
                }
            }
        }

        #endregion

        #region 标签相关 (转发到 GameplayTagAggregator)

        public void AddFixedTag(GameplayTag tag) => _tagAggregator.AddFixedTag(tag);
        public void AddFixedTags(GameplayTagSet tagSet) => _tagAggregator.AddFixedTags(tagSet);
        public bool RemoveFixedTag(GameplayTag tag) => _tagAggregator.RemoveFixedTag(tag);
        public void RemoveFixedTags(GameplayTagSet tagSet) => _tagAggregator.RemoveFixedTags(tagSet);
        public void AddDynamicTag(GameplayTag tag) => _tagAggregator.AddDynamicTag(tag);
        public void AddDynamicTags(GameplayTagSet tagSet) => _tagAggregator.AddDynamicTags(tagSet);
        public void RemoveDynamicTag(GameplayTag tag) => _tagAggregator.RemoveDynamicTag(tag);
        public void RemoveDynamicTags(GameplayTagSet tagSet) => _tagAggregator.RemoveDynamicTags(tagSet);
        public bool CheckAnyTags(GameplayTagSet tagSet) => _tagAggregator.CheckAnyTags(tagSet);
        public bool CheckAllTags(GameplayTagSet tagSet) => _tagAggregator.CheckAllTags(tagSet);

        // 按完整标签名的重载（运行时/编辑器通用，内部转成 GameplayTag）
        public void AddDynamicTag(string tagName) => _tagAggregator.AddDynamicTag(tagName);
        public void RemoveDynamicTag(string tagName) => _tagAggregator.RemoveDynamicTag(tagName);
        public bool CheckTag(string tagName) => _tagAggregator.CheckTag(tagName);

        public bool CheckCanRelease(ITagCondition tagCondition)
        {
            return _tagAggregator.CheckAllTags(tagCondition.RequiredTags) &&
                   !_tagAggregator.CheckAnyTags(tagCondition.BlockedTags);
        }

        #endregion

        #region 效果相关

        public bool TryGetEffect(GameplayTag effectTag, out GameplayDurationalEffect effect) =>
            _effectDic.TryGetValue(effectTag, out effect);

        public void AddEffect(GameplayDurationalEffect effect)
        {
            AddDynamicTags(effect.GrantedTags);
            RemoveDynamicTags(effect.RemovedTags);

            _effectDic.TryAdd(effect.EffectTag, effect);
            // 告知事件中心，效果被添加
            _gasEventCenter.Publish(GASEventCenterKeys.AddEffectEvent, effect);
        }

        public void RemoveEffect(GameplayDurationalEffect effect)
        {
            AddDynamicTags(effect.RemovedTags);
            RemoveDynamicTags(effect.GrantedTags);

            _effectDic.Remove(effect.EffectTag);

            // 告知事件中心，效果被移除
            _gasEventCenter.Publish(
                GASEventCenterKeys.RemoveEffectEvent,
                effect.EffectTag);
        }

        public void EffectStackChanged(GameplayDurationalEffect effect, int curStack)
        {
            _gasEventCenter.Publish(
                GASEventCenterKeys.EffectStackChangedEvent,
                effect,
                curStack);
        }

        /// <summary>效果应用（提供给外部）</summary>
        public void ApplyEffect(GameplayEffect effect, AbilitySystemComponent source, int stack = 1)
        {
            if (effect == null || !CheckCanRelease(effect)) return;

            switch (effect)
            {
                // 持续效果：转调它自己的 Execute —— 叠层/新建实例/进入都由效果自己处理
                case GameplayDurationalEffect durationalEffect:
                    durationalEffect.Execute(source, this, stack);
                    break;

                // 瞬时效果（含纯表现 Cue 效果）：转调它自己的 Execute（拷贝/绑定/表现都由效果自己处理）
                case GameplayInstantEffect instantEffect:
                    instantEffect.Execute(source, this);
                    break;
            }
        }
        #endregion
    }
}
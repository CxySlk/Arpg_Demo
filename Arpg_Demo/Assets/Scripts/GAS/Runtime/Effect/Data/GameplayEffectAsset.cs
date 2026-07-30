using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GameplayEffect 的 ScriptableObject 资产定义。
    /// 在 Unity Editor 中配置效果的所有数据：持续方式、时间、标签条件、属性修改器、表现层 Cue。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(GameplayEffectAsset), menuName = "GAS/" + nameof(GameplayEffectAsset))]
    public class GameplayEffectAsset : ConfigBase
    {
        /// <summary>效果标识符（来自 ConfigBase 的 EntityId）</summary>
        public EntityId AssetEntityID => GetEntityId();

        #region 效果持续方式

        [SerializeField, LabelText("效果持续方式")]
        private EffectDurationType _durationType;
        public EffectDurationType DurationType => _durationType;

        [SerializeField, LabelText("效果叠层方式"), ShowIf("@_durationType != EffectDurationType.Instance")]
        private GameplayEffectStackData _stackData;
        public GameplayEffectStackData StackData => _stackData;

        #endregion

        #region 时间相关

        [Header("时间相关")]
        [SerializeField, LabelText("持续时间(秒)"), ShowIf("@_durationType != EffectDurationType.Instance")]
        private float _durationSeconds;
        public float DurationSeconds => _durationSeconds;

        [SerializeField, LabelText("周期(秒)"), ShowIf("@_durationType != EffectDurationType.Instance")]
        private float _periodicSeconds;
        public float PeriodicSeconds => _periodicSeconds;

        #endregion

        #region 标签相关

        [Header("标签相关")]
        [SerializeField, LabelText("需要的标签")]
        private GameplayTagSet _requiredTags;
        public GameplayTagSet RequiredTags => _requiredTags;

        [SerializeField, LabelText("阻塞的标签")]
        private GameplayTagSet _blockedTags;
        public GameplayTagSet BlockedTags => _blockedTags;

        [SerializeField, LabelText("授予的标签")]
        private GameplayTagSet _grantTags;
        public GameplayTagSet GrantTags => _grantTags;

        [SerializeField, LabelText("移除的标签")]
        private GameplayTagSet _removedTags;
        public GameplayTagSet RemovedTags => _removedTags;

        #endregion

        #region 属性修饰器

        [Header("属性修饰器")]
        [SerializeField, LabelText("属性修饰器")]
        private GameplayEffectAttrModifier[] _attrModifier;
        public GameplayEffectAttrModifier[] AttrModifiers => _attrModifier;

        #endregion

        #region Cue表现层

        [SerializeField, LabelText("执行时Cue")]
        private GameplayCueInstant[] _onExcuteCues;
        public GameplayCueInstant[] OnExcuteCues => _onExcuteCues;

        [SerializeField, LabelText("添加时Cue")]
        private GameplayCue[] _onAddCues;
        public GameplayCue[] OnAddCues => _onAddCues;

        #endregion
    }
}

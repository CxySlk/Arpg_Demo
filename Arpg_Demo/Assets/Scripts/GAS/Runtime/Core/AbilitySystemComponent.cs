using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GAS 核心组件，挂载在角色 GameObject 上。
    /// 管理属性的初始化、标签系统接入、以及每帧 Tick 驱动。
    /// 具体的 Effect 应用/移除 和 属性修改 分别在 partial 文件中实现。
    /// </summary>
    public partial class AbilitySystemComponent : SerializedMonoBehaviour, IEffectTarget
    {
        [SerializeField] private AbilitySystemComponentPreset _preset;
        [ShowInInspector, ReadOnly] private AttributeSet _attrDic;
        [ShowInInspector, ReadOnly] private GameplayTagAggregator _tagAggregator;

        /// <summary>当前游戏帧计数（由 GameplayAbilitySystem 驱动）</summary>
        public int CurrentFrame { get; private set; }

        #region 生命周期

        private void Awake()
        {
            if (_preset != null)
                InitForPreset();
        }

        private void OnEnable() => GASHost.Instance.System.RegisterActivateASC(this);
        private void OnDisable() => GASHost.Instance.System.UnRegisterActivateASC(this);

        private void OnDestroy()
        {
            _attrDic?.Destroy();
            _tagAggregator?.Destroy();
        }

        /// <summary>
        /// 根据 Preset 初始化属性集和标签聚合器。
        /// 必须在 _preset 设置后调用。
        /// </summary>
        public void InitForPreset()
        {
            _attrDic = AttributeSet.Create();
            foreach (var attr in _preset.Attributes)
                _attrDic.AttrDic.Add(attr.HashCode, attr);

            _tagAggregator = GameplayTagAggregator.Create();
            _tagAggregator.Init(_preset.TagSet, TagIsDirty);
        }

        /// <summary>
        /// 驱动效果生命周期（由 GameplayAbilitySystem 每帧调用）。
        /// </summary>
        /// <param name="curFrame">当前游戏帧</param>
        public void Tick(int curFrame)
        {
            CurrentFrame = curFrame;
            TickEffects(curFrame);
        }

        #endregion

        #region 标签相关 (转发到 GameplayTagAggregator)

        public void AddFixedTags(GameplayTagSet tagSet) => _tagAggregator.AddFixedTags(tagSet);
        public void RemoveFixedTags(GameplayTagSet tagSet) => _tagAggregator.RemoveFixedTags(tagSet);
        public void AddDynamicTags(GameplayTagSet tagSet) => _tagAggregator.AddDynamicTags(tagSet);
        public void RemoveDynamicTags(GameplayTagSet tagSet) => _tagAggregator.RemoveDynamicTags(tagSet);
        public bool CheckAnyTags(GameplayTagSet tagSet) => _tagAggregator.CheckAnyTags(tagSet);
        public bool CheckAllTags(GameplayTagSet tagSet) => _tagAggregator.CheckAllTags(tagSet);

        #endregion
    }
}

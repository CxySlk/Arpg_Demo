using Sirenix.OdinInspector;

namespace GAS
{
    /// <summary>效果叠层的聚合方式</summary>
    public enum EffectStackAggregateType
    {
        /// <summary>仅叠层，不刷新持续时间</summary>
        [LabelText("仅叠层")] OnlyStack,
        /// <summary>叠层并刷新持续时间</summary>
        [LabelText("叠层并刷新")] StackAndRefresh
    }

    /// <summary>效果到期时的降层方式</summary>
    public enum EffectStackDecreaseType
    {
        /// <summary>到期直接清空移除</summary>
        [LabelText("清空")] Clear,
        /// <summary>到期降低一层，为零时移除</summary>
        [LabelText("降层")] Refresh
    }

    /// <summary>
    /// 效果叠层配置数据。控制最大叠层数、叠层聚合方式、到期降层方式。
    /// 支持对象池回收。
    /// </summary>
    [System.Serializable]
    public class GameplayEffectStackData
    {
        #region 叠层设置

        [LabelText("叠层聚合方式")]
        public EffectStackAggregateType AggregateType;

        [LabelText("叠层降低方式")]
        public EffectStackDecreaseType DecreaseType;

        [LabelText("最大叠层数")]
        public int MaxStackCount = 1;

        #endregion
    }
}

using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

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
    public class GameplayEffectStackData
    {
        #region 叠层属性

        [LabelText("叠层聚合方式"), SerializeField]
        private EffectStackAggregateType _aggregateType;
        public EffectStackAggregateType AggregateType => _aggregateType;

        [LabelText("叠层降低方式"), SerializeField]
        private EffectStackDecreaseType _decreaseType;
        public EffectStackDecreaseType DecreaseType => _decreaseType;

        [LabelText("最大叠层数"), SerializeField]
        private int _maxStackCount = 1;
        public int MaxStackCount => _maxStackCount;

        #endregion

        /// <summary>从对象池获取或新建实例</summary>
        public static GameplayEffectStackData Create() => PoolSystem.GetOrNewObject<GameplayEffectStackData>();

        /// <summary>初始化叠层配置</summary>
        public void Init(EffectStackAggregateType aggregateType, EffectStackDecreaseType decreaseType, int maxStackCount)
        {
            _aggregateType = aggregateType;
            _decreaseType = decreaseType;
            _maxStackCount = maxStackCount;
        }

        /// <summary>回收到对象池</summary>
        public void Destroy() => this.PushObject();
    }
}

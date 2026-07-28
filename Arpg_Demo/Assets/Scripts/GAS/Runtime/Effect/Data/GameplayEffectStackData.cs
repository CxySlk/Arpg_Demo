using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>效果叠加方式</summary>
    public enum EffectStackAggregateType
    {
        [LabelText("只叠层")] OnlyStack,
        [LabelText("叠层且刷新持续时间")] StackAndRefresh,
    }
    /// <summary>效果降层方式</summary>
    public enum EffectStackDecreaseType
    {
        [LabelText("直接清空")] Clear,
        [LabelText("层数减少")] Refresh,
    }
    public class GameplayEffectStackData
    {
        [SerializeField, LabelText("层数增加方式")]
        private EffectStackAggregateType _aggregateType;
        public EffectStackAggregateType AggregateType => _aggregateType;
        [SerializeField, LabelText("层数减少方式")]
        private EffectStackDecreaseType _decreaseType;
        public EffectStackDecreaseType DecreaseType => _decreaseType;
        [SerializeField, LabelText("最大层数")]
        private int _maxStackCount = 1;  // 默认为1
        public int MaxStackCount => _maxStackCount; 
        #region 生命周期相关
        public static GameplayEffectStackData Create() => PoolSystem.GetOrNewObject<GameplayEffectStackData>();
        public void Init(EffectStackAggregateType aggregateType, EffectStackDecreaseType decreaseType, int maxStackCount)
        {
            _aggregateType = aggregateType;
            _decreaseType = decreaseType;
            _maxStackCount = maxStackCount;
        }
        public void Destroy() => this.PushObject();
        #endregion
    }
}

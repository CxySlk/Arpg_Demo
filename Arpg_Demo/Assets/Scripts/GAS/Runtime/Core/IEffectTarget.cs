namespace GAS
{
    /// <summary>
    /// 可作为 GameplayEffect 的目标接收效果。
    /// AbilitySystemComponent 实现了该接口。
    /// </summary>
    public interface IEffectTarget
    {
        /// <summary>应用效果到该目标</summary>
        /// <param name="effectAsset">效果资产</param>
        /// <param name="source">效果来源（当前仅预留）</param>
        void ApplyEffect(GameplayEffectAsset effectAsset, AbilitySystemComponent source);

        /// <summary>从该目标移除效果</summary>
        /// <param name="effectAsset">效果资产</param>
        /// <param name="source">效果来源</param>
        void DisApplyEffect(GameplayEffectAsset effectAsset, AbilitySystemComponent source);
    }
}

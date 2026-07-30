namespace GAS
{
    /// <summary>
    /// 属性修改的 Application 和 Undo。由 AbilitySystemComponent 持有。
    /// </summary>
    public partial class AbilitySystemComponent
    {
        /// <summary>
        /// 批量应用属性修改器到所有者属性上。
        /// </summary>
        /// <param name="attrModifiers">属性修改器数组</param>
        /// <param name="instantModify">true=修改 BaseValue（瞬时效果），false=修改 CurValue（持续效果）</param>
        public void ApplyAttrModifiers(GameplayEffectAttrModifier[] attrModifiers, bool instantModify)
        {
            for (int i = 0; i < attrModifiers.Length; i++)
            {
                int hash = attrModifiers[i].AttrHashCode;
                if (!_attrDic.HasAttr(hash)) continue;
                attrModifiers[i].ApplyModify(_attrDic.AttrDic[hash], instantModify);
            }
        }

        /// <summary>
        /// 根据效果的 DurationType 决定应用方式：
        /// Instance → 修改 BaseValue；其他 → 修改 CurValue。
        /// </summary>
        public void ApplyAttrModifiers(GameplayEffect effect)
        {
            if (effect.AttrModifiers != null)
                ApplyAttrModifiers(effect.AttrModifiers, effect.DurationType == EffectDurationType.Instance);
        }

        /// <summary>
        /// 撤销单个属性修改器的效果（效果到期或移除时调用）。
        /// RealModifyValue 记录了累计修改量，撤销后归零。
        /// </summary>
        public void UndoAttributeModifiers(GameplayEffectAttrModifier modify)
        {
            if (!_attrDic.AttrDic.TryGetValue(modify.AttrHashCode, out var attr)) return;

            var delta = -modify.RealModifyValue;
            switch (modify.ModifierTarget)
            {
                case AttributeModifyTarget.CurrentValue:
                    attr.AddCurrentValue(delta, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MinValue:
                    attr.AddMinValue(delta, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MaxValue:
                    attr.AddMaxValue(delta, AttributeDataType.CurrentValue);
                    break;
            }
            modify.RealModifyValue = 0;
        }
    }
}

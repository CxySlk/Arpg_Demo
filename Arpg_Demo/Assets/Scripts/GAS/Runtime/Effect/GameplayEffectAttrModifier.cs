using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>属性修改方式</summary>
    [LabelText("属性修改方式")]
    public enum CalculateType
    {
        /// <summary>加法</summary>
        [LabelText("增加")] Add,
        /// <summary>乘法：不是直接乘以该值，而是加上乘后的值【比如BaseValue + MinValue * 0.3】</summary>
        [LabelText("乘法")] Multiple,
    }

    /// <summary>属性修改来源</summary>
    public enum CalculateSource
    {
        [LabelText("固定值")] FixedValue,
        [LabelText("当前值")] CurrentValue,
        [LabelText("最大值")] MaxValue
    }
    /// <summary>属性修改目标</summary>
    public enum AttributeModifyTarget
    {
        [LabelText("当前值")] CurrentValue,
        [LabelText("最小值")] MinValue,
        [LabelText("最大值")] MaxValue
    }

    /// <summary>效果的属性修饰器，用于效果激活时的及效果过期时的属性恢复</summary>
    public class GameplayEffectAttrModifier
    {
        #region 标识符
        [Header("属性标识符")]
        [LabelText("要修改的属性名"), SerializeField, ValueDropdown("@GASEditorData.Instance.Attributes"), OnValueChanged(nameof(OnAttributeNameChanged))]
        private string _attrName;
        public string AttrName => _attrName;
        [SerializeField, ReadOnly, LabelText("哈希值")]
        private int _attrHashCode;
        public int AttrHashCode => _attrHashCode;
        #endregion
        #region 修改设置
        [Header("修改设置")]
        [LabelText("修改方式"), SerializeField]
        private CalculateType _modifyType;
        public CalculateType ModifyType => _modifyType;
        [LabelText("修改来源"), SerializeField]
        private CalculateSource _modifierSource;
        public CalculateSource ModifierSource => _modifierSource;
        [LabelText("修改目标"), SerializeField]
        private AttributeModifyTarget _modifierTarget;
        public AttributeModifyTarget ModifierTarget => _modifierTarget;
        [LabelText("修改值"), SerializeField]
        private float _modifierValue;
        public float ModifierValue => _modifierValue;
        #endregion
        #region 效果过期
        [Header("效果过期相关")]
        [LabelText("效果过期后是否自动恢复"), SerializeField]
        private bool _experiedAutoRestore;
        public bool ExperiedAutoRestore => _experiedAutoRestore;
        /// <summary>真实修改的值，配合_experiedAutoRestore在效果结束后恢复该值使用</summary>
        [HideInInspector]
        public float RealModifyValue = 0;
        #endregion
        #region 生命周期
        public static GameplayEffectAttrModifier Create() => PoolSystem.GetOrNewObject<GameplayEffectAttrModifier>();
        public void Init(GameplayEffectAttrModifier modifier)
        {
            _attrName = modifier.AttrName;
            _attrHashCode = modifier.AttrHashCode;
            _modifyType = modifier.ModifyType;
            _modifierSource = modifier.ModifierSource;
            _modifierTarget = modifier.ModifierTarget;
            _modifierValue = modifier.ModifierValue;
            _experiedAutoRestore = modifier.ExperiedAutoRestore;
            RealModifyValue = 0;
        }

        public void Destroy() => this.PushObject();
        #endregion
#if UNITY_EDITOR
        private void OnAttributeNameChanged() => _attrHashCode = _attrName.GetHashCode();
#endif
        private float GetModifyValue(float baseValue)
        {
            switch (_modifyType)
            {
                case CalculateType.Add:
                    return _modifierValue;
                case CalculateType.Multiple:
                    return baseValue * _modifierValue;
                default:
                    return 0;
            }
        }
        public void ApplyModify(Attribute attribute, bool instantModify)
        {
            float modifyValue = 0;
            switch (_modifierSource)
            {
                case CalculateSource.FixedValue:
                    modifyValue = GetModifyValue(_modifierValue);
                    break;
                case CalculateSource.CurrentValue:
                    modifyValue = GetModifyValue(attribute.CurValue.CurValue);
                    break;
                case CalculateSource.MaxValue:
                    modifyValue = GetModifyValue(attribute.MaxValue.CurValue);
                    break;
            }
            if (!instantModify) RealModifyValue += modifyValue;

            switch (ModifierTarget)
            {
                case AttributeModifyTarget.CurrentValue:
                    if (instantModify) attribute.AddCurrentValue(modifyValue, AttributeDataType.BaseValue);
                    else attribute.AddCurrentValue(modifyValue, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MinValue:
                    if (instantModify) attribute.AddMinValue(modifyValue, AttributeDataType.BaseValue);
                    else attribute.AddMinValue(modifyValue, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MaxValue:
                    if (instantModify) attribute.AddMaxValue(modifyValue, AttributeDataType.BaseValue);
                    else attribute.AddMaxValue(modifyValue, AttributeDataType.CurrentValue);
                    break;
            }
        }
        public void UnDoModify(Attribute attribute)
        {
            switch (ModifierTarget)
            {
                case AttributeModifyTarget.CurrentValue:
                    attribute.SetCurrentValue(attribute.CurValue.CurValue - RealModifyValue,AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MinValue:
                    attribute.SetMinValue(attribute.MinValue.CurValue - RealModifyValue, AttributeDataType.CurrentValue);
                    break;
                case AttributeModifyTarget.MaxValue:
                    attribute.SetMaxValue(attribute.MaxValue.CurValue - RealModifyValue, AttributeDataType.CurrentValue);
                    break;
            }
        }
    }
}

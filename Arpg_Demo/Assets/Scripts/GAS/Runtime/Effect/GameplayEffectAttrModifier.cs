using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>属性修改方式（加法/乘法）</summary>
    [LabelText("属性修改方式")]
    public enum CalculateType
    {
        /// <summary>加法：直接加修改值</summary>
        [LabelText("增加")] Add,
        /// <summary>乘法：BaseValue + SourceValue * 修改值</summary>
        [LabelText("乘法")] Multiple,
    }

    /// <summary>属性修改的计算来源</summary>
    public enum CalculateSource
    {
        /// <summary>固定值：直接使用 ModifierValue</summary>
        [LabelText("固定值")] FixedValue,
        /// <summary>目标的当前值</summary>
        [LabelText("当前值")] CurrentValue,
        /// <summary>目标的最大值</summary>
        [LabelText("最大值")] MaxValue
    }

    /// <summary>属性修改的目标（修改哪个值）</summary>
    public enum AttributeModifyTarget
    {
        [LabelText("当前值")] CurrentValue,
        [LabelText("最小值")] MinValue,
        [LabelText("最大值")] MaxValue
    }

    /// <summary>
    /// 效果的属性修饰器。定义如何修改属性（方式/来源/目标/值），
    /// 并在效果激活时应用修改、效果到期时撤销（通过 RealModifyValue 记录累计量）。
    /// </summary>
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

        /// <summary>实际累计修改量（效果到期时用于撤销）</summary>
        [HideInInspector]
        public float RealModifyValue = 0;

        #endregion

        #region 生命周期

        /// <summary>从对象池获取或新建实例</summary>
        public static GameplayEffectAttrModifier Create() => PoolSystem.GetOrNewObject<GameplayEffectAttrModifier>();
        public static GameplayEffectAttrModifier[] CopyArray(GameplayEffectAttrModifier[] modifyArray)
        {
            if (modifyArray == null) return null;

            GameplayEffectAttrModifier[] _modifyArray = new GameplayEffectAttrModifier[modifyArray.Length];
            for (int i = 0; i < modifyArray.Length; i++)
            {
                _modifyArray[i] = PoolSystem.GetOrNewObject<GameplayEffectAttrModifier>();
                _modifyArray[i].Init(modifyArray[i]);
            }
            return _modifyArray;
        }

        /// <summary>从另一个 AttrModifier 拷贝配置（克隆用）</summary>
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

        /// <summary>回收到对象池</summary>
        public void Destroy() => this.PushObject();

        #endregion

#if UNITY_EDITOR
        private void OnAttributeNameChanged() => _attrHashCode = _attrName.GetHashCode();
#endif

        /// <summary>根据修改方式和基础值计算修改量</summary>
        private float GetModifyValue(float baseValue)
        {
            switch (_modifyType)
            {
                case CalculateType.Add: return _modifierValue;
                case CalculateType.Multiple: return baseValue * _modifierValue;
                default: return 0;
            }
        }

        /// <summary>
        /// 应用属性修改。
        /// instantModify=true → 修改 BaseValue（瞬时效果）
        /// instantModify=false → 修改 CurValue 并累加 RealModifyValue（持续效果，用于后续撤销）
        /// </summary>
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

        /// <summary>
        /// 撤销属性修改。将 RealModifyValue 的累计值从 CurValue 中减去。
        /// </summary>
        public void UnDoModify(Attribute attribute)
        {
            switch (ModifierTarget)
            {
                case AttributeModifyTarget.CurrentValue:
                    attribute.SetCurrentValue(attribute.CurValue.CurValue - RealModifyValue, AttributeDataType.CurrentValue);
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

using MessagePack;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    #region 内部类
    public enum AttributeDataType
    {
        BaseValue,
        CurrentValue
    }

    [MessagePackObject(AllowPrivate = true)]
    public partial struct AttributeData
    {
        [SerializeField, LabelText("基础值"), OnValueChanged(nameof(OnBaseValueChanged))]
        [Key(0)]
        private float _baseValue;
        [IgnoreMember]
        public float BaseValue
        {
            get => _baseValue;
            set
            {
                if (_baseValue == value) return;
                _baseValue = value;
            }
        }

        [SerializeField, LabelText("当前值")]
        [Key(1)]
        private float _curValue;
        [IgnoreMember]
        public float CurValue
        {
            get => _curValue;
            set
            {
                if (_curValue == value) return;
                _curValue = value;
                onValueChanged?.Invoke(value);
            }
        }

        public event Action<float> onValueChanged;

        public void Init(float baseValue, float curValue)
        {
            _baseValue = baseValue;
            _curValue = curValue;
        }
#if UNITY_EDITOR
        private void OnBaseValueChanged() => CurValue = BaseValue;
#endif
    }
    #endregion


    [MessagePackObject(AllowPrivate = true)]
    public sealed partial class Attribute
    {
        #region 变量相关
        [Key(0)]
        [SerializeField, LabelText("属性名"), ValueDropdown("@GASEditorData.Instance.Attributes"), OnValueChanged(nameof(OnAttrNameChanged))]
        private string _attrName;
        [Key(1)]
        [SerializeField, ReadOnly, LabelText("哈希值")]
        private int _hashCode;
        #endregion
        #region 属性变量
        [Key(2)]
        [SerializeField, LabelText("当前值")]
        private AttributeData _curValue;
        [Key(3)]
        [SerializeField, LabelText("最小值")]
        private AttributeData _minValue = new AttributeData()
        {
            BaseValue = 0,
            CurValue = 0,
        };
        [Key(4)]
        [SerializeField, LabelText("最大值")]
        private AttributeData _maxValue = new AttributeData()
        {
            BaseValue = float.MaxValue,
            CurValue = float.MaxValue
        };
        #endregion
        #region 公开属性
        [IgnoreMember]
        public string AttrName => _attrName;
        [IgnoreMember]
        public int HashCode => _hashCode;
        [IgnoreMember]
        public AttributeData CurValue => _curValue;
        [IgnoreMember]
        public AttributeData MinValue => _minValue;
        [IgnoreMember]
        public AttributeData MaxValue => _maxValue;
        [IgnoreMember]
        public float NormalizeValue => (_curValue.CurValue - _minValue.CurValue) / (_maxValue.CurValue - _minValue.CurValue);
        #endregion
        #region 事件相关
        private event Action<float> _onNormalizeValueChanged;
        public void RegisterCurrentValueChanged(Action<float> action) => _curValue.onValueChanged += action;
        public void UnRegisterCurrentValueChanged(Action<float> action) => _curValue.onValueChanged -= action;
        public void RegisterMinValueChanged(Action<float> action) => _minValue.onValueChanged += action;
        public void UnRegisterMinValueChanged(Action<float> action) => _minValue.onValueChanged -= action;
        public void RegisterMaxValueChanged(Action<float> action) => _maxValue.onValueChanged += action;
        public void UnRegisterMaxValueChanged(Action<float> action) => _maxValue.onValueChanged -= action;
        public void RegisterNormalizeValueChanged(Action<float> action) => _onNormalizeValueChanged += action;
        public void UnRegisterNormalizeValueChanged(Action<float> action) => _onNormalizeValueChanged -= action;
        #endregion
        #region Set相关
        public void SetCurrentValue(float value, AttributeDataType type)
        {
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    if (Mathf.Approximately(value, _curValue.BaseValue)) return;
                    var curDelta = value - _curValue.BaseValue;
                    _curValue.BaseValue = value;
                    _curValue.CurValue += curDelta;
                    break;
                case AttributeDataType.CurrentValue:
                    if (Mathf.Approximately(value, _curValue.CurValue)) return;
                    _curValue.CurValue = value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        public void SetMinValue(float value, AttributeDataType type)
        {
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    if (Mathf.Approximately(value, _minValue.BaseValue)) return;
                    var minDelta = value - _minValue.BaseValue;
                    _minValue.BaseValue = value;
                    _minValue.CurValue += minDelta;
                    break;
                case AttributeDataType.CurrentValue:
                    if (Mathf.Approximately(value, _minValue.CurValue)) return;
                    _minValue.CurValue = value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        public void SetMaxValue(float value, AttributeDataType type)
        {
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    if (Mathf.Approximately(value, _maxValue.BaseValue)) return;
                    var maxDelta = value - _maxValue.BaseValue;
                    _maxValue.BaseValue = value;
                    _maxValue.CurValue += maxDelta;
                    break;
                case AttributeDataType.CurrentValue:
                    if (Mathf.Approximately(value, _maxValue.CurValue)) return;
                    _maxValue.CurValue = value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        public void AddCurrentValue(float value, AttributeDataType type)
        {
            if (Mathf.Approximately(value, 0)) return;
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    _curValue.BaseValue += value;
                    _curValue.CurValue += value;
                    break;
                case AttributeDataType.CurrentValue:
                    _curValue.CurValue += value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        public void AddMinValue(float value, AttributeDataType type)
        {
            if (Mathf.Approximately(value, 0)) return;
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    _minValue.BaseValue += value;
                    _minValue.CurValue += value;
                    break;
                case AttributeDataType.CurrentValue:
                    _minValue.CurValue += value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        public void AddMaxValue(float value, AttributeDataType type)
        {
            if (Mathf.Approximately(value, 0)) return;
            switch (type)
            {
                case AttributeDataType.BaseValue:
                    _maxValue.BaseValue += value;
                    _maxValue.CurValue += value;
                    break;
                case AttributeDataType.CurrentValue:
                    _maxValue.CurValue += value;
                    _onNormalizeValueChanged?.Invoke(NormalizeValue);
                    break;
            }
        }
        #endregion
        #region 重写基本函数
        public override int GetHashCode() => _hashCode;
        public override bool Equals(object obj) => obj is Attribute attr && _hashCode == attr._hashCode;
        public static bool operator ==(Attribute a, Attribute b) => a._hashCode == b._hashCode && a._curValue.CurValue == b._curValue.CurValue;
        public static bool operator !=(Attribute a, Attribute b) => a._hashCode != b._hashCode || a._curValue.CurValue != b._curValue.CurValue;
        #endregion
        #region Editor
#if UNITY_EDITOR
        private void OnAttrNameChanged() => _hashCode = _attrName.GetHashCode();

        private IList<string> AttributeGetter() => GASEditorData.Instance.Attributes;
#endif
        #endregion
    }
}
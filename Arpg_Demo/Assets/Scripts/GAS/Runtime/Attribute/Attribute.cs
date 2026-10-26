using cfg;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    // public enum AttributeType
    // {
    //     [LabelText("生命值")] Hp,
    //     [LabelText("法力值")] Mp,
    //     [LabelText("攻击力")] Atk,
    //     [LabelText("防御力")] Def,
    //     [LabelText("移动速度")] MoveSpeed,
    //     [LabelText("下落速度")] FallSpeed,
    //     [LabelText("跳跃速度")] JumpSpeed,
    //     [LabelText("空中移动速度")] InAirMoveSpeed,
    //     [LabelText("回血速度")] HpRegenSpeed,
    //     [LabelText("回蓝速度")] MpRegenSpeed,
    // }
    
    /// <summary>
    /// 游戏属性定义。包含属性名（通过 HashCode 标识）、当前/最小/最大值，
    /// 以及值变化时的归一化事件回调。支持 Set/Add 操作，区分 BaseValue 和 CurValue 修改目标。
    /// </summary>
    [InlineProperty]
    public class  Attribute
    {
        #region 属性变量

        /// <summary>当前值单元</summary>
        [HorizontalGroup("行")]
        [SerializeField, LabelText("当前值"), LabelWidth(40)]
        private float _curValue;

        public float CurValue
        {
            get => _curValue;
            private set
            {
                value = Mathf.Clamp(value, _minValue, _maxValue);
                if (Mathf.Approximately(_curValue, value)) return;
                _curValue = value;
                _eventCenter?.Publish(GASEventCenterKeys.CurValueChangedKey, _attrType, _curValue);
                _eventCenter?.Publish(GASEventCenterKeys.NormalizeValueChangedKey, _attrType, NormalizeValue);
            }
        }

        /// <summary>最小值单元（默认 Base=0, Cur=0）</summary>
        [HorizontalGroup("行")]
        [SerializeField, LabelText("最小值"), LabelWidth(40)]
        private float _minValue;

        public float MinValue
        {
            get => _minValue;
            private set
            {
                value = Mathf.Min(value, _maxValue);
                if (Mathf.Approximately(_minValue, value)) return;
                _minValue = value;
                _eventCenter?.Publish(GASEventCenterKeys.MinValueChangedKey, _attrType, value);
                if (_curValue < value)
                {
                    _curValue = value;
                    _eventCenter?.Publish(GASEventCenterKeys.CurValueChangedKey, _attrType, value);
                }

                _eventCenter?.Publish(GASEventCenterKeys.NormalizeValueChangedKey, _attrType, NormalizeValue);
            }
        }

        /// <summary>最大值单元（默认 Base=float.MaxValue, Cur=float.MaxValue）</summary>
        [HorizontalGroup("行")]
        [SerializeField, LabelText("最大值"), LabelWidth(40)] [CustomContextMenu("设置最大值", "SetMaxMaxValue")]
        private float _maxValue;

        public float MaxValue
        {
            get => _maxValue;
            private set
            {
                value = Mathf.Max(value, _minValue);
                if (Mathf.Approximately(_maxValue, value)) return;
                _maxValue = value;
                _eventCenter?.Publish(GASEventCenterKeys.MaxValueChangedKey, _attrType, value);
                if (_curValue > value)
                {
                    _curValue = value;
                    _eventCenter?.Publish(GASEventCenterKeys.CurValueChangedKey, _attrType, value);
                }

                _eventCenter?.Publish(GASEventCenterKeys.NormalizeValueChangedKey, _attrType, NormalizeValue);
            }
        }

        /// <summary>归一化值</summary>
        public float NormalizeValue => (_curValue - _minValue) / (_maxValue - _minValue);

        #endregion

        #region 运行时依赖

        /// <summary>本属性所属的类型，随变化事件一起发布给订阅者</summary>
        private EAttributeType _attrType;

        /// <summary>变化事件发布到的事件中心（由 ASC 注入，即角色控制器的事件中心）</summary>
        private EventCenter _eventCenter;

        /// <summary>注入属性类型与事件中心（由 ASC 初始化属性集时调用）</summary>
        public void Init(EAttributeType attrType, EventCenter eventCenter)
        {
            _attrType = attrType;
            _eventCenter = eventCenter;
        }

        #endregion

        public Attribute()
        {
            _minValue = 0;
            _curValue = 100;
            _maxValue = 100;
        }

        public Attribute(Attribute attr)
        {
            _minValue = attr._minValue;
            _curValue = attr._curValue;
            _maxValue = attr._maxValue;
        }

        #region Set/Add 操作

        /// <summary>
        /// 设置当前值的 Base 或 Cur。
        /// 修改 BaseValue 时差值同步到 CurValue，修改 CurValue 时触发归一化事件。
        /// </summary>
        public void SetCurrentValue(float value) => CurValue = value;

        public void SetMinValue(float value) => MinValue = value;
        public void SetMaxValue(float value) => MaxValue = value;

        /// <summary>
        /// 累加当前值。修改 BaseValue 时同步 CurValue，修改 CurValue 时触发归一化事件。
        /// </summary>
        public void AddCurrentValue(float deltaValue) => CurValue += deltaValue;
        public void AddMinValue(float deltaValue) => MinValue += deltaValue;
        public void AddMaxValue(float deltaValue) => MaxValue += deltaValue;

        #endregion

#if UNITY_EDITOR
        private void SetMaxMaxValue() => MaxValue = float.MaxValue;
#endif
    }
}
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 时间轴标记事件基类。定义事件在技能时间轴上触发的起始帧。
    /// </summary>
    public abstract class MarkEventBase
    {
#if UNITY_EDITOR
        [SerializeField, LabelText("事件名")]
        private string _name;
        /// <summary>编辑器用的显示名（未设置时返回默认名）</summary>
        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? GetDefaultName() : _name;
            set => _name = value;
        }

        /// <summary>未设置名字时使用的默认名（子类可覆盖）</summary>
        protected virtual string GetDefaultName() => GetType().Name;
#endif

        [SerializeField, LabelText("起始帧")]
        private int _startFrame;
        public int StartFrame { get => _startFrame; set => _startFrame = Mathf.Max(0, value); }
    }
}

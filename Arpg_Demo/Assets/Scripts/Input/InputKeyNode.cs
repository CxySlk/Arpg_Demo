using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    /// <summary>
    /// 键盘输入节点：检测指定键盘按键的触发状态（按下/抬起/按住由基类 <see cref="InputType"/> 决定）。
    /// </summary>
    /// <remarks>
    /// 通过 Input System 的 <see cref="Keyboard.current"/> 读取按键控件状态。
    /// 注意：当前未对 <c>Keyboard.current</c> 判空，若运行平台无键盘设备会空引用。
    /// </remarks>
    public sealed class InputKeyNode : InputNodeBase
    {
        // 按键类型（避免分组属性在 SerializeReference+预制体修改时触发 Odin 崩溃，只用 LabelText）
        [LabelText("按键类型"), SerializeField] public Key keyCode;


        /// <summary>按下瞬间检测：该帧刚按下指定按键则触发一次。</summary>
        protected override void OnClickDownUpdate()
        {
            if (Keyboard.current[keyCode].wasPressedThisFrame) OnClick();
        }

        /// <summary>抬起瞬间检测：该帧刚松开指定按键则触发一次。</summary>
        protected override void OnClickUpUpdate()
        {
            if (Keyboard.current[keyCode].wasReleasedThisFrame) OnClick();
        }

        /// <summary>持续按住检测：按键处于按下状态则每帧触发。</summary>
        protected override void OnClickedUpdate()
        {
            if (Keyboard.current[keyCode].isPressed) OnClick();
        }
    }
}
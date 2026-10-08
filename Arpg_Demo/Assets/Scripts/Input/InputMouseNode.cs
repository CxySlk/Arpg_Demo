using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Input
{
    /// <summary>
    /// 鼠标输入节点：检测指定鼠标按键（左/右/中键）的触发状态（按下/抬起/按住由基类 <see cref="InputType"/> 决定）。
    /// </summary>
    /// <remarks>
    /// 通过 Input System 的 <see cref="Mouse.current"/> 读取对应按钮控件；
    /// 修改 <see cref="mouseType"/> 后需调用 <see cref="InputNodeBase.Init"/> 或 <see cref="SetMouseType"/> 重新绑定内部控件。
    /// </remarks>
    public class InputMouseNode : InputNodeBase
    {
        // 按键类型（避免分组属性在 SerializeReference+预制体修改时触发 Odin 崩溃，只用 LabelText）
        [LabelText("按键类型"), SerializeField] public InputMouseType mouseType;

        /// <summary>当前绑定的鼠标按钮控件（由 <see cref="SetMouseType"/> 设置）。</summary>
        private ButtonControl _workspace;

        public override void Init()
        {
            SetMouseType(mouseType);
        }

        /// <summary>切换要监听的鼠标按键并更新内部控件引用。</summary>
        /// <param name="mouseType">目标鼠标按键类型。</param>
        public void SetMouseType(InputMouseType mouseType)
        {
            switch (mouseType)
            {
                case InputMouseType.Left:
                    _workspace = Mouse.current.leftButton;
                    break;
                case InputMouseType.Right:
                    _workspace = Mouse.current.rightButton;
                    break;
                case InputMouseType.Middle:
                    _workspace = Mouse.current.middleButton;
                    break;
            }
        }

        /// <summary>按下瞬间检测：该帧刚按下则触发一次。</summary>
        protected override void OnClickDownUpdate()
        {
            if (_workspace.wasPressedThisFrame)
                OnClick();
        }

        /// <summary>抬起瞬间检测：该帧刚松开则触发一次。</summary>
        protected override void OnClickUpUpdate()
        {
            if (_workspace.wasReleasedThisFrame)
                OnClick();
        }

        /// <summary>持续按住检测：按键处于按下状态则每帧触发。</summary>
        protected override void OnClickedUpdate()
        {
            if (_workspace.isPressed)
                OnClick();
        }
    }
}
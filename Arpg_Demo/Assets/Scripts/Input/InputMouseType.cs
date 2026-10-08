using Sirenix.OdinInspector;

namespace Input
{
    /// <summary>
    /// 鼠标按键类型：用于 <see cref="InputMouseNode"/> 指定监听哪个鼠标按键。
    /// </summary>
    public enum InputMouseType
    {
        /// <summary>鼠标左键</summary>
        [LabelText("左键")] Left,

        /// <summary>鼠标右键</summary>
        [LabelText("右键")] Right,

        /// <summary>鼠标中键</summary>
        [LabelText("中键")] Middle,
    }
}

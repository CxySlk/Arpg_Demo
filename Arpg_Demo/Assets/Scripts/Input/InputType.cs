using Sirenix.OdinInspector;

namespace Input
{
    /// <summary>
    /// 输入触发类型：决定输入节点检测哪种按键状态。
    /// </summary>
    public enum InputType
    {
        /// <summary>按下瞬间（该帧刚按下时触发一次）</summary>
        [LabelText("按下")] ClickDown,

        /// <summary>抬起瞬间（该帧刚松开时触发一次）</summary>
        [LabelText("抬起")] ClickUp,

        /// <summary>持续按住（处于按下状态时每帧触发）</summary>
        [LabelText("按住")] Click
    }
}

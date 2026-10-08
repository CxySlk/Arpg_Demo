using UnityEngine;

/// <summary>
/// 输入管理器（静态）：基于 Input System 生成的 <see cref="GameInputAction"/> 资产，提供全局输入查询。
/// </summary>
/// <remarks>
/// 静态构造时创建并启用输入资产，退出时禁用。
/// 提供移动向量、整型方向（XIntInput/YIntInput）与各攻击/跳跃按钮的"本帧刚按下"查询；
/// <see cref="CanInput"/> 为全局输入开关，关闭时所有查询返回默认值（0 / false）。
/// </remarks>
public static class InputManager
{
    /// <summary>静态构造：创建并启用输入资产，注册退出时禁用。</summary>
    static InputManager()
    {
        inputActions = new GameInputAction();
        inputActions.Enable();
        Application.quitting += () => inputActions.Disable();
    }

    /// <summary>底层输入动作资产。</summary>
    private static GameInputAction inputActions;

    /// <summary>GameInput 动作映射（快捷访问）。</summary>
    private static GameInputAction.GameInputActions inputAction => inputActions.GameInput;

    /// <summary>全局输入开关：false 时所有输入查询返回默认值（用于 UI 弹窗/过场禁用操作）。</summary>
    public static bool CanInput { get; set; } = true;

    /// <summary>移动输入向量（X/Y）；输入关闭时返回 Vector2.zero。</summary>
    public static Vector2 MoveInput => CanInput ? inputAction.Move.ReadValue<Vector2>() : Vector2.zero;

    /// <summary>移动输入的 X 方向（-1/0/1，归一化整型）。</summary>
    public static int XIntInput =>
        CanInput ? MoveInput.x == 0 ? 0 : (int)(MoveInput.x / Mathf.Abs(MoveInput.x)) : 0;

    /// <summary>移动输入的 Y 方向（-1/0/1，归一化整型）。</summary>
    public static int YIntInput =>
        CanInput ? MoveInput.y == 0 ? 0 : (int)(MoveInput.y / Mathf.Abs(MoveInput.y)) : 0;

    /// <summary>左攻击：本帧是否刚按下（且输入开关开启）。</summary>
    public static bool LeftAttackInput => CanInput && inputAction.LeftAttack.WasPressedThisFrame();

    /// <summary>右攻击：本帧是否刚按下（且输入开关开启）。</summary>
    public static bool RightAttackInput => CanInput && inputAction.RightAttack.WasPressedThisFrame();

    public static bool AccelerateInput => CanInput && inputAction.Accelerate.IsPressed();
    
    /// <summary>跳跃：本帧是否刚按下（且输入开关开启）。</summary>
    public static bool JumpInput => CanInput && inputAction.Jump.WasPressedThisFrame();
}

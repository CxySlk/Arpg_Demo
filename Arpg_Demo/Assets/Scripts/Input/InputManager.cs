using UnityEngine;
using UnityEngine.InputSystem;

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

    // 手柄右摇杆去抖计时：摇杆推住不动时数值会一直超过阈值，需要冷却，避免每帧都在切目标。
    private static float _lastSwitchTime;

    /// <summary>
    /// 锁定/解锁：本帧是否刚按下（鼠标中键 或 手柄右摇杆按下）。
    /// 这里直接读 Mouse/Gamepad，暂未走 GameInputAction 资产（以后要统一可再迁）。
    /// </summary>
    public static bool LockOnInput =>
        CanInput && ((Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
                  || (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame));

    /// <summary>
    /// 切换目标方向：返回 +1 / -1 / 0。
    /// <para>滚轮：下滚 = +1（下一个/右），上滚 = -1（上一个/左）。</para>
    /// <para>手柄：右摇杆 X &gt; 0 = +1，&lt; 0 = -1（带 0.2s 去抖）。</para>
    /// </summary>
    public static int SwitchTargetInput
    {
        get
        {
            if (!CanInput) return 0;

            if (Mouse.current != null)
            {
                // scroll.y 每格是离散值（Windows 约 ±120）：>0 向上滚，<0 向下滚。
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _lastSwitchTime = Time.unscaledTime;
                    return scroll < 0f ? 1 : -1; // 下滚 = 下一个
                }
            }

            if (Gamepad.current != null)
            {
                float x = Gamepad.current.rightStick.ReadValue().x;
                // 摇杆推住会持续 > 阈值，所以用 _lastSwitchTime 做冷却，保证一次推动只切一次。
                if (Mathf.Abs(x) > 0.6f && Time.unscaledTime - _lastSwitchTime > 0.2f)
                {
                    _lastSwitchTime = Time.unscaledTime;
                    return x > 0f ? 1 : -1;
                }
            }

            return 0;
        }
    }
}

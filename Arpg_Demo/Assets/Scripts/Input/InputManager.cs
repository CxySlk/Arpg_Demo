using UnityEngine;

public static class InputManager
{
    static InputManager()
    {
        inputActions = new GameInputAction();
        inputActions.Enable();
        Application.quitting += () => inputActions.Disable();
    }

    private static GameInputAction inputActions;
    private static GameInputAction.GameInputActions inputAction => inputActions.GameInput;
    public static bool CanInput { get; set; } = true;

    public static Vector2 MoveInput => CanInput ? inputAction.Move.ReadValue<Vector2>() : Vector2.zero;
    public static bool LeftAttackInput => CanInput && inputAction.LeftAttack.WasPressedThisFrame();
    public static bool RightAttackInput => CanInput && inputAction.RightAttack.WasPressedThisFrame();
    public static bool JumpInput => CanInput && inputAction.Jump.WasPressedThisFrame();
}

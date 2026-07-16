using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    [ShowInInspector, ReadOnly] public Vector2 moveInput => InputManager.MoveInput;
    [ShowInInspector, ReadOnly] public bool jump => InputManager.JumpInput;
    [ShowInInspector, ReadOnly] public bool left => InputManager.LeftAttackInput;
    [ShowInInspector, ReadOnly] public bool right => InputManager.RightAttackInput;
    [ShowInInspector, ReadOnly] public bool canInput => InputManager.CanInput;
    [ShowInInspector, ReadOnly] public bool isGround => GetComponent<CharacterController>().isGrounded;
}
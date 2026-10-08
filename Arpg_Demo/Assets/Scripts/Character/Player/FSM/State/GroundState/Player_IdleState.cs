using ParadoxNotion.Design;
using UnityEngine;

[Category("Player/GroundState")]
public class Player_IdleState : Player_GroundState
{
    protected override void OnEnter()
    {
        playerController.Play("Idle");
    }
}

using ParadoxNotion.Design;
using UnityEngine;

[Category("Player/GroundState")]
public class Player_GroundState : PlayerStateBase
{
    protected override void OnUpdate()
    {
        // 模拟重力
        playerController.Move(Vector3.up * (-9.8f * Time.deltaTime));
    }
}
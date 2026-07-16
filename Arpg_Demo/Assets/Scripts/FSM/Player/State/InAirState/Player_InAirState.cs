using ParadoxNotion.Design;
using System.Collections;
using UnityEngine;

[Category("Player")]
public class Player_InAirState : PlayerStateBase
{
    [Name("下落动画名"), RequiredField]
    public string jumpLoop;
    [Name("落地动画名"), RequiredField]
    public string jumpEnd;
    private float jumpEndTime;

    private bool isChanged;

    private Coroutine coroutine;

    protected override void OnInit()
    {
        base.OnInit();
        jumpEndTime = PlayerConfig.StandardAnimationDic[jumpEnd].length;
    }

    protected override void OnEnter()
    {
        PlayerController.PlaySingleAnimation(jumpLoop, null);
        isChanged = false;
    }

    protected override void OnUpdate()
    {
        if (IsGrounded && !isChanged)
        {
            // 播放落地动画
            PlayerController.PlaySingleAnimation(jumpEnd, null);
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = StartCoroutine(InAirStateFinished(jumpEndTime));
            isChanged = true;
            return;
        }
        // 输入不为0
        if (!isChanged &&  MoveInput != Vector2.zero)
        {
            PlayerController.Move(MoveInput, PlayerConfig.InAirMoveSpeed);
            PlayerController.Rotate(MoveInput);
        }
        CharacterController.Move(Vector3.down * -Physics.gravity.y * Time.deltaTime);
    }

    private IEnumerator InAirStateFinished(float time)
    {
        yield return new WaitForSeconds(time);
        Finish();
    }
}

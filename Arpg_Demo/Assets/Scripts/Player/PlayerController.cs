using LLKFrame;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [LabelText("动画控制器"), SerializeField] private AnimationController animationController;
    [LabelText("角色配置"), SerializeField] private PlayerConfig playerConfig;
    [LabelText("角色控制器"), SerializeField] private CharacterController characterController;
    public PlayerConfig PlayerConfig => playerConfig;

    private void Awake()
    {
        animationController.Init();
    }

    public void PlaySingleAnimation(AnimationClip clip, Action<Vector3, Quaternion> onRootMotion, float playSpeed = 1, float transitionTime = .25f)
    {
        animationController.PlaySingleAnimation(clip, playSpeed, transitionTime);
        animationController.SetRootMotion(onRootMotion);
    }

    public void PlaySingleAnimation(string animationName, Action<Vector3, Quaternion> onRootMotion, float playSpeed = 1, float transitionTime = .25f)
    {
        if (playerConfig.StandardAnimationDic.TryGetValue(animationName, out AnimationClip clip))
            PlaySingleAnimation(clip, onRootMotion, playSpeed, transitionTime);
#if UNITY_EDITOR
        else LogSystem.LogError($"PlayerConfig里面没有名为{LogSystem.ErrorColor(animationName)}的动画");
#endif
    }

    public void PlayBlendAnimation(AnimationClip[] clips, Action<Vector3, Quaternion> onRootMotion, float playSpeed = 1, float transitionTime = .25f)
    {
        animationController.PlayBlendAnimation(clips, playSpeed, transitionTime);
        animationController.SetRootMotion(onRootMotion);
    }

    public void Rotate(Vector2 moveInput)
    {
        Vector3 input = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 target = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * input;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), PlayerConfig.RotateSpeed * Time.deltaTime);
    }
    public void Move(Vector2 moveInput, float moveSpeed)
    {
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        moveDir = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * moveDir;
        characterController.Move(moveDir * moveSpeed * Time.deltaTime);
    }
}

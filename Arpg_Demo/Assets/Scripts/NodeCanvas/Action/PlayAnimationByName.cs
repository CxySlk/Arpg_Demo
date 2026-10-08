using System.Collections;
using System.Collections.Generic;
using Animancer;
using Character;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using Sirenix.OdinInspector;
using UnityEngine;

[Name("播放动画（字典）")]
public class PlayAnimationByName : ActionTask
{
    [Name("角色控制器"), Required] public BBParameter<CharacterControllerBase> characterController;

    [Name("动画片段名"), Required] public BBParameter<string> clipName;
    [Name("播放速度"), Required] public float playSpeed = 1;
    [Name("开启根运动")] public bool onRootMotion;
    [Name("等待播放完毕")] public bool waitForComplete;

    [Name("启用音效")] public bool enableAudio;
    [Name("音效表"), ParadoxNotion.Design.ShowIf("enableAudio", 1)] public List<AnimationAudioEntry> audioEntries = new();

    private Coroutine _coroutine;

    protected override void OnExecute()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlayAnimationCoroutine(clipName.value));
    }

    private IEnumerator PlayAnimationCoroutine(string clipName)
    {
        CharacterControllerBase controller = characterController.value;
        AnimancerState state = controller.Play(clipName, playSpeed);
        if (enableAudio)
            AnimationAudioHelper.Bind(state, audioEntries, controller.transform);
        controller.SetRootMotion(onRootMotion ? OnRootMotion : null);
        if (waitForComplete) yield return state;
        _coroutine = null;
        EndAction();
    }

    private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        if (characterController.value == null) return;

        characterController.value.Move(deltaPos);
        characterController.value.Rotate(deltaRot);
    }
}
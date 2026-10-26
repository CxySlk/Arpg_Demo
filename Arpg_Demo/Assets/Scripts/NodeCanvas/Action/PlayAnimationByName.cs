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
    [Name("动画片段名"), Required] public BBParameter<string> clipName;
    [Name("播放速度"), Required] public float playSpeed = 1;
    [Name("开启根运动")] public bool onRootMotion;
    [Name("等待播放完毕")] public bool waitForComplete;

    [Name("启用音效")] public bool enableAudio;
    [Name("音效表"), ParadoxNotion.Design.ShowIf("enableAudio", 1)]
    public List<AnimationAudioEntry> audioEntries = new();

    private AnimancerState _state;
    private CharacterControllerBase _controller;

    protected override string OnInit()
    {
        _controller = agent.GetComponent<CharacterControllerBase>();
        return null;
    }

    protected override void OnExecute()
    {
        Play();
        if (!waitForComplete) EndAction();
    }

    protected override void OnUpdate()
    {
        if (!_state.IsPlaying)
            Play();
        if(!_state.IsPlayingAndNotEnding())
            EndAction();
    }

    private void Play()
    {
        _state = _controller.Play(clipName.value);
        if (enableAudio)
            AnimationAudioHelper.Bind(_state, audioEntries, _controller.transform);

        if (onRootMotion) _controller.SetRootMotion(OnRootMotion);
    }

    private void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
    {
        _controller.Move(deltaPos);
        _controller.Rotate(deltaRot);
    }
}
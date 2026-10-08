using System.Collections.Generic;
using Animancer;
using LLKFrame;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

/// <summary>一条"动画命名事件 → 音效"映射。</summary>
[System.Serializable]
public class AnimationAudioEntry
{
    [Name("事件名")] public BBParameter<StringAsset> eventName = new();
    [Name("音量")] public float volume = 1;
    [Name("音效")] public BBParameter<AudioClip> clip = new();
}

/// <summary>
/// 动画命名事件 → 音效 的通用绑定。节点内嵌"事件名→音效"表，播放动画时调用 <see cref="Bind"/>
/// 把表绑到该动画的命名事件上，动画触发同名事件即播放音效。
/// </summary>
public static class AnimationAudioHelper
{
    /// <summary>
    /// 把"事件名→音效"表绑到这段动画的命名事件上(同名事件全部挂;重复调用是替换,不会叠加)。
    /// 用 state 自己的 <see cref="AnimancerState.OwnedEvents"/>，不触发所有权断言。
    /// </summary>
    public static void Bind(AnimancerState state, IReadOnlyList<AnimationAudioEntry> entries,
        Transform playAt)
    {
        if (entries == null || entries.Count == 0) return;
        BindRecursive(state, entries, playAt);
    }

    private static void BindRecursive(AnimancerState state, IReadOnlyList<AnimationAudioEntry> entries,
        Transform playAt)
    {
        AnimancerEvent.Sequence events = state.OwnedEvents;
        foreach (AnimationAudioEntry entry in entries)
            events.SetCallbacks(entry.eventName.value,
                () => AudioSystem.PlayEffectMusic(entry.clip.value, playAt.position, entry.volume));

        for (int i = 0; i < state.ChildCount; i++)
            BindRecursive(state.GetChild(i), entries, playAt);
    }
}
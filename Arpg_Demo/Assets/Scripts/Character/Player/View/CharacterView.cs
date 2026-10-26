using System;
using System.Collections.Generic;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterView : SerializedMonoBehaviour
{
    [LabelText("动画组件"), SerializeField] private AnimancerComponent _animancer;

    [LabelText("Animator"), SerializeField]
    private Animator _animator;

    public AnimancerComponent Animancer => _animancer;

    private Animator Animator => _animator;

    // 标准动画字典：角色配置里预定义的常用动画(Idle/Move/Death 等)，按名字索引
    [LabelText("标准动画合集"), SerializeField] private Dictionary<string, TransitionAsset> _animationTransitionDic;

    /// <summary>按名字取标准动画过渡(TransitionAsset)。名字不存在时直接抛异常，便于定位配置缺失。</summary>
    public TransitionAsset GetTransition(string animationName) => _animationTransitionDic[animationName];

    /// <summary>按名字取动画片段长度(秒)。</summary>
    public float GetClipLength(string animationName)
    {
        TransitionAsset transition = GetTransition(animationName);
        return transition.MaximumLength;
    }

    #region 动画播放

    /// <summary>播放标准动画字典里的动画(按名字查找)。用 TransitionAsset 自身的淡入/事件/起点，只覆盖播放速度。</summary>
    public AnimancerState Play(string animationName, float fadeDuration = .25f,
        FadeMode fadeMode = FadeMode.FixedDuration)
    {
        TransitionAsset transition = GetTransition(animationName);
        AnimancerState state = _animancer.Play(transition, fadeDuration, fadeMode);
        return state;
    }

    #region Animancer 播放接口(与 AnimancerComponent 同名同参数,直接转调)

    /// <summary>播放动画片段(Animancer 标准播放)。</summary>
    public AnimancerState Play(AnimationClip clip)
    {
        if (!clip) return null;
        return Record(_animancer.Play(clip));
    }

    /// <summary>播放已有 state(Animancer 标准播放)。</summary>
    public AnimancerState Play(AnimancerState state) => Record(_animancer.Play(state));

    /// <summary>交叉淡化播放动画片段(Animancer 标准播放)。</summary>
    public AnimancerState Play(AnimationClip clip, float fadeDuration, FadeMode mode = default)
    {
        if (!clip) return null;
        return Record(_animancer.Play(clip, fadeDuration, mode));
    }

    /// <summary>交叉淡化播放已有 state(Animancer 标准播放)。</summary>
    public AnimancerState Play(AnimancerState state, float fadeDuration, FadeMode mode = default)
        => Record(_animancer.Play(state, fadeDuration, mode));

    /// <summary>播放过渡(Animancer 标准播放)。</summary>
    public AnimancerState Play(ITransition transition) => Record(_animancer.Play(transition));

    /// <summary>交叉淡化播放过渡(Animancer 标准播放)。</summary>
    public AnimancerState Play(ITransition transition, float fadeDuration, FadeMode mode = default)
        => Record(_animancer.Play(transition, fadeDuration, mode));

    /// <summary>按 key 播放已注册的动画(找不到返回 null)。</summary>
    public AnimancerState TryPlay(object key) => Record(_animancer.TryPlay(key));

    /// <summary>按 key 播放已注册的动画(找不到返回 null)。</summary>
    public AnimancerState TryPlay(IHasKey hasKey) => Record(_animancer.TryPlay(hasKey));

    /// <summary>按 key 交叉淡化播放已注册的动画(找不到返回 null)。</summary>
    public AnimancerState TryPlay(object key, float fadeDuration, FadeMode mode = default)
        => Record(_animancer.TryPlay(key, fadeDuration, mode));

    /// <summary>按 key 交叉淡化播放已注册的动画(找不到返回 null)。</summary>
    public AnimancerState TryPlay(IHasKey hasKey, float fadeDuration, FadeMode mode = default)
        => Record(_animancer.TryPlay(hasKey, fadeDuration, mode));

    /// <summary>停止并回绕指定动画片段。</summary>
    public AnimancerState Stop(AnimationClip clip) => _animancer.Stop(clip);

    /// <summary>停止并回绕指定 key 的动画。</summary>
    public AnimancerState Stop(IHasKey hasKey) => _animancer.Stop(hasKey);

    /// <summary>停止并回绕指定 key 的动画。</summary>
    public AnimancerState Stop(object key) => _animancer.Stop(key);

    /// <summary>停止并回绕所有动画。</summary>
    public void Stop() => _animancer.Stop();

    /// <summary>指定动画片段是否正在播放。</summary>
    public bool IsPlaying(AnimationClip clip) => _animancer.IsPlaying(clip);

    /// <summary>指定 key 的动画是否正在播放。</summary>
    public bool IsPlaying(IHasKey hasKey) => _animancer.IsPlaying(hasKey);

    /// <summary>指定 key 的动画是否正在播放。</summary>
    public bool IsPlaying(object key) => _animancer.IsPlaying(key);

    /// <summary>是否有任意动画正在播放。</summary>
    public bool IsPlaying() => _animancer.IsPlaying();

    /// <summary>指定动画片段是否正被任一 state 播放。</summary>
    public bool IsPlayingClip(AnimationClip clip) => _animancer.IsPlayingClip(clip);

    /// <summary>立即求值当前所有动画到目标对象。</summary>
    public void Evaluate() => _animancer.Evaluate();

    /// <summary>推进指定时间并立即求值所有动画。</summary>
    public void Evaluate(float deltaTime) => _animancer.Evaluate(deltaTime);

    /// <summary>记录当前播放的 state(直接播放 clip/state 时动画名为 null)。</summary>
    private AnimancerState Record(AnimancerState state)
    {
        if (state == null) return null;

        return state;
    }

    #endregion

    #endregion

    #region RootMotion

    /// <summary>动画根运动回调(Animator 同物体;赋值后由 OnAnimatorMove 转发 deltaPosition/deltaRotation)。</summary>
    public Action<Vector3, Quaternion> RootMotion { get; set; }

    /// <summary>设置/清空根运动回调。</summary>
    public void SetRootMotion(Action<Vector3, Quaternion> onRootMotion) => RootMotion = onRootMotion;

    private void OnAnimatorMove()
    {
        RootMotion?.Invoke(Animator.deltaPosition, Animator.deltaRotation);
    }

    #endregion
}
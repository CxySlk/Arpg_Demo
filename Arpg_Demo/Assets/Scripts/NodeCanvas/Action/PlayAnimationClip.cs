using System.Collections;
using Animancer;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Action
{
    [Name("播放动画片段")]
    [Category("动画/Action")]
    public class PlayAnimationClip : ActionTask
    {
        [Name("Animancer"), RequiredField] public BBParameter<AnimancerComponent> animancer;
        [Name("动画片段"), RequiredField] public BBParameter<AnimationClip> clip;
        [Name("切换时间"), RequiredField] public float fadeDuration = .25f;
        [Name("切换模式"), RequiredField] public FadeMode fadeMode = FadeMode.FixedDuration;
        [Name("等待播放完毕")] public bool waitForComplete;
        private Coroutine _coroutine;

        protected override void OnExecute()
        {
            if (waitForComplete)
            {
                if (_coroutine != null) StopCoroutine(_coroutine);
                _coroutine = StartCoroutine(PlayAnimationCoroutine());
            }
            else
            {
                animancer.value.Play(clip.value, fadeDuration, fadeMode);
                EndAction();
            }
        }

        private IEnumerator PlayAnimationCoroutine()
        {
            AnimancerState state = animancer.value.Play(clip.value, fadeDuration, fadeMode);
            yield return state;
            EndAction();
        }
    }
}
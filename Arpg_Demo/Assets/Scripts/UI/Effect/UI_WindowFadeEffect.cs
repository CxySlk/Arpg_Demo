using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 窗口透明度表现：用 <see cref="CanvasGroup.alpha"/> 做淡入淡出。
    /// 挂到窗口根物体上即可，无需窗口脚本再写动画代码。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UI_WindowFadeEffect : UI_WindowEffectBase
    {
        [LabelText("弹入缓动"), SerializeField] private Ease _enterEase = Ease.OutQuad;
        [LabelText("弹出缓动"), SerializeField] private Ease _exitEase = Ease.InQuad;

        private CanvasGroup _canvasGroup;

        private void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>弹入：alpha 0→1（起始值同帧直接置 0，不会闪一帧完全可见）</summary>
        public override void PlayEnter()
        {
            _tween?.Kill();
            _canvasGroup.alpha = 0f;
            _tween = _canvasGroup.DOFade(1f, _duration).SetEase(_enterEase).SetUpdate(true);
        }

        /// <summary>弹出：alpha 1→0，播完回调（基类据此隐藏窗口）</summary>
        public override void PlayExit(Action onComplete)
        {
            _tween?.Kill();
            _tween = _canvasGroup.DOFade(0f, _duration).SetEase(_exitEase).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}

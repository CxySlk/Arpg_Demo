using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 窗口缩放表现：在"设计缩放 × 起始系数"与设计缩放之间缩放。
    /// 挂到窗口根物体上即可，无需窗口脚本再写动画代码；与 <see cref="UI_WindowFadeEffect"/> 可任意组合。
    /// </summary>
    public class UI_WindowScaleEffect : UI_WindowEffectBase
    {
        [LabelText("弹入起始缩放系数"), SerializeField] private float _scaleFrom = 0f;
        [LabelText("弹入缓动"), SerializeField] private Ease _enterEase = Ease.OutBack;
        [LabelText("弹出缓动"), SerializeField] private Ease _exitEase = Ease.InBack;

        /// <summary>设计缩放（预制体上的 localScale），弹入的终点 / 弹出的基准</summary>
        private Vector3 _designScale;

        private void Awake() => _designScale = transform.localScale;

        /// <summary>弹入：设计缩放 × 起始系数 → 设计缩放（OutBack 会略微超过再回弹，产生"弹开"感）</summary>
        public override void PlayEnter()
        {
            _tween?.Kill();
            transform.localScale = _designScale * _scaleFrom;
            _tween = transform.DOScale(_designScale, _duration).SetEase(_enterEase).SetUpdate(true);
        }

        /// <summary>弹出：设计缩放 → 设计缩放 × 起始系数，播完回调（基类据此隐藏窗口）</summary>
        public override void PlayExit(Action onComplete)
        {
            _tween?.Kill();
            _tween = transform.DOScale(_designScale * _scaleFrom, _duration).SetEase(_exitEase).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}

using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    /// <summary>位移动画的偏移取法：固定向量 / 面板宽度。</summary>
    public enum UIWindowMoveOffsetMode
    {
        [LabelText("固定偏移")] Fixed,
        [LabelText("面板宽度")] PanelWidth,
    }

    /// <summary>
    /// 窗口位移动画：弹入从"设计位置 + 偏移"位移回设计位置，弹出反向位移。
    /// 挂到窗口根物体上即可，由 <see cref="LLKFrame.UI_WindowBase"/> 自动驱动。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UI_WindowMoveEffect : UI_WindowEffectBase
    {
        [LabelText("偏移模式"), SerializeField]
        private UIWindowMoveOffsetMode _mode = UIWindowMoveOffsetMode.PanelWidth;

        [LabelText("固定偏移"), ShowIf("_mode", UIWindowMoveOffsetMode.Fixed), SerializeField]
        private Vector2 _fixedOffset = new(1200f, 0f);

        [LabelText("宽度系数"), ShowIf("_mode", UIWindowMoveOffsetMode.PanelWidth), SerializeField]
        private float _widthFactor = 1f;

        [LabelText("弹入缓动"), SerializeField] private Ease _enterEase = Ease.OutQuad;
        [LabelText("弹出缓动"), SerializeField] private Ease _exitEase = Ease.InQuad;

        private RectTransform _rect;
        private Vector2 _designPos;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _designPos = _rect.anchoredPosition;
        }

        /// <summary>本次的位移偏移量。</summary>
        private Vector2 Offset => _mode == UIWindowMoveOffsetMode.PanelWidth
            ? new Vector2(_rect.rect.width * _widthFactor, 0f)
            : _fixedOffset;

        /// <summary>弹入：置到"设计位置 + 偏移"，再位移回设计位置。</summary>
        public override void PlayEnter()
        {
            _tween?.Kill();
            _rect.anchoredPosition = _designPos + Offset;
            _tween = _rect.DOAnchorPos(_designPos, _duration).SetEase(_enterEase).SetUpdate(true);
        }

        /// <summary>弹出：位移到"设计位置 + 偏移"，播完回调。</summary>
        public override void PlayExit(Action onComplete)
        {
            _tween?.Kill();
            _tween = _rect.DOAnchorPos(_designPos + Offset, _duration).SetEase(_exitEase).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}

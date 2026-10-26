using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UI_Bar : MonoBehaviour
{
    [SerializeField] private Image _imgBar1;
    [SerializeField] private Image _imgBar2;
    [SerializeField, Range(0, 3)] private float _bar1Duration = .5f;
    [SerializeField, Range(0, 3)] private float _bar2Duration = .5f;
    [SerializeField, LabelText("最小变化阈值")] private float _minProgressDelta = 0.001f;

    private float _barWidth;
    private float _curProgress = 1f;
    private Tween _progressTween;

    public void Init()
    {
        _barWidth = (_imgBar1.transform as RectTransform)!.rect.width;
        SetProgress(1);
    }

    [Button("设置进度")]
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        // 变化小于阈值：直接忽略，不处理
        if (Mathf.Abs(progress - _curProgress) < _minProgressDelta) return;
        _curProgress = progress;

        // 正在播：等它播完再补一次到最新目标；空闲则立即播
        if (_progressTween != null && _progressTween.IsActive())
        {
            _progressTween.onComplete = Play;   // 覆盖式设置，不会叠加回调
            return;
        }

        Play();
    }

    // 顺序播放：血条1 播完 → 血条2，终点取最新 _curProgress
    private void Play()
    {
        _progressTween = DOTween.Sequence()
            .Append(_imgBar1.rectTransform.DOSizeDelta(BarSize(_imgBar1), _bar1Duration).SetEase(Ease.OutCubic))
            .Append(_imgBar2.rectTransform.DOSizeDelta(BarSize(_imgBar2), _bar2Duration).SetEase(Ease.OutCubic));
    }

    private Vector2 BarSize(Image bar) => new(_curProgress * _barWidth, bar.rectTransform.rect.height);
}

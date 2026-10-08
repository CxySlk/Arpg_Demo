using System;
using System.Globalization;
using DG.Tweening;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

/// <summary>
/// 单条战斗飘字实例(3D 世界空间版本)。
///
/// 【与 UI 版的区别】
/// - 使用 <see cref="TextMeshPro"/>(3D 世界文字),不是 UGUI 的 TextMeshProUGUI;
/// - 物体直接放置在世界坐标(挂在场景/池根下,不依赖任何 Canvas);
/// - 上飘在世界空间沿 Y 轴进行;淡出直接改 TMP 顶点颜色 alpha(无 CanvasGroup);
/// - 可选始终面向主相机(Billboard),保证从任意角度都能正面看到数字。
///
/// 生命周期:FloatingTextManager 从对象池取出 → Play() 播放 → 播完自动
/// PushGameObject() 回池(池由 PoolSystem.Global 管理)。
/// </summary>
public class FloatingTextItem : MonoBehaviour
{
    [LabelText("文本"), SerializeField] private TextMeshPro _text;
    [LabelText("普通伤害颜色"), SerializeField] private Color _damageColor = new(1f, 0.84f, 0.2f);
    [LabelText("暴击颜色"), SerializeField] private Color _criticalColor = new(1f, 0.45f, 0.15f);
    [LabelText("治疗颜色"), SerializeField] private Color _healColor = new(0.35f, 1f, 0.45f);
    [LabelText("普通字号(世界单位)"), SerializeField] private float _normalFontSize = 2f;
    [LabelText("暴击字号倍率"), SerializeField] private float _criticalScale = 1.5f;
    [LabelText("上飘距离(世界单位)"), SerializeField] private float _floatDistance = 1.2f;
    [LabelText("上飘时长(秒)"), SerializeField] private float _floatDuration = 0.8f;
    [LabelText("淡出时长(秒)"), SerializeField] private float _fadeDuration = 0.35f;
    [LabelText("初始缩放"), SerializeField] private Vector3 _popScale = new(1.3f, 1.3f, 1.3f);
    [LabelText("弹出时长(秒)"), SerializeField] private float _popDuration = 0.15f;
    [LabelText("是否始终面向主相机(Billboard)"), SerializeField] private bool _faceCamera = true;

    private Color _baseColor;
    private Tween _tween;

    /// <summary>
    /// 开始播放一条飘字。
    /// 调用前 Manager 已把本物体 transform.position 设到世界出生点,
    /// 本方法只负责设置文本样式并驱动上飘/淡出动画。
    /// </summary>
    public void Play(FloatTextRequest request)
    {
        // 复位(池中可能残留上一条的状态)
        if (_tween != null && _tween.IsActive())
            _tween.Kill();
        _tween = null;

        transform.localScale = Vector3.one;
        _baseColor = GetTextColor(request.kind, request.isCritical);
        _text.fontSize = _normalFontSize * (request.isCritical ? _criticalScale : 1f);
        _text.color = _baseColor;
        _text.text =request.value.ToString("0.##", CultureInfo.CurrentCulture);
        
        FaceToCameraIfEnabled();

        // 弹出(放大回落到 1)→ 上飘(世界 Y 轴)→ 淡出 → 回池
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * _floatDistance;
        transform.localScale = _popScale;

        // 3D TMP 无 CanvasGroup,淡出改为手动修改文字颜色 alpha
        _tween = DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOMove(endPos, _floatDuration).SetEase(Ease.OutCubic))
            .Append(DOTween.To(FadeAlphaGetter, FadeAlphaSetter, 0f, _fadeDuration).SetEase(Ease.InQuad))
            .OnComplete(Recycle);
    }

    private float FadeAlphaGetter() => _text.color.a;
    private void FadeAlphaSetter(float alpha)
    {
        Color c = _text.color;
        c.a = alpha;
        _text.color = c;
    }

    /// <summary>每帧让文字正面朝向主相机(世界飘字也保持可读)。</summary>
    private void Update()
    {
        if (_faceCamera)
            FaceToCameraIfEnabled();
    }

    private void FaceToCameraIfEnabled()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 让文字平面朝向相机方向(forward 指向相机)
        Vector3 dir = transform.position - cam.transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private Color GetTextColor(FloatTextKind kind, bool isCritical)
    {
        switch (kind)
        {
            case FloatTextKind.Heal:
                return _healColor;
            case FloatTextKind.Damage:
            default:
                return isCritical ? _criticalColor : _damageColor;
        }
    }

    private void Recycle()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
        this.gameObject.PushGameObject();
    }

    /// <summary>防御:被池 SetActive(false)/销毁等场景下终止残留 Tween,避免回池后报错。</summary>
    private void OnDisable()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();
        _tween = null;
    }
}

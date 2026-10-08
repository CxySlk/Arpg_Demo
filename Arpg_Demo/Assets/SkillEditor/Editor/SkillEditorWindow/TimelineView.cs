using System;
using GAS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 时间轴：维护当前帧/总帧状态，绘制刻度线，处理选帧/缩放/帧号输入。
    /// 帧变化通过 FrameChanged(frame, isOutOfRange) 广播，由主窗口协调 scrub 与清理。
    /// </summary>
    public class TimelineView
    {
        private readonly SkillEditorUI _ui;
        private readonly ContentView _content;
        private readonly SkillEditorConfig _config;
        private AbilityAsset _asset;
        /// <summary>是否已从资产加载过（防止默认帧数/未加载状态被写回资产）</summary>
        private bool _assetLoaded;
        /// <summary>是否已加载技能资产（供 SaveConfig 判断是否可写回总帧数）</summary>
        internal bool IsAssetLoaded => _assetLoaded;
        private bool _isScrubbing;

        /// <summary>帧变化：(frame, 是否超出原总帧范围)</summary>
        public event Action<int, bool> FrameChanged;
        public event Action<int> FrameCountChanged;

        private int _currentFrameIndex;
        public int CurrentFrameIndex
        {
            get => _currentFrameIndex;
            set
            {
                value = Mathf.Max(0, value);
                if (value == _currentFrameIndex) return;
                bool outOfRange = value > _frameCount;
                if (outOfRange) FrameCount = value;   // 超出范围则自动扩展总帧
                _currentFrameIndex = value;
                if (_ui.curFrameIndexField != null) _ui.curFrameIndexField.SetValueWithoutNotify(value);
                _ui.timeShaft?.MarkDirtyRepaint();
                SceneView.RepaintAll();
                FrameChanged?.Invoke(value, outOfRange);
            }
        }

        private int _frameCount = 30;
        public int FrameCount
        {
            get => _frameCount;
            set
            {
                value = Mathf.Max(1, value);
                if (value == _frameCount) return;
                _frameCount = value;
                // 只有已从资产加载过才写回资产，防止默认 30 在窗口重建等时机覆盖资产真实总帧数
                if (_asset != null && _assetLoaded)
                {
                    _asset.totalFrames = value;
                    EditorUtility.SetDirty(_asset);
                }
                if (_ui.totalFrameIndexField != null) _ui.totalFrameIndexField.SetValueWithoutNotify(value);
                FrameCountChanged?.Invoke(value);
            }
        }

        /// <summary>当前帧宽（像素）</summary>
        public float FrameUnitWidth => _config.frameUnitWidth;

        public TimelineView(SkillEditorUI ui, ContentView content, SkillEditorConfig config)
        {
            _ui = ui;
            _content = content;
            _config = config;
        }

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        public void Reset(AbilityAsset asset, int totalFrames)
        {
            BindAsset(asset);
            _assetLoaded = asset != null;   // 只有从资产加载过才允许把总帧数写回资产
            _currentFrameIndex = 0;
            _frameCount = Mathf.Max(1, totalFrames);
            if (_ui.curFrameIndexField != null) _ui.curFrameIndexField.SetValueWithoutNotify(_currentFrameIndex);
            if (_ui.totalFrameIndexField != null) _ui.totalFrameIndexField.SetValueWithoutNotify(_frameCount);
        }

        public void Init()
        {
            _ui.timeShaft.onGUIHandler = DrawTimeShaft;
            _ui.timeShaft.RegisterCallback<WheelEvent>(e =>
            {
                int delta = -(int)e.delta.y;
                _config.frameUnitWidth = Mathf.Clamp(_config.frameUnitWidth + delta,
                    SkillEditorConfig.standFrameUnitWidth,
                    SkillEditorConfig.maxFrameWidthLV * SkillEditorConfig.standFrameUnitWidth);
                _ui.timeShaft.MarkDirtyRepaint();
                _content.RightListView.style.width = _frameCount * _config.frameUnitWidth;
            });
            _ui.timeShaft.RegisterCallback<MouseDownEvent>(e =>
            {
                _isScrubbing = true;
                CurrentFrameIndex = GetFrameIndexByLocalPos(e.localMousePosition.x);
            });
            _ui.timeShaft.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (_isScrubbing)
                    CurrentFrameIndex = GetFrameIndexByLocalPos(e.localMousePosition.x);
            });
            _ui.timeShaft.RegisterCallback<MouseUpEvent>(e => _isScrubbing = false);
            _ui.timeShaft.RegisterCallback<MouseCaptureOutEvent>(e => _isScrubbing = false);

            // 初始同步用 SetValueWithoutNotify：避免"默认帧数"触发回调写回资产（域重载/Play 模式重建窗口时的关键防护）
            _ui.curFrameIndexField.SetValueWithoutNotify(_currentFrameIndex);
            _ui.curFrameIndexField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == e.previousValue) return;
                CurrentFrameIndex = e.newValue;
            });

            _ui.totalFrameIndexField.SetValueWithoutNotify(_frameCount);
            _ui.totalFrameIndexField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == e.previousValue) return;
                FrameCount = e.newValue;
            });

            _ui.btnPreviousFrame.clicked += () => CurrentFrameIndex--;
            _ui.btnNextFrame.clicked += () => CurrentFrameIndex++;
        }

        private void DrawTimeShaft()
        {
            if (_ui.timeShaft == null) return;

            Rect rect = _ui.timeShaft.contentRect;
            int tickStep = SkillEditorConfig.maxFrameWidthLV + 1 - (int)(_config.frameUnitWidth / SkillEditorConfig.standFrameUnitWidth);
            tickStep = Mathf.Max(1, tickStep / 2);
            int index = Mathf.CeilToInt(_content.ContentOffsetPos / _config.frameUnitWidth);
            float startOffset = (_config.frameUnitWidth - _content.ContentOffsetPos % _config.frameUnitWidth) % _config.frameUnitWidth;

            Handles.BeginGUI();
            for (float i = startOffset; i < rect.width; i += _config.frameUnitWidth)
            {
                if (index % tickStep == 0)
                {
                    Handles.DrawLine(new Vector2(i, rect.height), new Vector2(i, rect.height - 10));
                    string indexStr = index.ToString();
                    GUI.Label(new Rect(i - indexStr.Length * 4.5f, 0, 35, 20), indexStr);
                }
                else
                    Handles.DrawLine(new Vector2(i, rect.height), new Vector2(i, rect.height - 5));
                index++;
            }

            float selectFrameOffset = _currentFrameIndex * _config.frameUnitWidth - _content.ContentOffsetPos;
            if (selectFrameOffset >= 0 && selectFrameOffset <= rect.width)
                Handles.DrawLine(new Vector2(selectFrameOffset, 0), new Vector2(selectFrameOffset, 1000));
            Handles.EndGUI();
        }

        private int GetFrameIndexByLocalPos(float localPos)
        {
            float mousePos = localPos + _content.ContentOffsetPos;
            return Mathf.RoundToInt(mousePos / _config.frameUnitWidth);
        }
    }
}

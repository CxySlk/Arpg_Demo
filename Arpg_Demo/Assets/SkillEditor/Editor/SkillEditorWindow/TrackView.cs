using System.Collections.Generic;
using GAS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 右侧轨道：事件条按起始帧/时长定位、GE 事件中心对齐、拖拽改帧、选中高亮。
    /// </summary>
    public class TrackView
    {
        private readonly SkillEditorUI _ui;
        private readonly ContentView _content;
        private readonly SkillEditorConfig _config;
        private readonly TimelineView _timeline;
        private readonly MarkEventListLayout _layout;
        private AbilityAsset _asset;

        private readonly Dictionary<MarkEventBase, VisualElement> _rightBars = new();
        private readonly Dictionary<MarkEventBase, VisualElement> _rightRowBgs = new();

        private MarkEventBase _draggingMark;
        private float _dragOffsetX;
        private float _dragBarWidth;
        private Vector2 _dragStartPos;

        /// <summary>左键点击事件条（非拖拽）时触发，用于选中</summary>
        public event System.Action<MarkEventBase> MarkClicked;

        /// <summary>拖拽改帧落定后触发，用于刷新属性面板等</summary>
        public event System.Action<MarkEventBase> MarkFrameChanged;

        public TrackView(SkillEditorUI ui, ContentView content, SkillEditorConfig config, TimelineView timeline,
            MarkEventListLayout layout)
        {
            _ui = ui;
            _content = content;
            _config = config;
            _timeline = timeline;
            _layout = layout;
        }

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        /// <summary>重建右侧轨道。resort = true 时按"分类 + 起始帧"重新排序(增删事件、切换资产、点保存时用)</summary>
        public void Rebuild(bool resort)
        {
            _content.RightListView.Clear();
            _rightBars.Clear();
            _rightRowBgs.Clear();

            if (_asset == null) return;

            if (resort) _layout.Reload(_asset.GetAllMarkEvents());

            List<MarkListEntry> entries = _layout.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                MarkListEntry entry = entries[i];
                if (entry.IsGroupHeader) BuildGroupHeaderRow(entry);
                else BuildTrackRow(entry);
            }

            _content.RightListView.style.minHeight = _layout.TotalHeight;
        }

        /// <summary>起始帧变化(属性面板输入)时只把事件条挪到新位置,不重建、不重新排序</summary>
        public void RefreshBarPosition(MarkEventBase mark)
        {
            if (mark == null) return;
            if (!_rightBars.TryGetValue(mark, out VisualElement bar)) return;

            float frameWidth = _config.frameUnitWidth;
            bar.style.left = BarLeftFor(mark, frameWidth, EventBarWidthFor(mark, frameWidth));
        }

        public void UpdateBarName(MarkEventBase mark, string name)
        {
            if (_rightBars.TryGetValue(mark, out VisualElement bar) && bar.childCount > 0)
                ((Label)bar[0]).text = name;
        }

        public void RefreshSelection(MarkEventBase selected)
        {
            foreach (KeyValuePair<MarkEventBase, VisualElement> kv in _rightBars)
            {
                bool isSel = kv.Key == selected;
                kv.Value.style.borderLeftWidth = isSel ? 2 : 1;
                kv.Value.style.borderLeftColor = isSel ? Color.white : Color.black;
            }
            foreach (KeyValuePair<MarkEventBase, VisualElement> kv in _rightRowBgs)
            {
                kv.Value.style.backgroundColor = kv.Key == selected
                    ? new Color(0.2f, 0.5f, 1f, 0.15f)
                    : Color.clear;
            }
        }

        /// <summary>右侧分类标题行(绝对定位,宽度铺满时间轴,与左侧同高度保证对齐)</summary>
        private void BuildGroupHeaderRow(MarkListEntry entry)
        {
            VisualElement header = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 0, top = entry.Top,
                    width = Length.Percent(100), height = entry.Height,
                    flexDirection = FlexDirection.Row, alignItems = Align.Center,
                    backgroundColor = SkillEditorConfig.GroupHeaderBackgroundColor
                }
            };
            header.Add(new Label
            {
                style =
                {
                    width = 4, height = entry.Height - 4, flexShrink = 0,
                    backgroundColor = entry.GroupColor
                }
            });
            header.Add(new Label(entry.GroupName)
            {
                style =
                {
                    fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 6,
                    color = SkillEditorConfig.GroupHeaderTextColor
                }
            });
            _content.RightListView.Add(header);
        }

        private void BuildTrackRow(MarkListEntry entry)
        {
            MarkEventBase mark = entry.Mark;
            float frameWidth = _config.frameUnitWidth;
            float top = entry.Top;

            // 行选中背景层（全宽覆盖整行，宽度用百分比跟随列表宽度，缩放后不用重建也对）
            VisualElement rowBg = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 0, top = top,
                    width = Length.Percent(100), height = entry.Height,
                    backgroundColor = Color.clear
                }
            };
            _rightRowBgs[mark] = rowBg;
            _content.RightListView.Add(rowBg);

            // 事件条
            float barWidth = EventBarWidthFor(mark, frameWidth);
            float barLeft = BarLeftFor(mark, frameWidth, barWidth);

            VisualElement bar = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, top = top + SkillEditorConfig.RowGap,
                    left = barLeft, width = barWidth, height = SkillEditorConfig.RowHeight - 4,
                    backgroundColor = SkillEditorConfig.GetMarkColor(mark),
                    borderLeftWidth = 1, borderLeftColor = Color.black
                }
            };
            Label label = new(mark.Name) { style = { fontSize = 11, color = Color.black, overflow = Overflow.Hidden } };
            bar.Add(label);
            _rightBars[mark] = bar;
            _content.RightListView.Add(bar);

            // 点击选中 + 拖拽改帧
            bar.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                _draggingMark = mark;
                _dragStartPos = evt.localPosition;
                _dragOffsetX = evt.localPosition.x;
                _dragBarWidth = bar.resolvedStyle.width > 0 ? bar.resolvedStyle.width : bar.style.width.value.value;
                bar.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            bar.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_draggingMark != mark) return;
                int frame = Mathf.RoundToInt((evt.localPosition.x - _dragOffsetX + mark.StartFrame * frameWidth) / frameWidth);
                frame = Mathf.Clamp(frame, 0, _timeline.FrameCount);
                if (frame != mark.StartFrame)
                {
                    if (_asset.MoveMarkEvent(mark, frame))
                    {
                        bar.style.left = BarLeftFor(frame, mark, frameWidth, _dragBarWidth);
                        EditorUtility.SetDirty(_asset);
                    }
                    else
                    {
                        Debug.LogWarning("该帧已存在 GE 事件，同一帧最多一个 GE，无法移动");
                        bar.style.left = BarLeftFor(mark, frameWidth, _dragBarWidth);
                    }
                }
            });
            bar.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_draggingMark != mark) return;
                bool wasClick = Vector2.Distance(evt.localPosition, _dragStartPos) < 3f;
                _draggingMark = null;
                bar.ReleasePointer(evt.pointerId);
                EditorUtility.SetDirty(_asset);

                if (wasClick) MarkClicked?.Invoke(mark);
                else MarkFrameChanged?.Invoke(mark);
            });
        }

        /// <summary>单帧事件（单帧 GE / 自定义帧事件）：只影响所在帧，事件条宽度 = 一帧宽并居中于所在帧。
        /// 持续 GE（durationFrames > 1）与持续 Cue 一样按帧跨度左对齐绘制。</summary>
        private static bool IsSingleFrameEvent(MarkEventBase mark)
        {
            if (mark is ReleaseGEMarkEvent ge) return ge.GetDurationFrames() <= 1;
            return mark is FrameEvent;
        }

        /// <summary>事件条左边界 = 起始帧 × 帧宽;单帧事件（GE/自定义）中心对准所在帧</summary>
        private float BarLeftFor(MarkEventBase mark, float frameWidth, float barWidth)
        {
            return BarLeftFor(mark.StartFrame, mark, frameWidth, barWidth);
        }

        private float BarLeftFor(int startFrame, MarkEventBase mark, float frameWidth, float barWidth)
        {
            float left = startFrame * frameWidth;
            if (IsSingleFrameEvent(mark))
                left -= barWidth * 0.5f;
            return left;
        }

        private float EventBarWidthFor(MarkEventBase mark, float frameWidth)
        {
            if (mark is DurationCueMarkEvent cueMark && cueMark.DurationCue != null)
                return SkillEditorConfig.DurationFrames(cueMark.DurationCue) * frameWidth;
            if (mark is ReleaseGEMarkEvent geMark)
                return Mathf.Max(1, geMark.durationFrames) * frameWidth;
            if (mark is FrameEvent)
                return frameWidth;
            return SkillEditorConfig.EventBarWidth;
        }
    }
}

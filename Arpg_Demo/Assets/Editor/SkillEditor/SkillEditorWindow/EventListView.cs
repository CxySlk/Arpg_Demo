using System;
using System.Collections.Generic;
using GAS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 左侧事件列表：添加 Cue/GE 事件、重命名、删除、行选中、拖拽排序。
    /// 选中变化通过 SelectionChanged 广播；结构变化通过 MarkEventsChanged 广播。
    /// </summary>
    public class EventListView
    {
        private readonly SkillEditorUI _ui;
        private readonly ContentView _content;
        private readonly TimelineView _timeline;
        private readonly MarkEventListLayout _layout;
        private AbilityAsset _asset;

        public event Action<MarkEventBase> SelectionChanged;
        public event Action MarkEventsChanged;
        public event Action<MarkEventBase, string> NameChanged;

        private MarkEventBase _selectedMark;
        public MarkEventBase SelectedMark => _selectedMark;

        private readonly Dictionary<MarkEventBase, VisualElement> _leftRows = new();

        public EventListView(SkillEditorUI ui, ContentView content, TimelineView timeline, MarkEventListLayout layout)
        {
            _ui = ui;
            _content = content;
            _timeline = timeline;
            _layout = layout;
        }

        public void BindAsset(AbilityAsset asset) => _asset = asset;

        public void Reset(AbilityAsset asset)
        {
            BindAsset(asset);
            _selectedMark = null;
        }

        /// <summary>重建左侧列表。resort = true 时按"分类 + 起始帧"重新排序(增删事件、切换资产、点保存时用)</summary>
        public void Rebuild(bool resort)
        {
            _content.LeftListView.Clear();
            _leftRows.Clear();

            if (_asset == null)
            {
                _content.LeftListView.Add(new Label("请选择技能配置"));
                return;
            }

            if (resort) _layout.Reload(_asset.GetAllMarkEvents());

            _content.LeftListView.Add(BuildAddRow());

            List<MarkListEntry> entries = _layout.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                MarkListEntry entry = entries[i];
                if (entry.IsGroupHeader)
                {
                    _content.LeftListView.Add(BuildGroupHeaderRow(entry));
                    continue;
                }
                _leftRows[entry.Mark] = BuildEventRow(entry.Mark);
            }

            _content.LeftListView.style.minHeight = _layout.TotalHeight;

            RefreshSelection();
        }

        /// <summary>分类标题行(与右侧同高度,保证左右两列对齐)</summary>
        private VisualElement BuildGroupHeaderRow(MarkListEntry entry)
        {
            VisualElement header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center,
                    height = entry.Height, marginTop = SkillEditorConfig.RowGap,
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
            return header;
        }

        public void Select(MarkEventBase mark)
        {
            if (_selectedMark == mark) return;
            _selectedMark = mark;
            RefreshSelection();
            SelectionChanged?.Invoke(mark);
        }

        private VisualElement BuildAddRow()
        {
            VisualElement addRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginLeft = 4, marginRight = 4, height = SkillEditorConfig.AddButtonsRowHeight, alignItems = Align.Center }
            };
            Button addCueBtn = new(() => AddMarkEvent<DurationCueMarkEvent>()) { text = "＋Cue事件" };
            addCueBtn.style.flexGrow = 1;
            addRow.Add(addCueBtn);
            Button addGeBtn = new(() => AddMarkEvent<ReleaseGEMarkEvent>()) { text = "＋GE事件" };
            addGeBtn.style.flexGrow = 1;
            addRow.Add(addGeBtn);
            Button addFrameEventBtn = new(() => AddMarkEvent<FrameEvent>()) { text = "＋自定义事件" };
            addFrameEventBtn.style.flexGrow = 1;
            addRow.Add(addFrameEventBtn);
            return addRow;
        }

        private VisualElement BuildEventRow(MarkEventBase mark)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, height = SkillEditorConfig.RowHeight, width = Length.Percent(100),
                    marginLeft = 4, marginRight = 4, marginTop = SkillEditorConfig.RowGap, alignItems = Align.Center,
                    borderBottomWidth = 1, borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1), position = Position.Relative
                }
            };

            Label colorBox = new Label
            {
                style = { width = 14, height = SkillEditorConfig.RowHeight - 6, backgroundColor = SkillEditorConfig.GetMarkColor(mark), marginRight = 4, flexShrink = 0 }
            };
            row.Add(colorBox);

            Label typeLabel = new(SkillEditorConfig.GetMarkTypeName(mark)) { style = { width = 30, fontSize = 11, flexShrink = 0 } };
            row.Add(typeLabel);

            TextField nameField = new TextField
            {
                value = mark.Name,
                style = { flexGrow = 1, flexShrink = 1, minWidth = 0, marginRight = 54, height = SkillEditorConfig.RowHeight - 6 }
            };
            nameField.RegisterValueChangedCallback(evt =>
            {
                mark.Name = evt.newValue;
                EditorUtility.SetDirty(_asset);
                NameChanged?.Invoke(mark, evt.newValue);
            });
            row.Add(nameField);

            Button delBtn = new(() =>
            {
                _asset.RemoveMarkEvent(mark);
                if (_selectedMark == mark) _selectedMark = null;
                EditorUtility.SetDirty(_asset);
                MarkEventsChanged?.Invoke();
            })
            { text = "删除", style = { position = Position.Absolute, right = 0, top = 0, width = 50, height = SkillEditorConfig.RowHeight - 2 } };
            row.Add(delBtn);

            row.RegisterCallback<MouseDownEvent>(evt => Select(mark));

            _content.LeftListView.Add(row);
            return row;
        }

        private void AddMarkEvent<T>() where T : MarkEventBase, new()
        {
            if (_asset == null)
            {
                Debug.LogWarning("请先选择技能配置");
                return;
            }
            MarkEventBase mark = _asset.AddMarkEvent<T>(_timeline.CurrentFrameIndex);
            _selectedMark = mark;
            EditorUtility.SetDirty(_asset);
            MarkEventsChanged?.Invoke();
        }

        /// <summary>高亮左侧选中行（右侧高亮由 TrackView 处理）</summary>
        public void RefreshSelection()
        {
            foreach (KeyValuePair<MarkEventBase, VisualElement> kv in _leftRows)
            {
                bool isSel = kv.Key == _selectedMark;
                kv.Value.style.backgroundColor = isSel ? new Color(0.2f, 0.5f, 1f, 0.3f) : Color.clear;
            }
        }
    }
}

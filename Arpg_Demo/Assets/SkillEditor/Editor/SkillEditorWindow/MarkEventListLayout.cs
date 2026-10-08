using System.Collections.Generic;
using GAS;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 列表里的一行:分类标题行(Mark == null)或事件行。
    /// Top/Height 由 MarkEventListLayout 统一算好,左右两个列表都按它排,保证两列纵向对齐。
    /// </summary>
    public class MarkListEntry
    {
        /// <summary>事件行对应的标记事件;分类标题行为 null</summary>
        public MarkEventBase Mark;

        /// <summary>分类标题行的标题(事件行为 null)</summary>
        public string GroupName;

        /// <summary>分类标题行的左侧色条颜色</summary>
        public Color GroupColor;

        /// <summary>行顶部的 y(相对列表顶端,右侧列表按绝对定位使用)</summary>
        public float Top;

        /// <summary>行高</summary>
        public float Height;

        public bool IsGroupHeader => Mark == null;
    }

    /// <summary>
    /// 事件列表的显示顺序与行位置:先按分类(Cue → GE → 自定义),同一分类内按起始帧升序。
    /// 注意:**只有 Reload 才会重新排序**。拖动事件条改帧期间不重排(行位置保持不动,只动事件条本身),
    /// 点"保存配置"时由主窗口调用一次 Reload,界面才重新排序。
    /// </summary>
    public class MarkEventListLayout
    {
        /// <summary>排序用的中间结构(带上原始下标,保证起始帧相同时顺序稳定)</summary>
        private struct SortItem
        {
            public MarkEventBase Mark;
            public int GroupOrder;
            public int SourceIndex;
        }

        private readonly List<MarkListEntry> _entries = new();

        /// <summary>按当前顺序排列的行集合</summary>
        public List<MarkListEntry> Entries => _entries;

        /// <summary>列表内容总高度(含分类标题行)</summary>
        public float TotalHeight { get; private set; } = SkillEditorConfig.AddButtonsRowHeight + 4;

        /// <summary>按分类 + 起始帧重新排序,并重算每一行的位置</summary>
        public void Reload(List<MarkEventBase> marks)
        {
            _entries.Clear();

            List<SortItem> items = new();
            if ( marks != null )
            {
                for ( int i = 0; i < marks.Count; i++ )
                {
                    if ( marks[i] == null ) continue;
                    items.Add(new SortItem
                    {
                        Mark = marks[i],
                        GroupOrder = SkillEditorConfig.GetMarkGroupOrder(marks[i]),
                        SourceIndex = i
                    });
                }
            }

            // 分类优先 → 起始帧升序 → 原顺序(保证稳定)
            items.Sort((a, b) =>
            {
                if ( a.GroupOrder != b.GroupOrder ) return a.GroupOrder.CompareTo(b.GroupOrder);
                int frameCompare = a.Mark.StartFrame.CompareTo(b.Mark.StartFrame);
                return frameCompare != 0 ? frameCompare : a.SourceIndex.CompareTo(b.SourceIndex);
            });

            // 每个分类的数量,用于标题文案
            Dictionary<int, int> groupCounts = new();
            for ( int i = 0; i < items.Count; i++ )
            {
                groupCounts.TryGetValue(items[i].GroupOrder, out int count);
                groupCounts[items[i].GroupOrder] = count + 1;
            }

            float top = SkillEditorConfig.AddButtonsRowHeight;
            int currentGroup = int.MinValue;

            for ( int i = 0; i < items.Count; i++ )
            {
                SortItem item = items[i];

                if ( item.GroupOrder != currentGroup )
                {
                    currentGroup = item.GroupOrder;
                    _entries.Add(new MarkListEntry
                    {
                        Mark = null,
                        GroupName = SkillEditorConfig.GetMarkGroupName(item.GroupOrder, groupCounts[item.GroupOrder]),
                        GroupColor = SkillEditorConfig.GetMarkColor(item.Mark),
                        Top = top,
                        Height = SkillEditorConfig.GroupHeaderHeight
                    });
                    top += SkillEditorConfig.GroupHeaderHeight + SkillEditorConfig.RowGap;
                }

                _entries.Add(new MarkListEntry
                {
                    Mark = item.Mark,
                    Top = top,
                    Height = SkillEditorConfig.RowHeight
                });
                top += SkillEditorConfig.RowHeight + SkillEditorConfig.RowGap;
            }

            TotalHeight = Mathf.Max(100, top + 4);
        }
    }
}

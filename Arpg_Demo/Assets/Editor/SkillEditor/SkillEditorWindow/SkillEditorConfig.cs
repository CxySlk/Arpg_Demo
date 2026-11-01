using GAS;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器配置：帧宽、布局常量、事件样式工具。
    /// 所有可配置数字集中于此，避免魔法数字散落。
    /// </summary>
    public class SkillEditorConfig
    {
        /// <summary>当前帧宽（像素），滚轮缩放</summary>
        public float frameUnitWidth = 10;

        public const float standFrameUnitWidth = 10;
        public const int maxFrameWidthLV = 10;

        /// <summary>
        /// 持续 Cue 的时长（秒）→ 帧数：编辑器的轨道宽度与 scrub 范围都按帧算。
        /// 至少 1 帧，避免 0 时长的条看不见。
        /// </summary>
        public static int DurationFrames(GameplayCueDurational cue)
        {
            if (cue == null) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(cue.DurationSeconds * LLKFrame.TimeSystem.FrameRate));
        }

        // 布局常量
        public const float RowHeight = 24;
        public const float RowGap = 2;
        /// <summary>分类标题行高度(左右两个列表都用它,保证两列纵向对齐)</summary>
        public const float GroupHeaderHeight = 20;

        /// <summary>分类标题行背景色</summary>
        public static readonly Color GroupHeaderBackgroundColor = new(0.18f, 0.18f, 0.18f, 1f);
        /// <summary>分类标题行文字色</summary>
        public static readonly Color GroupHeaderTextColor = new(0.8f, 0.8f, 0.8f, 1f);
        public const float AddButtonsRowHeight = 34;
        public const float EventBarWidth = 60;
        public const float LeftColumnWidth = 222;
        public const float PropertyPanelMinWidth = 340;
        /// <summary>事件详情内容最小宽度：固定铺开在较宽布局内，超出面板时通过横向滚动查看右侧</summary>
        public const float PropertyPanelContentWidth = 460;

        /// <summary>事件类型色块颜色</summary>
        public static Color GetMarkColor(MarkEventBase mark)
        {
            if (mark == null) return Color.gray;
            if (mark is DurationCueMarkEvent) return new Color(0.2f, 0.6f, 1f);
            if (mark is ReleaseGEMarkEvent) return new Color(1f, 0.6f, 0.2f);
            if (mark is FrameEvent) return new Color(0.6f, 0.9f, 0.3f);
            return Color.gray;
        }

        /// <summary>事件类型显示名</summary>
        public static string GetMarkTypeName(MarkEventBase mark)
        {
            if (mark == null) return "无效事件";
            if (mark is DurationCueMarkEvent) return "Cue";
            if (mark is ReleaseGEMarkEvent) return "GE";
            if (mark is FrameEvent) return "自定义";
            return mark.GetType().Name;
        }

        /// <summary>分类排序权重:越小排越前(列表按此分类依次排列)</summary>
        public static int GetMarkGroupOrder(MarkEventBase mark)
        {
            if (mark == null) return int.MaxValue;
            if (mark is DurationCueMarkEvent) return 0;
            if (mark is ReleaseGEMarkEvent) return 1;
            if (mark is FrameEvent) return 2;
            return 3;
        }

        /// <summary>分类标题名</summary>
        public static string GetMarkGroupName(int groupOrder, int count)
        {
            switch ( groupOrder )
            {
                case 0: return $"Cue 事件 ({count})";
                case 1: return $"GE 事件 ({count})";
                case 2: return $"自定义事件 ({count})";
                default: return $"其他事件 ({count})";
            }
        }
    }
}

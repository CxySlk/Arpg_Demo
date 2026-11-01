using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 左右滚动布局：左右列表垂直滚动同步、左侧列宽锁定、提供右侧内容偏移。
    /// </summary>
    public class ContentView
    {
        private readonly SkillEditorUI _ui;

        public VisualElement LeftListView => _ui.leftListView;
        public VisualElement RightListView => _ui.rightListView;

        /// <summary>右侧滚动偏移（时间轴绘制、事件条定位用）</summary>
        public float ContentOffsetPos => Mathf.Abs(_ui.rightScrollView.scrollOffset.x);

        public ContentView(SkillEditorUI ui)
        {
            _ui = ui;

            _ui.rightScrollView.verticalScroller.valueChanged += e => _ui.leftScrollView.verticalScroller.value = e;
            _ui.rightScrollView.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _ui.leftScrollView.verticalScroller.valueChanged += e => _ui.rightScrollView.verticalScroller.value = e;

            // 锁定左侧列宽，防止内容伸缩
            _ui.contentLeft.style.width = SkillEditorConfig.LeftColumnWidth;
            _ui.contentLeft.style.minWidth = SkillEditorConfig.LeftColumnWidth;
            _ui.contentLeft.style.maxWidth = SkillEditorConfig.LeftColumnWidth;
            _ui.leftListView.style.width = SkillEditorConfig.LeftColumnWidth;
            _ui.leftListView.style.overflow = Overflow.Hidden;
        }
    }
}

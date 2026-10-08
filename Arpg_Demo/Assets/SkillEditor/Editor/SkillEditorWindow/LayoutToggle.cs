using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 左右面板折叠控制：左侧轨道列表 / 右侧事件详情可收起，收起后中间区域自动占满。
    /// 折叠状态用 EditorPrefs 持久化，重开窗口保留。
    /// </summary>
    public class LayoutToggle
    {
        private const string LeftKey = "SkillEditor.LeftCollapsed";
        private const string RightKey = "SkillEditor.RightCollapsed";

        private readonly SkillEditorUI _ui;
        private readonly Button _btnLeft;
        private readonly Button _btnRight;

        public LayoutToggle(SkillEditorUI ui)
        {
            _ui = ui;
            _btnLeft = ui.root.Q<Button>("btnToggleLeft");
            _btnRight = ui.root.Q<Button>("btnToggleRight");

            if (_btnLeft != null) _btnLeft.clicked += ToggleLeft;
            if (_btnRight != null) _btnRight.clicked += ToggleRight;

            // 应用上次折叠状态
            ApplyLeft(UnityEditor.EditorPrefs.GetBool(LeftKey, false));
            ApplyRight(UnityEditor.EditorPrefs.GetBool(RightKey, false));
        }

        private void ToggleLeft()
        {
            bool collapsed = !UnityEditor.EditorPrefs.GetBool(LeftKey, false);
            UnityEditor.EditorPrefs.SetBool(LeftKey, collapsed);
            ApplyLeft(collapsed);
        }

        private void ToggleRight()
        {
            bool collapsed = !UnityEditor.EditorPrefs.GetBool(RightKey, false);
            UnityEditor.EditorPrefs.SetBool(RightKey, collapsed);
            ApplyRight(collapsed);
        }

        private void ApplyLeft(bool collapsed)
        {
            if (_ui.contentLeft == null) return;
            _ui.contentLeft.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (_btnLeft != null)
                _btnLeft.text = collapsed ? "▶" : "◀";
        }

        private void ApplyRight(bool collapsed)
        {
            if (_ui.contentRightMost == null) return;
            _ui.contentRightMost.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            if (_btnRight != null)
                _btnRight.text = collapsed ? "◀" : "▶";
        }
    }
}

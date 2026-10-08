using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// UXML 元素引用容器。创建 GUI 时一次性查找全部元素，
    /// 之后各视图通过它访问，避免到处 root.Q 与 nameof(字段) 的脆弱写法。
    /// </summary>
    public class SkillEditorUI
    {
        /// <summary>UXML 根元素</summary>
        public readonly VisualElement root;

        // 顶部
        public Button btnSkillEditorScene;
        public Button btnOldScene;
        public Button btnConfigInfo;
        public ObjectField previewPrefabField;
        public ObjectField previewObjectField;
        public ObjectField skillClipConfigField;

        // 时间轴
        public IMGUIContainer timeShaft;
        public IntegerField curFrameIndexField;
        public IntegerField totalFrameIndexField;
        public Button btnPreviousFrame;
        public Button btnNextFrame;
        public Button btnPlay;

        // 内容区
        public ScrollView leftScrollView;
        public ScrollView rightScrollView;
        public VisualElement leftListView;
        public VisualElement rightListView;
        public VisualElement contentLeft;
        public VisualElement contentRightMost;

        public SkillEditorUI(VisualElement root)
        {
            this.root = root;

            btnSkillEditorScene = root.Q<Button>("btnSkillEditorScene");
            btnOldScene = root.Q<Button>("btnOldScene");
            btnConfigInfo = root.Q<Button>("btnConfigInfo");
            previewPrefabField = root.Q<ObjectField>("previewPrefabField");
            previewObjectField = root.Q<ObjectField>("previewObjectField");
            skillClipConfigField = root.Q<ObjectField>("skillClipConfigField");

            timeShaft = root.Q<IMGUIContainer>("timeShaft");
            curFrameIndexField = root.Q<IntegerField>("curFrameIndexField");
            totalFrameIndexField = root.Q<IntegerField>("totalFrameIndexField");
            btnPreviousFrame = root.Q<Button>("btnPreviousFrame");
            btnNextFrame = root.Q<Button>("btnNextFrame");
            btnPlay = root.Q<Button>("btnPlay");

            leftScrollView = root.Q<ScrollView>("leftScrollView");
            rightScrollView = root.Q<ScrollView>("rightScrollView");
            leftListView = leftScrollView.Q<VisualElement>("leftListView");
            rightListView = rightScrollView.Q<VisualElement>("rightListView");
            contentLeft = root.Q<VisualElement>("ContentLeft");
            contentRightMost = root.Q<VisualElement>("ContentRightMost");
        }
    }
}

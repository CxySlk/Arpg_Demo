using GAS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器主窗口：组合各模块视图，协调模块间事件。
    /// 所有运行时状态放在各普通类视图中（CreateGUI 时 new），
    /// 主窗口仅保留需跨域重载的 [SerializeField] 状态（配置资产、预览模型）。
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        [SerializeField] private AbilityAsset abilityAsset;
        [SerializeField] private Transform previewModelRoot;
        [SerializeField] private Transform previewModel;

        private SkillEditorUI ui;
        private ContentView content;
        private TimelineView timeline;
        private PreviewSceneManager preview;
        private EventListView eventList;
        private TrackView track;
        /// <summary>事件列表的显示顺序与行位置(分类 + 起始帧),左右两个列表共用</summary>
        private MarkEventListLayout markLayout;
        private PropertyPanelView propertyPanel;
        private PlaybackController playback;
        private SceneGizmoView gizmos;

        // 供 PreviewSceneManager 读写（域重载保留的预览模型引用）
        internal Transform PreviewModelRoot { get => previewModelRoot; set => previewModelRoot = value; }
        internal Transform PreviewModel { get => previewModel; set => previewModel = value; }

        /// <summary>记住最近使用的技能资产路径,域重载/Play 模式后用于恢复</summary>
        private const string SkillAssetPathKey = "SkillEditor.LastAbilityAssetPath";

        [MenuItem("技能编辑器/打开技能编辑器")]
        public static void OpenSkillEditorWindow()
        {
            SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
            wnd.titleContent = new GUIContent("技能编辑器窗口");
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(m_VisualTreeAsset.Instantiate());

            ui = new SkillEditorUI(rootVisualElement);
            content = new ContentView(ui);
            SkillEditorConfig config = new();
            markLayout = new MarkEventListLayout();
            timeline = new TimelineView(ui, content, config);
            preview = new PreviewSceneManager(ui, this);
            eventList = new EventListView(ui, content, timeline, markLayout);
            track = new TrackView(ui, content, config, timeline, markLayout);
            propertyPanel = new PropertyPanelView(ui);
            playback = new PlaybackController(ui, content, timeline, preview);
            gizmos = new SceneGizmoView(abilityAsset, preview);
            new LayoutToggle(ui);

            // 资产绑定与初始化
            timeline.BindAsset(abilityAsset);
            eventList.BindAsset(abilityAsset);
            track.BindAsset(abilityAsset);
            propertyPanel.BindAsset(abilityAsset);
            playback.BindAsset(abilityAsset);

            BindEvents();
            BindTopBar();

            timeline.Init();
            preview.Init();
            playback.Init();
            gizmos.Register();

            // 恢复/同步能力配置（域重载/Play 模式后若序列化状态丢失,用 EditorPrefs 记住的路径恢复）
            if (abilityAsset == null)
            {
                string path = EditorPrefs.GetString(SkillAssetPathKey, "");
                if (!string.IsNullOrEmpty(path))
                    abilityAsset = AssetDatabase.LoadAssetAtPath<AbilityAsset>(path);
            }
            if (abilityAsset != null)
            {
                ui.skillClipConfigField.SetValueWithoutNotify(abilityAsset);
                ApplyConfig(abilityAsset);
            }

            // 脚本编译前清理预览物体
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            playback.ClearPreviewObjects();
        }

        private void OnDestroy()
        {
            playback.StopPlayback();
            gizmos.Unregister();
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            playback.ClearPreviewObjects();
        }

        private void BindEvents()
        {
            // 帧变化 → scrub 预览（传当前预览模型物体：动画/粒子/音效都以它为目标）
            timeline.FrameChanged += (frame, outOfRange) => playback.ScrubTo(frame, outOfRange,
                preview.PreviewModel != null ? preview.PreviewModel.gameObject : null);
            // 总帧变化 → 轨道宽度与重建(不重排序)
            timeline.FrameCountChanged += count =>
            {
                ui.rightListView.style.width = count * timeline.FrameUnitWidth;
                track.Rebuild(false);
            };

            // 事件选中 → 属性面板 + 右侧高亮 + 场景手柄
            eventList.SelectionChanged += mark =>
            {
                propertyPanel.Rebuild(mark);
                track.RefreshSelection(mark);
                gizmos.SelectedMark = mark;      // 场景手柄只绘制选中的事件
                SceneView.RepaintAll();
            };
            // 事件结构变化（增删）→ 全量重建并重新排序
            eventList.MarkEventsChanged += () =>
            {
                eventList.Rebuild(true);
                track.Rebuild(true);
                track.RefreshSelection(eventList.SelectedMark);
                propertyPanel.Rebuild(eventList.SelectedMark);
                gizmos.SelectedMark = eventList.SelectedMark;   // 新增/删除事件后同步选中
                SceneView.RepaintAll();
            };
            // 事件名变化 → 同步右侧条文案
            eventList.NameChanged += (mark, name) => track.UpdateBarName(mark, name);
            // 点击右侧事件条 → 选中（同步左侧高亮 + 属性面板）
            track.MarkClicked += mark => eventList.Select(mark);
            // 拖拽改帧落定 → 刷新属性面板（起始帧等同步）
            track.MarkFrameChanged += mark => propertyPanel.Rebuild(mark);

            // 属性面板内容变化（时长等）→ 重建轨道(保持当前行顺序,不重排序)
            propertyPanel.ContentChanged += () => track.Rebuild(false);
            // 属性面板改起始帧 → 只挪事件条,不重建不重排(避免排序跳动)
            propertyPanel.StartFrameChanged += mark => track.RefreshBarPosition(mark);
            // 粒子 Cue 参数变化 → 刷新预览实例
            propertyPanel.ParticleTransformChanged += mark => playback.RefreshPreviewParticleTransform(mark);
            // 场景手柄改粒子位置/旋转 → 刷新预览实例
            gizmos.ParticleTransformChanged += mark => playback.RefreshPreviewParticleTransform(mark);
        }

        private void BindTopBar()
        {
            // 技能配置选择
            ui.skillClipConfigField.objectType = typeof(AbilityAsset);
            ui.skillClipConfigField.allowSceneObjects = false;
            ui.skillClipConfigField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == e.previousValue) return;
                SaveConfig();
                abilityAsset = e.newValue as AbilityAsset;
                // 记住最近使用的技能资产,域重载/Play 模式后自动恢复
                if (abilityAsset != null)
                    EditorPrefs.SetString(SkillAssetPathKey, AssetDatabase.GetAssetPath(abilityAsset));
                else
                    EditorPrefs.DeleteKey(SkillAssetPathKey);
                ApplyConfig(abilityAsset);
            });
        }

        /// <summary>应用技能配置到各视图并重建</summary>
        private void ApplyConfig(AbilityAsset asset)
        {
            playback.StopPlayback();
            timeline.Reset(asset, asset != null ? asset.totalFrames : 30);
            eventList.Reset(asset);
            track.BindAsset(asset);
            propertyPanel.BindAsset(asset);
            playback.BindAsset(asset);
            gizmos.BindAsset(asset);
            gizmos.SelectedMark = null;   // 切换技能配置后无选中,不绘制场景手柄

            eventList.Rebuild(true);
            track.Rebuild(true);
            propertyPanel.Rebuild(eventList.SelectedMark);
            ui.rightListView.style.width = timeline.FrameCount * timeline.FrameUnitWidth;
        }

        /// <summary>保存当前配置文件到资产,并刷新界面重新排序(拖拽改帧期间不排序,保存后才排)</summary>
        internal void SaveConfig()
        {
            if (abilityAsset == null) return;
            if (!timeline.IsAssetLoaded) return;   // 时间轴未从资产加载过,不写回,防止默认帧数覆盖资产真实值
            abilityAsset.totalFrames = timeline.FrameCount;
            EditorUtility.SetDirty(abilityAsset);
            AssetDatabase.SaveAssets();

            // 保存后按"分类 + 起始帧"重新排序刷新界面
            eventList.Rebuild(true);
            track.Rebuild(true);
            track.RefreshSelection(eventList.SelectedMark);
            Debug.Log($"已保存配置文件：{abilityAsset.name}");
        }

        private void OnBeforeAssemblyReload() => playback.ClearPreviewObjects();
    }
}

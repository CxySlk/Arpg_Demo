using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillEditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private VisualElement root;
    // 技能编辑器配置
    private SkillEditorConfig skillEditorConfig = new SkillEditorConfig();
    #region 初始化
    [MenuItem("技能编辑器/打开技能编辑器")]
    public static void ShowExample()
    {
        SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("技能编辑器窗口");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        // 初始化顶部
        InitTop();
        // 初始化时间轴
        InitTimeShaft();
        // 初始化Content
        InitContent();
    }
    #endregion
    #region Top初始化
    private const string SkillEditorScenePath = "Assets/SkillEditor/Editor/SkillEditorScene.unity";
    private string oldScenePath;
    private Transform previewModelRoot;
    private GameObject previewModel;
    // 演示预制体Field
    private ObjectField previewPrefabField;
    // 演示物体Field
    private ObjectField previewObjectField;
    // 当前技能配置文件
    private SkillClipConfig skillClipConfig;

    // 获取角色展示根物体
    private void GetPreviewModelRoot()
    {
        // 只有演示模型根物体为空及当前场景为编辑器场景才需要操作
        if (previewModelRoot == null && EditorSceneManager.GetActiveScene().path == SkillEditorScenePath)
        {
            // 获取演示模型根物体
            previewModelRoot = GameObject.Find("PreviewModelRoot")?.transform;
            // 演示模型根物体为空，创建一个新的
            if (previewModelRoot == null) previewModelRoot = new GameObject("PreviewModelRoot").transform;
            // 获取演示模型
            if (previewModelRoot.childCount > 0) previewModel = previewModelRoot.GetChild(0).gameObject;
            // 如果角色演示根物体下没有模型，那么就从预制体生成一个
            if (previewModel == null && previewPrefabField.value is GameObject prefab && prefab != null)
                previewModel = Instantiate(prefab, Vector3.zero, Quaternion.identity, previewModelRoot);
            previewObjectField.value = previewModel;
        }
    }

    private void InitTop()
    {
        ObjectField skillClipConfigField;

        // 打开编辑器场景
        Button btnSkillEditorScene = root.Q<Button>(nameof(btnSkillEditorScene));
        btnSkillEditorScene.clicked += () =>
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                oldScenePath = EditorSceneManager.GetActiveScene().path;
                EditorSceneManager.OpenScene(SkillEditorScenePath);
            }
            GetPreviewModelRoot();
        };

        // 打开之前的旧场景
        Button btnOldScene = root.Q<Button>(nameof(btnOldScene));
        btnOldScene.clicked += () =>
        {
            if (oldScenePath != null && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(oldScenePath);
            GetPreviewModelRoot();
        };

        // 显示配置信息
        Button btnConfigInfo = root.Q<Button>(nameof(btnConfigInfo));
        btnConfigInfo.clicked += () =>
        {
            if (skillClipConfig != null)
                Selection.activeObject = skillClipConfig;
        };

        // 演示角色预制体
        previewPrefabField = root.Q<ObjectField>(nameof(previewPrefabField));
        previewPrefabField.objectType = typeof(GameObject);
        previewPrefabField.allowSceneObjects = false;
        previewPrefabField.RegisterValueChangedCallback((value) =>
        {
            // 预制体相同，没必要接下来的操作
            if (value.newValue == null || value.newValue == value.previousValue) return;
            // 删除之前的物体
            DestroyImmediate(previewObjectField.value);
            // 创建新的演示物体
            previewModel = Instantiate(value.newValue as GameObject, Vector3.zero, Quaternion.identity, previewModelRoot);
            previewObjectField.value = previewModel;
        });

        // 演示角色物体
        previewObjectField = root.Q<ObjectField>(nameof(previewObjectField));
        previewObjectField.objectType = typeof(GameObject);
        previewObjectField.allowSceneObjects = true;
        previewObjectField.RegisterValueChangedCallback((value) =>
        {
            if (value.newValue == value.previousValue) return;
            // 删除之前的物体
            DestroyImmediate(value.previousValue);
            previewModel = value.newValue as GameObject;
        });

        // 技能配置文件
        skillClipConfigField = root.Q<ObjectField>(nameof(skillClipConfigField));
        skillClipConfigField.objectType = typeof(SkillClipConfig);
        skillClipConfigField.allowSceneObjects = false;
        skillClipConfigField.RegisterValueChangedCallback((value) =>
        {
            if (value.newValue == value.previousValue) return;
            skillClipConfig = value.newValue as SkillClipConfig;
        });

        GetPreviewModelRoot();
    }
    #endregion
    #region 时间轴绘制
    /// <summary>时间轴容器（对应 UXML 中 name="timeShaft" 的 VisualElement）</summary>
    private IMGUIContainer timeShaft;
    /// <summary>当前选中的帧索引（从 0 开始）</summary>
    private int currentFrameIndex = 0;
    private int CurrentFrameIndex
    {
        get => currentFrameIndex;
        set
        {
            value = Mathf.Max(0, value);
            if (value == currentFrameIndex) return;
            currentFrameIndex = value;
            if (currentFrameIndex > frameCount) FrameCount = currentFrameIndex;
            if (curFrameField != null) curFrameField.value = value;
        }
    }
    private int frameCount = 30;
    private int FrameCount
    {
        get => frameCount;
        set
        {
            value = Mathf.Max(1, value);
            if (value == frameCount) return;
            frameCount = value;
            if (totalFrameField != null) totalFrameField.value = value;
            rightListView.style.width = value * skillEditorConfig.frameUnitWidth;
        }
    }
    // 当前帧索引
    private IntegerField curFrameField;
    private IntegerField totalFrameField;
    /// <summary>
    /// 初始化时间轴：
    /// 1. 绑定 curFrameIndexField / totalFrameIndexField 的值变化回调
    /// 2. 绑定上一帧/下一帧按钮的点击事件
    /// </summary>
    private void InitTimeShaft()
    {
        timeShaft = root.Q<IMGUIContainer>(nameof(timeShaft));
        timeShaft.onGUIHandler = DrawTimeShaft;
        timeShaft.RegisterCallback<WheelEvent>(e =>
        {
            int delta = -(int)e.delta.y;
            skillEditorConfig.frameUnitWidth = Mathf.Clamp(skillEditorConfig.frameUnitWidth + delta, SkillEditorConfig.standFrameUnitWidth, SkillEditorConfig.maxFrameWidthLV * SkillEditorConfig.standFrameUnitWidth);
            timeShaft.MarkDirtyRepaint();
        });
        timeShaft.RegisterCallback<MouseDownEvent>(e =>
        {
            CurrentFrameIndex = GetFrameIndexByLocalPos(e.localMousePosition.x);
        });


        // 当前帧索引
        curFrameField = root.Q<IntegerField>("curFrameIndexField");
        curFrameField.value = currentFrameIndex;
        curFrameField.RegisterValueChangedCallback(e =>
        {
            if (e.newValue == e.previousValue) return;
            currentFrameIndex = e.newValue;
        });
        // 总帧数
        totalFrameField = root.Q<IntegerField>("totalFrameIndexField");
        totalFrameField.value = frameCount;
        totalFrameField.RegisterValueChangedCallback(e =>
        {
            if (e.newValue == currentFrameIndex) return;
            FrameCount = e.newValue;
        });

        root.Q<Button>("btnPreviousFrame").clicked += () => CurrentFrameIndex--;
        root.Q<Button>("btnNextFrame").clicked += () => CurrentFrameIndex++;
    }
    // 画时间轴
    private void DrawTimeShaft()
    {
        if (timeShaft == null) return;

        Rect rect = timeShaft.contentRect;
        // 计算每隔多少帧画一个长帧
        int tickStep;
        tickStep = SkillEditorConfig.maxFrameWidthLV + 1 - (int)(skillEditorConfig.frameUnitWidth / SkillEditorConfig.standFrameUnitWidth);
        tickStep = Mathf.Max(1, tickStep / 2);
        // 起始帧数
        int index = Mathf.CeilToInt((contentOffsetPos / skillEditorConfig.frameUnitWidth));
        // 起始偏移位置
        float startOffset = (skillEditorConfig.frameUnitWidth - contentOffsetPos % skillEditorConfig.frameUnitWidth) % skillEditorConfig.frameUnitWidth;
        Handles.BeginGUI();
        // 画帧
        for (float i = startOffset; i < rect.width; i += skillEditorConfig.frameUnitWidth)
        {
            // 画长帧
            if (index % tickStep == 0)
            {
                Handles.DrawLine(new Vector2(i, rect.height), new Vector2(i, rect.height - 10));
                // 绘制帧数
                string indexStr = index.ToString();
                GUI.Label(new Rect(i - indexStr.Length * 4.5f, 0, 35, 20), indexStr);
            }
            else Handles.DrawLine(new Vector2(i, rect.height), new Vector2(i, rect.height - 5));
            index++;
        }
        // 画选择帧
        float selectFrameOffset = currentFrameIndex * skillEditorConfig.frameUnitWidth - contentOffsetPos;
        if (selectFrameOffset >= 0 && selectFrameOffset <= rect.width)
        {
            Handles.DrawLine(new Vector2(selectFrameOffset, 0), new Vector2(selectFrameOffset, 1000));
        }
        Handles.EndGUI();
    }


    private int GetFrameIndexByLocalPos(float localPos)
    {
        float mousePos = localPos + contentOffsetPos;
        return Mathf.RoundToInt(mousePos / skillEditorConfig.frameUnitWidth);
    }
    #endregion
    #region Content相关
    private ScrollView rightScrollView; // MainContent
    private VisualElement rightListView; // MainContent

    private ScrollView leftScrollView; // 左边滑动条
    private VisualElement leftListView; // MainContent
    private float contentOffsetPos => Mathf.Abs(rightScrollView.scrollOffset.x);
    private void InitContent()
    {
        rightScrollView = root.Q<ScrollView>(nameof(rightScrollView));
        rightListView = rightScrollView.Q<VisualElement>(nameof(rightListView));
        rightScrollView.verticalScroller.valueChanged += e => leftScrollView.verticalScroller.value = e;

        leftScrollView = root.Q<ScrollView>(nameof(leftScrollView));
        leftListView = leftScrollView.Q<VisualElement>(nameof(leftListView));
        leftScrollView.verticalScroller.valueChanged += e => rightScrollView.verticalScroller.value = e;
    }
    #endregion

    private class SkillEditorConfig
    {
        public const float standFrameUnitWidth = 10;
        public const int maxFrameWidthLV = 10;
        public float frameUnitWidth = 10;
    }
}

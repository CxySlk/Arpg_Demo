using GAS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Object;

namespace SkillEditor
{
    /// <summary>
    /// 预览场景/模型/ASC 管理：编辑场景切换、预览模型选择与实例化、预览 ASC 获取。
    /// 预览模型引用由主窗口 [SerializeField] 持有（域重载保留），此处通过主窗口读写。
    /// </summary>
    public class PreviewSceneManager
    {
        private const string SkillEditorScenePath = "Assets/SkillEditor/Editor/SkillEditorScene.unity";
        private const string PreviewRootName = "PreviewModelRoot";

        /// <summary>旧场景路径用 EditorPrefs 记住,域重载/重编译后按钮依然可用</summary>
        private const string OldScenePathKey = "SkillEditor.OldScenePath";

        private readonly SkillEditorUI _ui;
        private readonly SkillEditorWindow _window;

        public Transform PreviewModel => _window.PreviewModel;

        public PreviewSceneManager(SkillEditorUI ui, SkillEditorWindow window)
        {
            _ui = ui;
            _window = window;
        }

        public void Init()
        {
            _ui.btnSkillEditorScene.clicked += OpenSkillEditorScene;
            _ui.btnOldScene.clicked += OpenOldScene;
            _ui.btnConfigInfo.clicked += () => _window.SaveConfig();

            _ui.previewPrefabField.objectType = typeof(GameObject);
            _ui.previewPrefabField.allowSceneObjects = false;
            _ui.previewPrefabField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == null || e.newValue == e.previousValue) return;
                if (_window.PreviewModel != null) DestroyImmediate(_window.PreviewModel.gameObject);
                // 根下面残留的旧模型也一并清掉：预览模型取的是根的第一个子物体，留着旧的会被优先取到
                ClearPreviewModelChildren();
                // 用 PrefabUtility 实例化：场景里存的是“预制体实例”，与预制体的连接保留（ASC 等配置跟着预制体走）
                _window.PreviewModel = ((GameObject)PrefabUtility.InstantiatePrefab(e.newValue as GameObject, _window.PreviewModelRoot)).transform;
                _ui.previewObjectField.value = _window.PreviewModel;
            });

            _ui.previewObjectField.objectType = typeof(GameObject);
            _ui.previewObjectField.allowSceneObjects = true;
            _ui.previewObjectField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == e.previousValue) return;
                if (e.previousValue != null)
                    DestroyImmediate((e.previousValue as Transform).gameObject);
                _window.PreviewModel = e.newValue as Transform;
            });

            GetPreviewModelRoot();
        }

        /// <summary>
        /// 同步预览模型（只在技能编辑器场景里做）：
        /// 根物体取场景里已有的 PreviewModelRoot（找不到就新建一个空根），
        /// 预览模型直接取根下面的**第一个子物体**；根下面没有子物体时预览模型留空，
        /// 不会用预览预制体字段去实例化兜底。
        /// </summary>
        private void GetPreviewModelRoot()
        {
            if (EditorSceneManager.GetActiveScene().path != SkillEditorScenePath) return;

            _window.PreviewModelRoot = GameObject.Find(PreviewRootName)?.transform;
            if (_window.PreviewModelRoot == null)
                _window.PreviewModelRoot = new GameObject(PreviewRootName).transform;

            _window.PreviewModel = _window.PreviewModelRoot.childCount > 0
                ? _window.PreviewModelRoot.GetChild(0)
                : null;

            _ui.previewObjectField.value = _window.PreviewModel;
        }

        /// <summary>清掉预览模型根下面已有的模型（换模型时先清空，避免根下面堆着旧模型）</summary>
        private void ClearPreviewModelChildren()
        {
            if (_window.PreviewModelRoot == null) return;
            for (int i = _window.PreviewModelRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_window.PreviewModelRoot.GetChild(i).gameObject);
        }

        private void OpenSkillEditorScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // 记住来时的场景(排除未保存的空路径与技能编辑器场景自身),重编译后仍能返回
                string currentPath = EditorSceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentPath) && currentPath != SkillEditorScenePath)
                    EditorPrefs.SetString(OldScenePathKey, currentPath);
                EditorSceneManager.OpenScene(SkillEditorScenePath);
            }
            GetPreviewModelRoot();
        }

        private void OpenOldScene()
        {
            string oldScenePath = EditorPrefs.GetString(OldScenePathKey, "");
            if (string.IsNullOrEmpty(oldScenePath)) return;
            // 已经处在目标场景里就不用再切
            if (EditorSceneManager.GetActiveScene().path == oldScenePath) return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(oldScenePath);
            GetPreviewModelRoot();
        }

        /// <summary>获取预览模型上已初始化的 AbilitySystemComponent（用于触发 Cue/GE）</summary>
        public AbilitySystemComponent GetPreviewASC()
        {
            if (_window.PreviewModel == null) return null;
            AbilitySystemComponent asc = _window.PreviewModel.GetComponent<AbilitySystemComponent>();
            if (asc == null)
                Debug.LogWarning("预览模型没有 AbilitySystemComponent，请先在预览模型上配置并初始化 ASC，才能触发 Cue/GE 事件");
            return asc;
        }
    }
}

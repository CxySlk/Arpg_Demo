using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LLKFrame
{
    public class GameRoot : MonoBehaviour
    {
        private static GameRoot instance;
        public static GameRoot Instance => instance;

        /// <summary>GameRoot 的 Transform，可能为空（域重载后实例尚未 Awake 时）</summary>
        public static Transform RootTransform => instance?.transform;
        [LabelText("框架配置"), SerializeField] private LLKFrameSetting frameSetting;
        public static LLKFrameSetting FrameSetting => instance.frameSetting;
        [LabelText("本地化系统"), SerializeField] private LocalizationSystem localizationSystem;
        public static LocalizationSystem LocalizationSystem => instance.localizationSystem;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            // 域重载后 DDOL 场景中的对象会重新 Awake:若已处于 DontDestroyOnLoad 场景,
            // 再调用 DontDestroyOnLoad 会触发 Unity 场景断言(m_GameObjects.find...),需跳过。
            if (gameObject.scene.name != "DontDestroyOnLoad")
                DontDestroyOnLoad(gameObject);
            InitSystems();
        }
        private static void InitSystems()
        {
            SaveSystem.Init();
            AudioSystem.Init();
            LocalizationSystem.Init();
            UISystem.Init();
            MonoSystem.Init();
        }
        #region Editor
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitForEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // 域重载后 instance 为 null，需延迟到下一帧等场景加载完再执行
            EditorApplication.delayCall += () =>
            {
                if (instance == null)
                    instance = FindAnyObjectByType<GameRoot>();
                instance?.InitSystemForEditor();
            };
        }
        private void InitSystemForEditor()
        {
            frameSetting?.InitForEditor();
        }
#endif
        #endregion
    }
}

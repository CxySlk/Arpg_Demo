using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace LLKFrame
{
    public class LocalizationSystem : MonoBehaviour
    {
        public static LocalizationSystem Instance { get; private set; }

        [LabelText("本地化全局配置"), SerializeField] private LocalizationConfig globalConfig;
        public static LocalizationConfig GlobalConfig => Instance.globalConfig;

        [LabelText("语言类型"), SerializeField] private LanguageType languageType;
        public static LanguageType LanguageType
        {
            get => Instance.languageType;
            set
            {
                if (value == Instance.languageType) return;
                Instance.languageType = value;
                languageUpdateEvent?.Invoke(value);
            }
        }

        public static Action<LanguageType> languageUpdateEvent;

        public static void Init()
        {
            Instance = GameRoot.LocalizationSystem;
        }

        /// <summary>
        /// 根据KeyName获取本地化配置内容
        /// </summary>
        public static T GetContent<T>(string keyName) where T : class, ILocalizationData => GetContent<T>(keyName, LanguageType);
        public static T GetContent<T>(string keyName, LanguageType type) where T : class, ILocalizationData
        {
            if (GlobalConfig == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("LLKFrame:本地化的全局配置不能为空");
#endif
                return null;
            }
            return GlobalConfig.GetContent<T>(keyName, type);
        }

        /// <summary>
        /// 注册语言改变事件
        /// </summary>
        public static void RegisterLanguageEvent(Action<LanguageType> action) => languageUpdateEvent += action;
        /// <summary>
        /// 取消注册语言改变事件
        /// </summary>
        public static void UnregisterLanguageEvent(Action<LanguageType> action) => languageUpdateEvent -= action;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Instance == null) return;
            languageUpdateEvent?.Invoke(Instance.languageType);
        }
#endif
    }
}
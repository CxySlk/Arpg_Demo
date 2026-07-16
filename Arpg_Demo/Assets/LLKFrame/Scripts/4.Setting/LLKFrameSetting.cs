using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LLKFrame
{
    [CreateAssetMenu(fileName = "LLKFrameSetting", menuName = "LLKFrame/LLKFrameSetting")]
    public class LLKFrameSetting : ConfigBase
    {
        [DictionaryDrawerSettings(KeyLabel = "窗口名", ValueLabel = "窗口信息")]
        public Dictionary<string, UIWindowData> uiWindowDataDic = new Dictionary<string, UIWindowData>();
#if UNITY_EDITOR
        [Button("重置")]
        public void Reset() => InitForEditor();

        public void InitForEditor() => InitUIDataDic();

        private void InitUIDataDic()
        {
            uiWindowDataDic.Clear();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    UIWindowAttribute[] attributes = type.GetCustomAttributes<UIWindowAttribute>();
                    foreach (UIWindowAttribute attribute in attributes)
                    {
                        UIWindowData data = new UIWindowData(attribute.prefabPath, attribute.isCache, attribute.layer);
                        uiWindowDataDic.Add(attribute.keyName, data);
                    }
                }
            }
        }
#endif
    }
}
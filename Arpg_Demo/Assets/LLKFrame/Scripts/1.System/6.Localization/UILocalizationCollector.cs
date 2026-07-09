using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

namespace LLKFrame
{
    public class UILocalizationCollector : SerializedMonoBehaviour
    {
        #region 内部类
        private class MaskableGraphicData
        {
            public MaskableGraphic component;
            public string keyName;
        }
        #endregion

        [LabelText("本地化局部配置"), SerializeField] private LocalizationConfig localConfig;
        [LabelText("本地化组件映射配置"), SerializeField] private List<MaskableGraphicData> maskableGraphicDataList;

        private void OnEnable()
        {
            LocalizationSystem.RegisterLanguageEvent(OnUpdateLanguageType);
            if (LocalizationSystem.Instance != null)
                OnUpdateLanguageType(LocalizationSystem.LanguageType);
        }

        private void OnDisable() => LocalizationSystem.UnregisterLanguageEvent(OnUpdateLanguageType);

        // 更新语言
        private void OnUpdateLanguageType(LanguageType type)
        {
            if (maskableGraphicDataList == null) return;

            for (int i = 0; i < maskableGraphicDataList.Count; i++)
                UpdateComponentLanguage(maskableGraphicDataList[i], type);
        }
        // 根据本地组件的映射来更新语言
        private void UpdateComponentLanguage(MaskableGraphicData component, LanguageType type)
        {
            // 先从本地配置找
            if (localConfig.config.TryGetValue(component.keyName, out Dictionary<LanguageType, ILocalizationData> dic))
            {
                // 查找配置
                if (dic.TryGetValue(type, out ILocalizationData data))
                {
                    switch (component.component)
                    {
                        case Text text:
                            text.text = (data as LocalizationTextData).content;
                            break;
                        case Image image:
                            image.sprite = (data as LocalizationImageData).content;
                            break;
                    }
                }
#if UNITY_EDITOR
                else LogWarning($"LLKFrame:本地化局部配置中未找到键名{WarningColor(component.keyName)}有该语言类型{WarningColor(type)}的配置");
#endif
                return;
            }
            else // 本地配置没有该则keyName从全局配置查找
            {
                switch (component.component)
                {
                    case Text comp:
                        LocalizationTextData textData = LocalizationSystem.GetContent<LocalizationTextData>(component.keyName, type);
                        if (textData != null) comp.text = textData.content;
#if UNITY_EDITOR
                        else LogWarning($"LLKFrame:本地化全局配置中未找到该键名{WarningColor(component.keyName)}的该语言类型{WarningColor(type)}配置");
#endif
                        break;
                    case TMP_Text comp:
                        LocalizationTextData tmpTextData = LocalizationSystem.GetContent<LocalizationTextData>(component.keyName, type);
                        if (tmpTextData != null) comp.text = tmpTextData.content;
#if UNITY_EDITOR
                        else LogWarning($"LLKFrame:本地化全局配置中未找到该键名{WarningColor(component.keyName)}的该语言类型{WarningColor(type)}配置");
#endif
                        break;
                    case Image comp:
                        LocalizationImageData imgData = LocalizationSystem.GetContent<LocalizationImageData>(component.keyName, type);
                        if (imgData != null) comp.sprite = imgData.content;
#if UNITY_EDITOR
                        else Debug.LogWarning($"LLKFrame:本地化全局配置中未找到该键名{WarningColor(component.keyName)}的该语言类型{WarningColor(type)}配置");
#endif
                        break;
                }
            }
        }
    }
}


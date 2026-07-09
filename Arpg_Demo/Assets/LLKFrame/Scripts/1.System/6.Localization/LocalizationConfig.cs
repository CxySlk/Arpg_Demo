using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
namespace LLKFrame
{
    [CreateAssetMenu(fileName = nameof(LocalizationConfig), menuName = "LLKFrame/" + nameof(LocalizationConfig))]
    public class LocalizationConfig : ConfigBase
    {
        [DictionaryDrawerSettings(KeyLabel = "键名",ValueLabel = "内容配置")]
        public Dictionary<string, Dictionary<LanguageType, ILocalizationData>> config = new Dictionary<string, Dictionary<LanguageType, ILocalizationData>>();

        public T GetContent<T>(string keyName, LanguageType type) where T : class, ILocalizationData
        {
            ILocalizationData data = null;
            if (config.TryGetValue(keyName, out Dictionary<LanguageType, ILocalizationData> dic))
                dic.TryGetValue(type, out data);
            return data as T;
        }
    }
}

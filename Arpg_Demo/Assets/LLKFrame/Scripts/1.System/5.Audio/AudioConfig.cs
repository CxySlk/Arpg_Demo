using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace LLKFrame
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "LLKFrame/AudioConfig")]
    public class AudioConfig : ConfigBase
    {
        [LabelText("AudioClip配置"), DictionaryDrawerSettings(KeyLabel = "KeyName", ValueLabel = "AudioClip"), SerializeField]
        private Dictionary<string, AudioClip> configDic = new Dictionary<string, AudioClip>();
        public Dictionary<string, AudioClip> ConfigDic => configDic;
    }
}

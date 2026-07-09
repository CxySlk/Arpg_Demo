using LLKFrame;
using UnityEngine;

namespace LLKFrame
{
    public interface ILocalizationData { }

    public class LocalizationTextData : ILocalizationData
    {
        public string content;
    }
    public class LocalizationImageData : ILocalizationData
    {
        public Sprite content;
    }
}

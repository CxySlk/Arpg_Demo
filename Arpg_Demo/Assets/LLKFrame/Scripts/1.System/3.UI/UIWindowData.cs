using Sirenix.OdinInspector;
using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// 单个 UI 窗口的配置数据。
    /// LLKFrameSetting.InitUIDataDic 通过反射扫描所有带 UIWindowAttribute 的类自动生成
    /// </summary>
    public class UIWindowData
    {
        [LabelText("预制体路径")] public readonly string prefabPath;
        [LabelText("是否缓存")] public readonly bool isCache;
        [LabelText("UI层级")] public readonly int layer;
        [LabelText("窗口实例"), ReadOnly] public GameObject instance;

        /// <param name="prefabPath">Addressables 预制体路径</param>
        /// <param name="isCache">关闭时是否缓存实例</param>
        /// <param name="layer">所属层级索引</param>
        public UIWindowData(string prefabPath, bool isCache, int layer)
        {
            this.prefabPath = prefabPath;
            this.isCache = isCache;
            this.layer = layer;
            instance = null;
        }
    }
}

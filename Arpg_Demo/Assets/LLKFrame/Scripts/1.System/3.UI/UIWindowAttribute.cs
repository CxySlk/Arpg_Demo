using System;

namespace LLKFrame
{
    /// <summary>
    /// 标记 UI 窗口类的特性。<br/>
    /// 挂载到窗口类上后，LLKFrameSetting 会在初始化时通过反射自动收集配置到 uiWindowDataDic
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class UIWindowAttribute : Attribute
    {
        /// <summary>预制体的 Addressables 加载路径</summary>
        public readonly string prefabPath;
        /// <summary>窗口唯一标识 key</summary>
        public readonly string keyName;
        /// <summary>关闭时是否缓存（true=失活保留，false=直接销毁）</summary>
        public readonly bool isCache;
        /// <summary>窗口所属层级索引</summary>
        public readonly int layer;

        /// <param name=""prefabPath"">预制体路径</param>
        /// <param name=""keyName"">窗口 key</param>
        /// <param name=""isCache"">是否缓存</param>
        /// <param name=""layer"">层级</param>
        public UIWindowAttribute(string prefabPath, string keyName, bool isCache, int layer)
        {
            this.prefabPath = prefabPath;
            this.keyName = keyName;
            this.isCache = isCache;
            this.layer = layer;
        }
    }
}

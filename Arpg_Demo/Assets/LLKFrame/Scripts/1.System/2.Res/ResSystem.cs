using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LLKFrame
{
    /// <summary>
    /// 资源加载系统，基于 Addressables 封装
    /// 同步加载资源并返回实例或引用，同时提供资源释放接口
    /// </summary>
    public static class ResSystem
    {
        /// <summary>
        /// 同步加载资源
        /// <br/>- 若资源为 GameObject，先加载预制体再 Instantiate 新实例并保留原名
        /// <br/>- 其他资源类型（材质、贴图、配置等）直接返回加载后的引用
        /// </summary>
        /// <typeparam name="TObj">资源类型，须继承 UnityEngine.Object</typeparam>
        /// <param name="key">Addressables 地址</param>
        /// <returns>加载到的资源（GameObject 时返回新实例化的对象）</returns>
        public static TObj LoadAsset<TObj>(string key) where TObj : Object
        {
            TObj res = Addressables.LoadAssetAsync<TObj>(key).WaitForCompletion();
            if (res is GameObject obj)
            {
                string objName = obj.name;
                obj = GameObject.Instantiate(obj);
                obj.name = objName;
                return obj as TObj;
            }
            return res;
        }

        /// <summary>
        /// 释放指定资源，减少 Addressables 引用计数
        /// </summary>
        /// <typeparam name="TObj">资源类型</typeparam>
        /// <param name="obj">待释放的资源引用</param>
        /// <remarks>场景切换或不再需要该资源时调用，避免内存泄漏</remarks>
        public static void Release<TObj>(TObj obj) => Addressables.Release(obj);
    }
}

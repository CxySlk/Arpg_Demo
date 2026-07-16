using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using static LLKFrame.LogSystem;
#endif

namespace LLKFrame
{
    /// <summary>
    /// 资源加载系统，基于 Addressables 封装
    /// 同步加载资源并返回实例或引用，同时提供资源释放接口
    /// </summary>
    public static class ResSystem
    {
        #region 内部类
        private abstract class LoadResItem
        {
            public int count;
            public abstract void ReleaseAsset();
        }
        private class LoadResItem<TObj> : LoadResItem where TObj : UnityEngine.Object
        {
            public TObj resItem;

            public void Init(TObj resItem)
            {
                this.resItem = resItem;
                count = 0;
            }

            public TObj LoadAsset()
            {
                count++;
                if (resItem is GameObject)
                {
                    TObj obj = GameObject.Instantiate(resItem);
                    obj.name = resItem.name;
                    return obj;
                }
                else return resItem;
            }

            public override void ReleaseAsset()
            {
                count--;
                if (count <= 0)
                {
                    Addressables.Release(resItem);
                    resItem = null;
                }
            }
        }
        #endregion
        /// <summary>
        /// <资源名，<资源类型，>>
        /// </summary>
        private static Dictionary<string, Dictionary<Type, LoadResItem>> resCacheDic = new Dictionary<string, Dictionary<Type, LoadResItem>>();

        /// <summary>
        /// 同步加载资源
        /// <br/>- 若资源为 GameObject，先加载预制体再 Instantiate 新实例并保留原名
        /// <br/>- 其他资源类型（材质、贴图、配置等）直接返回加载后的引用
        /// </summary>
        /// <typeparam name="TObj">资源类型，须继承 UnityEngine.Object</typeparam>
        /// <param name="keyName">Addressables 地址</param>
        /// <returns>加载到的资源（GameObject 时返回新实例化的对象）</returns>
        public static TObj LoadAsset<TObj>(string keyName) where TObj : UnityEngine.Object
        {
            Type type = typeof(TObj);
            TObj res;

            // 加载过该key的缓存
            if (resCacheDic.TryGetValue(keyName, out Dictionary<Type, LoadResItem> cacheDic))
            {
                // 缓存中有该TObj类型的缓存
                if (cacheDic.TryGetValue(type, out LoadResItem resItem))
                    res = (resItem as LoadResItem<TObj>).LoadAsset();
                // 缓存中没有该TObj类型的缓存，调用Addressables加载
                else
                {
                    // 加载资源
                    LoadResItem<TObj> loadResItem = PoolSystem.GetObject<LoadResItem<TObj>>();
                    res = Addressables.LoadAssetAsync<TObj>(keyName).WaitForCompletion();
                    // 把缓存加入到字典中
                    loadResItem.Init(res);
                    cacheDic.Add(type, loadResItem);
                    // 获取真实的资源
                    res = loadResItem.LoadAsset();
                }
            }
            // 未加载过该key的缓存
            else
            {
                // 加载资源
                res = Addressables.LoadAssetAsync<TObj>(keyName).WaitForCompletion();
                // 初始化资源加载数据
                LoadResItem<TObj> resItem = PoolSystem.GetOrNewObject<LoadResItem<TObj>>();
                resItem.Init(res);
                // 获取真正的资源
                res = resItem.LoadAsset();
                // 加入缓存数据
                cacheDic = new Dictionary<Type, LoadResItem>();
                cacheDic.Add(type, resItem);
                resCacheDic.Add(keyName, cacheDic);
            }
            return res;
        }

        public static void Release(string keyName, Type type)
        {
            if (resCacheDic.TryGetValue(keyName, out Dictionary<Type, LoadResItem> cacheDic) &&
                cacheDic.TryGetValue(type, out LoadResItem resItem))
            {
                resItem.ReleaseAsset();
                if (resItem.count <= 0)
                {
                    cacheDic.Remove(type);
                    resItem.PushObject();
                }
            }
#if UNITY_EDITOR
            else LogError("Addressables资源释放出错，Type：", ErrorColor(type), "，KeyName：" + ErrorColor(keyName));
#endif
        }

        /// <summary>
        /// 释放指定资源，减少 Addressables 引用计数
        /// </summary>
        /// <typeparam name="TObj">资源类型</typeparam>
        /// <param name="keyName">待释放的资源引用</param>
        /// <remarks>场景切换或不再需要该资源时调用，避免内存泄漏</remarks>
        public static void Release<TObj>(string keyName) where TObj : UnityEngine.Object => Release(keyName, typeof(TObj));

    }
}

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LLKFrame
{
    /// <summary>
    /// UI 窗口管理系统。
    /// 按层级管理窗口的显示、关闭、缓存与销毁，支持 keyName 或泛型类型打开/关闭窗口。
    /// </summary>
    public class UISystem : SerializedMonoBehaviour
    {
        #region UI_Layer 内部类
        /// <summary>
        /// 单个 UI 层级，管理该层级内窗口的排序、显隐与遮罩
        /// </summary>
        private class UI_Layer
        {
            [LabelText("层级根节点"), SerializeField] private Transform layerRoot;
            [LabelText("遮罩"), SerializeField] private Image layerMask;
            [LabelText("是否开启遮罩"), SerializeField, OnValueChanged("IsMaskValueChanged")] private bool isMask;

            /// <summary>遮罩开关：开启时拦截下层鼠标事件</summary>
            public bool IsMask
            {
                get => isMask;
                set
                {
                    if (value == isMask) return;
                    isMask = value;
                }
            }
#if UNITY_EDITOR
            private void IsMaskValueChanged() => layerMask.raycastTarget = isMask;
#endif
            private int count;
            /// <summary>当前层级内的窗口数量</summary>
            public int Count
            {
                get => count;
                set
                {
                    if (value == count) return;
                    count = value;
                    IsMask = count != 0;
                }
            }

            /// <summary>将窗口挂载到本层级并置顶显示</summary>
            public void ShowWindow(Transform window)
            {
                window.SetParent(layerRoot, false);
                window.gameObject.SetActive(true);
                window.SetAsLastSibling();
                layerMask.transform.SetSiblingIndex(layerRoot.childCount > 2 ? layerRoot.childCount - 2 : 0);
                Count++;
            }

            /// <summary>关闭窗口（失活保留，不销毁）</summary>
            public void CloseWindow(Transform window)
            {
                window.gameObject.SetActive(false);
                window.SetAsFirstSibling();
                layerMask.transform.SetSiblingIndex(layerRoot.childCount > 2 ? layerRoot.childCount - 2 : 0);
                Count--;
            }

            /// <summary>销毁窗口</summary>
            public void DestroyWindow(Transform window)
            {
                Object.Destroy(window.gameObject);
                Count--;
                layerMask.transform.SetSiblingIndex(layerRoot.childCount > 2 ? layerRoot.childCount - 2 : 0);
            }
        }
        #endregion
        #region 变量定义
        [SerializeField] private UI_Layer[] uiLayers;
        private static UISystem instance;

        /// <summary>窗口配置字典，来自 GameRoot.FrameSetting</summary>
        private static Dictionary<string, UIWindowData> UiWindowDataDic => GameRoot.FrameSetting.uiWindowDataDic;

        /// <summary>已实例化的窗口缓存字典</summary>
        private static Dictionary<string, UI_WindowBase> uiWindowDic = new Dictionary<string, UI_WindowBase>();
        #endregion

        public static void Init() => instance = GameRoot.Instance.GetComponentInChildren<UISystem>();

        // ==============================
        //  Show 方法族
        // ==============================

        /// <summary>
        /// 通过 keyName 显示窗口
        /// </summary>
        /// <param name=""keyName"">窗口标识</param>
        /// <param name=""layer"">目标层级，-1 表示使用默认层级</param>
        /// <returns>窗口基类引用</returns>
        public static UI_WindowBase Show(string keyName, int layer = -1)
        {
            if (!UiWindowDataDic.TryGetValue(keyName, out UIWindowData windowData))
                Debug.LogError($"LLKFrame：未找到{keyName}的Window配置");

            // 之前有过缓存 → 直接复用
            if (uiWindowDic.TryGetValue(keyName, out UI_WindowBase window))
            {
                layer = layer == -1 ? window.layer : layer;
                // 切换到新的层级
                if (window.layer != layer)
                {
                    instance.uiLayers[window.layer].CloseWindow(window.transform);
                    instance.uiLayers[layer].ShowWindow(window.transform);
                }
                window.Show(layer);
            }
            else
            {
                // 首次实例化
                GameObject obj = ResSystem.LoadAsset<GameObject>(windowData.prefabPath);
                windowData.instance = obj;
                window = obj.GetComponent<UI_WindowBase>();
                window.Init();
                layer = layer == -1 ? windowData.layer : layer;
                window.Show(layer);
                uiWindowDic.Add(keyName, window);
            }
            // 将窗口显示到对应的层级上
            instance.uiLayers[layer].ShowWindow(window.transform);
            return window;
        }

        /// <summary>通过 keyName 显示窗口并强转为指定类型</summary>
        public static TWindow Show<TWindow>(string keyName, int layer = -1) where TWindow : UI_WindowBase
        {
            return Show(keyName, layer) as TWindow;
        }

        /// <summary>通过泛型类型名作为 key 显示窗口</summary>
        public static TWindow Show<TWindow>(int layer = -1) where TWindow : UI_WindowBase
        {
            return Show<TWindow>(typeof(TWindow).Name, layer);
        }

        // ==============================
        //  Close 方法族
        // ==============================

        /// <summary>
        /// 通过 keyName 关闭窗口。
        /// <br/>- isCache = true：失活保留到缓存中
        /// <br/>- isCache = false：直接销毁并从缓存移除
        /// </summary>
        /// <param name=""keyName"">窗口标识</param>
        public static void Close(string keyName)
        {
            if (!uiWindowDic.TryGetValue(keyName, out UI_WindowBase window))
                return;

            if (!UiWindowDataDic.TryGetValue(keyName, out UIWindowData windowData))
                Debug.LogError($"LLKFrame：未找到{keyName}的Window配置");

            window.Close();
            // 可以缓存的窗口
            if (windowData.isCache)
                instance.uiLayers[window.layer].CloseWindow(window.transform);
            // 无法缓存
            else
            {
                // 释放Addressables资源
                ResSystem.Release<GameObject>(windowData.prefabPath);

                windowData.instance = null;
                instance.uiLayers[window.layer].DestroyWindow(window.transform);
                uiWindowDic.Remove(keyName);
            }
        }

        /// <summary>通过泛型类型名关闭窗口</summary>
        public static void Close<TWindow>() where TWindow : UI_WindowBase
        {
            Close(typeof(TWindow).Name);
        }
    }
}

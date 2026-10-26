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

            [LabelText("是否开启遮罩"), SerializeField, OnValueChanged("IsMaskValueChanged")]
            private bool isMask;

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
                RefreshMaskIndex();
                Count++;
            }

            /// <summary>关闭记账：让出层级计数（是否销毁、何时销毁由调用方决定）。
            /// 不重排 sibling，避免正在播退出动画的窗口被同层窗口挡住。</summary>
            public void CloseWindow(Transform window)
            {
                RefreshMaskIndex();
                Count--;
            }

            /// <summary>销毁窗口（层级计数已由 <see cref="CloseWindow"/> 扣过）</summary>
            public void DestroyWindow(Transform window)
            {
                Destroy(window.gameObject);
                RefreshMaskIndex();
            }

            /// <summary>遮罩始终保持在层级根的倒数第二位</summary>
            private void RefreshMaskIndex() =>
                layerMask.transform.SetSiblingIndex(layerRoot.childCount > 2 ? layerRoot.childCount - 2 : 0);
        }

        #endregion

        #region 变量定义

        [SerializeField] private UI_Layer[] uiLayers;
        private static UISystem instance;

        /// <summary>UISystem 单例实例（供外部访问拖拽层等）</summary>
        public static UISystem Instance => instance;

        /// <summary>窗口配置字典，来自 GameRoot.FrameSetting</summary>
        private static Dictionary<string, UIWindowData> UiWindowDataDic => GameRoot.FrameSetting.uiWindowDataDic;

        /// <summary>已实例化的窗口缓存字典</summary>
        private static Dictionary<string, UI_WindowBase> uiWindowDic = new Dictionary<string, UI_WindowBase>();

        #endregion

        [LabelText("拖拽层"), SerializeField] private Transform _dragLayer;
        public Transform DragLayer => _dragLayer;

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
        /// <br/>- isCache = false：关闭表现播完后销毁并从缓存移除
        /// </summary>
        /// <param name=""keyName"">窗口标识</param>
        public static void Close(string keyName)
        {
            if (!uiWindowDic.TryGetValue(keyName, out UI_WindowBase window))
                return;

            if (!UiWindowDataDic.TryGetValue(keyName, out UIWindowData windowData))
                Debug.LogError($"LLKFrame：未找到{keyName}的Window配置");

            // 可以缓存的窗口：关闭表现照常播，播完由窗口自己失活，实例留在缓存中
            if (windowData.isCache)
            {
                window.Close();
                instance.uiLayers[window.layer].CloseWindow(window.transform);
                return;
            }

            // 无法缓存：先记账（关闭表现期间窗口仍然可见），等表现播完再真正销毁，避免销毁打断关闭动画
            instance.uiLayers[window.layer].CloseWindow(window.transform);
            window.Close(() =>
            {
                // 关闭表现期间窗口又被 Show 出来 → 取消本次销毁
                if (window.IsShow) return;

                windowData.instance = null;
                instance.uiLayers[window.layer].DestroyWindow(window.transform);
                uiWindowDic.Remove(keyName);
            });
        }

        /// <summary>通过泛型类型名关闭窗口</summary>
        public static void Close<TWindow>() where TWindow : UI_WindowBase
        {
            Close(typeof(TWindow).Name);
        }

        public static TWindow GetWindow<TWindow>(string keyName) where TWindow : UI_WindowBase
        {
            uiWindowDic.TryGetValue(keyName, out UI_WindowBase window);
            return window as TWindow;
        }        
        public static TWindow GetWindow<TWindow>() where TWindow : UI_WindowBase
        {
            return  GetWindow<TWindow>(typeof(TWindow).Name);
        }

        /// <summary>
        /// 关闭所有的窗口，即使是缓存中的
        /// </summary>
        public static void CloseAll()
        {
            if (instance == null)
                return;

            List<string> keys = new(uiWindowDic.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                if (!uiWindowDic.TryGetValue(keys[i], out UI_WindowBase window) || window == null)
                    continue;

                if (window.IsShow)
                    Close(keys[i]);
                else if (UiWindowDataDic.TryGetValue(keys[i], out UIWindowData data) && data.isCache)
                    window.gameObject.SetActive(false);
            }
        }
    }
}
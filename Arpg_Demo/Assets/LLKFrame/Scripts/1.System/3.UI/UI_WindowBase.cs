using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// UI 窗口抽象基类，所有具体窗口脚本都应继承此类
    /// </summary>
    public abstract class UI_WindowBase : SerializedMonoBehaviour
    {
        //private string 
        /// <summary>当前所在层级索引</summary>
        public int layer { get; private set; }

        public bool IsShow { get; protected set; } = false;

        /// <summary>本物体上启用的显隐表现效果（初始化时收集；只查本物体，不含子物体/父物体）</summary>
        private readonly List<IUIWindowEffect> _windowEffects = new();

        /// <summary>表现世代号：Show/Close 各自自增，用来作废尚未播完的旧回调</summary>
        private int _effectToken;

        /// <summary>还没播完弹出的效果数量（归零才真正隐藏窗口）</summary>
        private int _exitPendingCount;

        /// <summary>全部效果弹出播完后的收尾回调（由 UISystem 传入，用于延迟销毁不缓存的窗口）</summary>
        private Action _onExitCompleted;

        /// <summary>窗口初始化，首次实例化时由 UISystem.Show 调用</summary>
        /// <remarks>子类覆写时必须调用 base.Init()，否则挂载的显隐表现效果不会被收集。</remarks>
        public virtual void Init()
        {
            _windowEffects.Clear();
            // GetComponents(而非 GetComponentsInChildren)：只取本物体上的效果组件
            foreach (IUIWindowEffect effect in GetComponents<IUIWindowEffect>())
            {
                if (effect is Behaviour behaviour && behaviour.enabled)
                    _windowEffects.Add(effect);
            }
        }

        /// <summary>显示窗口</summary>
        public virtual void Show(int layer)
        {
            this.layer = layer;
            CancelPendingExit(); // 作废上一次关闭遗留的收尾回调

            gameObject.SetActive(true);
            IsShow = true;
            OnShow();
            PlayEnterEffects();
        }

        protected virtual void OnShow()
        {
        }


        /// <summary>关闭窗口</summary>
        /// <param name="onClosed">
        /// 整个关闭表现播完（窗口已隐藏）后回调；没挂效果时同帧同步回调。
        /// UISystem 用它延迟销毁"不缓存"的窗口，避免销毁打断关闭动画。
        /// </param>
        public virtual void Close(Action onClosed = null)
        {
            IsShow = false;
            OnClose();
            PlayExitEffects(onClosed);
        }

        /// <summary>窗口关闭时调用，子类在此清理资源（取消事件订阅等）</summary>
        protected virtual void OnClose()
        {
        }

        protected virtual void OnDestroy()
        {
            ResSystem.Release<GameObject>(name);
        }

        #region 显隐表现（IUIWindowEffect）

        /// <summary>作废尚未播完的关闭表现（其收尾回调不再执行）</summary>
        private void CancelPendingExit()
        {
            _effectToken++;
            _exitPendingCount = 0;
            _onExitCompleted = null;
        }

        /// <summary>驱动本物体上所有效果的弹入</summary>
        private void PlayEnterEffects()
        {
            foreach (IUIWindowEffect effect in _windowEffects)
                effect.PlayEnter();
        }

        /// <summary>驱动本物体上所有效果的弹出；全部播完后隐藏窗口并执行收尾回调。</summary>
        private void PlayExitEffects(Action onClosed)
        {
            if (_windowEffects.Count == 0)
            {
                // 没挂效果：同帧隐藏，与引入表现效果之前的行为一致
                gameObject.SetActive(false);
                onClosed?.Invoke();
                return;
            }

            int token = ++_effectToken;
            _exitPendingCount = _windowEffects.Count;
            _onExitCompleted = onClosed;

            foreach (IUIWindowEffect effect in _windowEffects)
                effect.PlayExit(() => OnEffectExitCompleted(token));
        }

        /// <summary>单个效果的弹出播完；全部播完后隐藏窗口并执行收尾回调。</summary>
        private void OnEffectExitCompleted(int token)
        {
            // 已被新的 Show/Close 作废，或同一效果重复回调
            if (token != _effectToken || _exitPendingCount <= 0) return;
            if (--_exitPendingCount > 0) return;

            gameObject.SetActive(false);
            Action completed = _onExitCompleted;
            _onExitCompleted = null;
            completed?.Invoke();
        }

        #endregion
    }
}

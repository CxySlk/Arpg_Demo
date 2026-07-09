using System;
using System.Collections.Generic;

namespace LLKFrame
{
    /// <summary>
    /// 事件中心，基于字符串 key 的观察者模式实现，支持 0~3 个参数的泛型事件
    /// </summary>
    public static class EventCenter
    {
        #region 事件数据类
        /// <summary>
        /// 事件数据接口，统一提供 ClearAll 方法以便清理
        /// </summary>
        private interface IEventData { public void Clear(); }

        /// <summary>
        /// 无参事件数据
        /// </summary>
        private class EventData : IEventData
        {
            public Action action;
            public EventData(Action action) => this.action = action;
            /// <summary>注册事件（注意：this.action 才是字段，入参 action 会遮蔽字段）</summary>
            public void AddEvent(Action action) => action += action;
            /// <summary>注销事件</summary>
            public void RemoveEvent(Action action) => action -= action;
            /// <summary>触发事件</summary>
            public void TriggerEvent() => action?.Invoke();
            public void Clear() => action = null;
        }
        /// <summary>
        /// 1 个泛型参数的事件数据
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        private class EventData<T> : IEventData
        {
            public Action<T> action;
            public EventData(Action<T> action) => this.action = action;
            public void AddEvent(Action<T> action) => action += action;
            public void RemoveEvent(Action<T> action) => action -= action;
            public void TriggerEvent(T arg) => action?.Invoke(arg);
            public void Clear() => action = null;
        }
        /// <summary>
        /// 2 个泛型参数的事件数据
        /// </summary>
        /// <typeparam name="T1">第 1 个参数类型</typeparam>
        /// <typeparam name="T2">第 2 个参数类型</typeparam>
        private class EventData<T1, T2> : IEventData
        {
            public Action<T1, T2> action;
            public EventData(Action<T1, T2> action) => this.action = action;
            public void AddEvent(Action<T1, T2> action) => action += action;
            public void RemoveEvent(Action<T1, T2> action) => action -= action;
            public void TriggerEvent(T1 arg1, T2 arg2) => action?.Invoke(arg1, arg2);
            public void Clear() => action = null;
        }
        /// <summary>
        /// 3 个泛型参数的事件数据
        /// </summary>
        /// <typeparam name="T1">第 1 个参数类型</typeparam>
        /// <typeparam name="T2">第 2 个参数类型</typeparam>
        /// <typeparam name="T3">第 3 个参数类型</typeparam>
        private class EventData<T1, T2, T3> : IEventData
        {
            public Action<T1, T2, T3> action;
            public EventData(Action<T1, T2, T3> action) => this.action = action;
            public void AddEvent(Action<T1, T2, T3> action) => action += action;
            public void RemoveEvent(Action<T1, T2, T3> action) => action -= action;
            public void TriggerEventData(T1 arg1, T2 arg2, T3 arg3) => action?.Invoke(arg1, arg2, arg3);
            public void Clear() => action = null;
        }
        #endregion
        /// <summary>
        /// 事件字典，key 为事件名，value 为对应的事件数据对象
        /// </summary>
        private static Dictionary<string, IEventData> eventDic;
        public static void Init() => eventDic = new Dictionary<string, IEventData>();

        #region 无参事件
        /// <summary>
        /// 注册无参事件监听
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        /// <param name="action">回调方法</param>
        public static void AddEventListener(string eventKey, Action action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData).AddEvent(action);
            else
                eventDic.Add(eventKey, new EventData(action));
        }

        /// <summary>
        /// 注销无参事件监听
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        /// <param name="action">回调方法</param>
        public static void RemoveEventListener(string eventKey, Action action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData).RemoveEvent(action);
        }
        /// <summary>
        /// 触发无参事件
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        public static void TriggerEvent(string eventKey)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData).TriggerEvent();
        }
        #endregion
        #region 1个参数事件
        /// <summary>
        /// 注册带 1 个参数的事件监听
        /// </summary>
        public static void AddEventListener<T>(string eventKey, Action<T> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>).AddEvent(action);
            else
                eventDic.Add(eventKey, new EventData<T>(action));
        }

        /// <summary>
        /// 注销带 1 个参数的事件监听
        /// </summary>
        public static void RemoveEventListener<T>(string eventKey, Action<T> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>).RemoveEvent(action);
        }
        /// <summary>
        /// 触发带 1 个参数的事件
        /// </summary>
        public static void TriggerEvent<T>(string eventKey, T arg)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>).TriggerEvent(arg);
        }
        #endregion
        #region 2个参数事件
        /// <summary>
        /// 注册带 2 个参数的事件监听
        /// </summary>
        public static void AddEventListener<T1, T2>(string eventKey, Action<T1, T2> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>).AddEvent(action);
            else
                eventDic.Add(eventKey, new EventData<T1, T2>(action));
        }

        /// <summary>
        /// 注销带 2 个参数的事件监听
        /// </summary>
        public static void RemoveEventListener<T1, T2>(string eventKey, Action<T1, T2> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>).RemoveEvent(action);
        }
        /// <summary>
        /// 触发带 2 个参数的事件
        /// </summary>
        public static void TriggerEvent<T1, T2>(string eventKey, T1 arg1, T2 arg2)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>).TriggerEvent(arg1, arg2);
        }
        #endregion
        #region 3个参数事件
        /// <summary>
        /// 注册带 3 个参数的事件监听
        /// </summary>
        public static void AddEventListener<T1, T2, T3>(string eventKey, Action<T1, T2, T3> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>).AddEvent(action);
            else
                eventDic.Add(eventKey, new EventData<T1, T2, T3>(action));
        }

        /// <summary>
        /// 注销带 3 个参数的事件监听
        /// </summary>
        public static void RemoveEventListener<T1, T2, T3>(string eventKey, Action<T1, T2, T3> action)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>).RemoveEvent(action);
        }
        /// <summary>
        /// 触发带 3 个参数的事件
        /// </summary>
        public static void TriggerEvent<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>).TriggerEventData(arg1, arg2, arg3);
        }
        #endregion

        #region 清理
        /// <summary>
        /// 清理指定事件的回调列表
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        public static void Clear(string eventKey)
        {
            if (eventDic.TryGetValue(eventKey, out IEventData eventData))
                eventData.Clear();
        }
        /// <summary>
        /// 清理所有事件
        /// </summary>
        public static void ClearAll()
        {
            foreach (IEventData value in eventDic.Values)
                value.Clear();
            eventDic.Clear();
        }
        #endregion
    }
}

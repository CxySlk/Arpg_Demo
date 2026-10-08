using System;
using System.Collections.Generic;

namespace LLKFrame
{
    /// <summary>
    /// 事件中心，基于 EventKey 的观察者模式实现，支持 0~3 个参数的泛型事件
    /// </summary>
    public class EventCenter
    {
        public static readonly EventCenter Global = new EventCenter();

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
            public void Subscribe(Action action) => this.action += action;
            /// <summary>注销事件</summary>
            public void UnSubscribe(Action action) => this.action -= action;
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
            public void Subscribe(Action<T> action) => this.action += action;
            public void UnSubscribe(Action<T> action) => this.action -= action;
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
            public void Subscribe(Action<T1, T2> action) => this.action += action;
            public void UnSubscribe(Action<T1, T2> action) => this.action -= action;
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
            public void Subscribe(Action<T1, T2, T3> action) => this.action += action;
            public void UnSubscribe(Action<T1, T2, T3> action) => this.action -= action;
            public void Publish(T1 arg1, T2 arg2, T3 arg3) => action?.Invoke(arg1, arg2, arg3);
            public void Clear() => action = null;
        }
        #endregion
        #region 事件类型字典的比较器
        private class EventDicEqualityComparer : IEqualityComparer<EventKeyBase>
        {
            public bool Equals(EventKeyBase x, EventKeyBase y) => x.Equals(y);
            public int GetHashCode(EventKeyBase arg) => arg.GetHashCode();
        }
        #endregion
        /// <summary>
        /// 事件字典，key 为事件名，value 为对应的事件数据对象
        /// </summary>
        private readonly Dictionary<EventKeyBase, IEventData> _eventDic = new Dictionary<EventKeyBase, IEventData>();

        #region 无参事件
        /// <summary>
        /// 注册无参事件监听
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        /// <param name="action">回调方法</param>
        public void Subscribe(EventKey eventKey, Action action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData)!.Subscribe(action);
            else
                _eventDic.Add(eventKey, new EventData(action));
        }

        /// <summary>
        /// 注销无参事件监听
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        /// <param name="action">回调方法</param>
        public void UnSubscribe(EventKey eventKey, Action action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData)!.UnSubscribe(action);
        }
        /// <summary>
        /// 触发无参事件
        /// </summary>
        /// <param name="eventKey">事件标识</param>
        public void Publish(EventKey eventKey)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData)!.TriggerEvent();
        }
        #endregion
        #region 1个参数事件
        /// <summary>
        /// 注册带 1 个参数的事件监听
        /// </summary>
        public void Subscribe<T>(EventKey<T> eventKey, Action<T> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>)!.Subscribe(action);
            else
                _eventDic.Add(eventKey, new EventData<T>(action));
        }

        /// <summary>
        /// 注销带 1 个参数的事件监听
        /// </summary>
        public void UnSubscribe<T>(EventKey<T> eventKey, Action<T> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>)!.UnSubscribe(action);
        }
        /// <summary>
        /// 触发带 1 个参数的事件
        /// </summary>
        public void Publish<T>(EventKey<T> eventKey, T arg)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T>)!.TriggerEvent(arg);
        }
        #endregion
        #region 2个参数事件
        /// <summary>
        /// 注册带 2 个参数的事件监听
        /// </summary>
        public void Subscribe<T1, T2>(EventKey<T1, T2> eventKey, Action<T1, T2> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>)!.Subscribe(action);
            else
                _eventDic.Add(eventKey, new EventData<T1, T2>(action));
        }

        /// <summary>
        /// 注销带 2 个参数的事件监听
        /// </summary>
        public void UnSubscribe<T1, T2>(EventKey<T1, T2> eventKey, Action<T1, T2> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>)!.UnSubscribe(action);
        }
        /// <summary>
        /// 触发带 2 个参数的事件
        /// </summary>
        public void Publish<T1, T2>(EventKey<T1, T2> eventKey, T1 arg1, T2 arg2)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2>)!.TriggerEvent(arg1, arg2);
        }
        #endregion
        #region 3个参数事件
        /// <summary>
        /// 注册带 3 个参数的事件监听
        /// </summary>
        public void Subscribe<T1, T2, T3>(EventKey<T1, T2, T3> eventKey, Action<T1, T2, T3> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>)!.Subscribe(action);
            else
                _eventDic.Add(eventKey, new EventData<T1, T2, T3>(action));
        }

        /// <summary>
        /// 注销带 3 个参数的事件监听
        /// </summary>
        public void UnSubscribe<T1, T2, T3>(EventKey<T1, T2, T3> eventKey, Action<T1, T2, T3> action)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>)!.UnSubscribe(action);
        }
        /// <summary>
        /// 触发带 3 个参数的事件
        /// </summary>
        public void Publish<T1, T2, T3>(EventKey<T1, T2, T3> eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                (eventData as EventData<T1, T2, T3>)!.Publish(arg1, arg2, arg3);
        }
        #endregion
        #region 清理
        /// <summary>
        /// 清理无参事件
        /// </summary>
        public void Clear(EventKey eventKey)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                eventData.Clear();
        }
        /// <summary>
        /// 清理带 1 个参数的事件
        /// </summary>
        public void Clear<T>(EventKey<T> eventKey)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                eventData.Clear();
        }
        /// <summary>
        /// 清理带 2 个参数的事件
        /// </summary>
        public void Clear<T1, T2>(EventKey<T1, T2> eventKey)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                eventData.Clear();
        }
        /// <summary>
        /// 清理带 3 个参数的事件
        /// </summary>
        public void Clear<T1, T2, T3>(EventKey<T1, T2, T3> eventKey)
        {
            if (_eventDic.TryGetValue(eventKey, out IEventData eventData))
                eventData.Clear();
        }
        /// <summary>
        /// 清理所有事件
        /// </summary>
        public void ClearAll()
        {
            foreach (IEventData value in _eventDic.Values)
                value.Clear();
            _eventDic.Clear();
        }
        #endregion
    }
}

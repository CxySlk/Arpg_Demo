using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LLKFrame
{
    public interface IMouseEvent :
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerMoveHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    { }

    public enum EventSystemType
    {
        OnBeginDrag,
        OnDrag,
        OnEndDrag,
        OnMouseDown,
        OnMouseUp,
        OnClick,
        OnMouseEnter,
        OnMouseMove,
        OnMouseExit,
    }

    [DisallowMultipleComponent]
    public class LLKEventListener : MonoBehaviour, IMouseEvent
    {
        #region 内部类
        private interface ILLKEventData
        {
            public void Destroy();
        }
        private class LLKEventData<T> : ILLKEventData
        {
            private Action<T> action;
            public void AddEventListener(Action<T> action) => this.action = action;
            public void RemoveEventListener(Action<T> action) => this.action -= action;
            public void TriggerEvent(T arg) => action?.Invoke(arg);
            public void Clear() => action = null;
            public void Destroy()
            {
                Clear();
                this.PushObject();
            }
        }
        private class LLKEventDatas
        {
            private Dictionary<int, ILLKEventData> eventDic = new Dictionary<int, ILLKEventData>();
            public void AddEventListener<T>(EventSystemType eventType, Action<T> action)
            {
                if (!eventDic.TryGetValue((int)eventType, out ILLKEventData eventData))
                {
                    eventData = PoolSystem.GetOrNewObject<LLKEventData<T>>();
                    eventDic.Add((int)eventType, eventData);
                }
                (eventData as LLKEventData<T>).AddEventListener(action);
            }
            public void RemoveEventListener<T>(EventSystemType eventType, Action<T> action)
            {
                if (eventDic.TryGetValue((int)eventType, out ILLKEventData eventData))
                    (eventData as LLKEventData<T>).RemoveEventListener(action);
            }
            public void TriggerEvent<T>(EventSystemType eventType, T arg)
            {
                if (eventDic.TryGetValue((int)eventType, out ILLKEventData eventData))
                    (eventData as LLKEventData<T>).TriggerEvent(arg);
            }
            public void Clear(EventSystemType eventType)
            {
                if (eventDic.Remove((int)eventType, out ILLKEventData eventData))
                    eventData.Destroy();
            }
            public void ClearAll()
            {
                foreach (ILLKEventData eventData in eventDic.Values)
                    eventData.Destroy();
                eventDic.Clear();
            }
            public void Destroy()
            {
                ClearAll();
                this.PushObject();
            }
        }
        #endregion
        private LLKEventDatas eventDatas = PoolSystem.GetOrNewObject<LLKEventDatas>();

        #region 鼠标相关（参数为PointerEventData）
        /// 拖拽相关
        public void OnBeginDrag(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnBeginDrag, eventData);
        public void OnDrag(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnDrag, eventData);
        public void OnEndDrag(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnEndDrag, eventData);
        /// 按下相关
        public void OnPointerDown(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnMouseDown, eventData);
        public void OnPointerUp(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnMouseUp, eventData);
        public void OnPointerClick(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnClick, eventData);
        /// 鼠标进入、移动、退出
        public void OnPointerEnter(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnMouseEnter, eventData);
        public void OnPointerMove(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnMouseMove, eventData);
        public void OnPointerExit(PointerEventData eventData) => eventDatas.TriggerEvent(EventSystemType.OnMouseExit, eventData);
        #endregion

        /// <summary>
        /// 新增事件监听
        /// </summary>
        /// <typeparam name="TArg">目前只支持鼠标相关事件---PointerEventData</typeparam>
        /// <param name="eventType">事件类型</param>
        /// <param name="action">事件</param>
        public void AddEventListener<TArg>(EventSystemType eventType, Action<TArg> action) => eventDatas.AddEventListener(eventType, action);
        public void RemovetListener<TArg>(EventSystemType eventType, Action<TArg> action) => eventDatas.RemoveEventListener(eventType, action);
        public void Clear(EventSystemType eventType) => eventDatas.Clear(eventType);
        public void ClearAll() => eventDatas.ClearAll();
        public void Destroy()
        {
            eventDatas.Destroy();
            eventDatas = null;
        }
        private void OnDestroy() => eventDatas?.Destroy();
    }
}

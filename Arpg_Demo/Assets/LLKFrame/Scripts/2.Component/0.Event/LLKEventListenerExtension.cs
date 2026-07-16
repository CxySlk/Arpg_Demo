using System;
using UnityEngine;

namespace LLKFrame
{
    public static class LLKEventListenerExtension
    {
        public static void AddEventListener<T>(this Component component, EventSystemType eventType, Action<T> action)
        {
            LLKEventListener eventListener = component.GetComponent<LLKEventListener>();
            if(eventListener==null)
                eventListener = component.gameObject.AddComponent<LLKEventListener>();
            eventListener.AddEventListener(eventType, action);
        }
        public static void RemoveEventListener<T>(this Component component, EventSystemType eventType, Action<T> action)
        {
            LLKEventListener eventListener = component.GetComponent<LLKEventListener>();
            if(eventListener==null)
                eventListener = component.gameObject.AddComponent<LLKEventListener>();
            eventListener.RemovetListener(eventType, action);
        }
        public static void ClearEventListener<T>(this Component component, EventSystemType eventType, Action<T> action)
        {
            LLKEventListener eventListener = component.GetComponent<LLKEventListener>();
            if(eventListener==null)
                eventListener = component.gameObject.AddComponent<LLKEventListener>();
            eventListener.RemovetListener(eventType, action);
        }
    }
}

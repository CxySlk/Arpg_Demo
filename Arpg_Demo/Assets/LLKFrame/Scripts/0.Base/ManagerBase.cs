using UnityEngine;

namespace LLKFrame
{
    public abstract class ManagerBase : MonoBehaviour
    {
        public virtual void Init() { }
    }
    public abstract class ManagerBase<T> : ManagerBase where T : ManagerBase<T>
    {
        private T instance;
        public T Instance => instance;
        protected virtual void Awake() => instance = this as T;
    }
}
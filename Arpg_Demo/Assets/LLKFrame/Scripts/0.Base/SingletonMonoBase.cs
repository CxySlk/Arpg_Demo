using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// 泛型单例基类（MonoBehaviour 版），子类需继承 SingletonMonoBase<TSpec>
    /// </summary>
    /// <typeparam name="T">子类类型</typeparam>
    public abstract class SingletonMonoBase<T> : MonoBehaviour where T : SingletonMonoBase<T>
    {
        protected static T instance;
        /// <summary>
        /// 全局唯一实例
        /// </summary>
        public static T Instance => instance;
        /// <summary>
        /// Awake 时将自身赋值给静态实例
        /// </summary>
        protected virtual void Awake() => instance = this as T;
    }
}

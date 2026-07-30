namespace LLKFrame
{
    /// <summary>
    /// 泛型单例基类（非 MonoBehaviour），子类需继承 SingletonBase<TSpec> 并提供无参构造
    /// </summary>
    /// <typeparam name="T">子类类型</typeparam>
    public abstract class SingletonBase<T> where T : SingletonBase<T>, new()
    {
        private static T instance;
        /// <summary>
        /// 全局唯一实例，首次访问时按需创建
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = new T();
                return instance;
            }
        }
    }
}

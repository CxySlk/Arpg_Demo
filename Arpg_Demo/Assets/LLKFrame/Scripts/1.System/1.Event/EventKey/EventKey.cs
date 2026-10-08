namespace LLKFrame
{
    public abstract class EventKeyBase
    {
        protected string keyString;
        public override int GetHashCode() => keyString.GetHashCode();
        public override bool Equals(object obj)
        {
            return (obj.GetType() == GetType()) &&
                    keyString == ((EventKeyBase)obj).keyString;
        }
    }
    /// <summary>没有参数的事件值键对象</summary>
    public class EventKey : EventKeyBase
    {
        public static implicit operator EventKey(string keyString)
        {
            EventKey eventKeyBase = new EventKey();
            eventKeyBase.keyString = keyString;
            return eventKeyBase;
        }
    }
    /// <summary>1 个参数的事件值键对象</summary>
    public class EventKey<T> : EventKeyBase
    {
        public static implicit operator EventKey<T>(string keyString)
        {
            EventKey<T> eventKeyBase = new EventKey<T>();
            eventKeyBase.keyString = keyString;
            return eventKeyBase;
        }
    }
    /// <summary>2 个参数的事件值键对象</summary>
    public class EventKey<T1,T2> : EventKeyBase
    {
        public static implicit operator EventKey<T1,T2>(string keyString)
        {
            EventKey<T1,T2> eventKeyBase = new EventKey<T1,T2>();
            eventKeyBase.keyString = keyString;
            return eventKeyBase;
        }
    }
    /// <summary>3 个参数的事件值键对象</summary>
    public class EventKey<T1,T2,T3> : EventKeyBase
    {
        public static implicit operator string(EventKey<T1,T2,T3> eventKey) => eventKey.keyString;
        public static implicit operator EventKey<T1,T2,T3>(string keyString)
        {
            EventKey<T1,T2,T3> eventKeyBase = new EventKey<T1,T2,T3>();
            eventKeyBase.keyString = keyString;
            return eventKeyBase;
        }
    }
}

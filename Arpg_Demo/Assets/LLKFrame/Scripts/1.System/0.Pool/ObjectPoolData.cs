using System.Collections.Generic;

namespace LLKFrame
{
    public class ObjectPoolData
    {
        private Stack<object> poolStack = new Stack<object>();
        public object GetObject()
        {
            if (poolStack.Count > 0)
                return poolStack.Pop();
            return null;
        }

        public void PushObject(object obj) => poolStack.Push(obj);
    }
}

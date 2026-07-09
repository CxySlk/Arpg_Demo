using System;
using System.Collections.Generic;

namespace LLKFrame
{
    public class ObjectPoolModule
    {
        private Dictionary<Type, ObjectPoolData> poolDic = new Dictionary<Type, ObjectPoolData>();

        public object GetObject(Type type)
        {
            if (poolDic.TryGetValue(type, out ObjectPoolData poolData))
                return poolData.GetObject();
            return null;
        }
        public void PushObject(object obj)
        {
            Type type = obj.GetType();
            if (!poolDic.TryGetValue(type, out ObjectPoolData poolData))
            {
                poolData = new ObjectPoolData();
                poolDic.Add(type, poolData);
            }
            poolData.PushObject(obj);
        }
        public void Clear(Type type) => poolDic.Remove(type);
        public void ClearAll() => poolDic.Clear();
    }
}

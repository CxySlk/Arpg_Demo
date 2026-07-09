using System.Collections.Generic;
using UnityEngine;

namespace LLKFrame
{
    public class GameObjectPoolModule
    {
        private Dictionary<string, GameObjectPoolData> poolDic = new Dictionary<string, GameObjectPoolData>();
        private Transform poolRoot;
        public void Init(Transform poolRoot) => this.poolRoot = poolRoot;

        public GameObject GetGameObject(string keyName,Transform parent)
        {
            if (poolDic.TryGetValue(keyName, out GameObjectPoolData poolData))
                return poolData.GetGameObject(parent);
            else return null;
        }
        public void PushGameObject(string keyName, GameObject obj)
        {
            if (!poolDic.TryGetValue(keyName, out GameObjectPoolData poolData))
            {
                poolData = new GameObjectPoolData(keyName, poolRoot);
                poolDic.Add(keyName, poolData);
            }
            poolData.PushGameObject(obj);
        }
        public void Clear(string keyName)
        {
            if (poolDic.Remove(keyName, out GameObjectPoolData poolData))
                poolData.Destroy();
        }
        public void ClearAll()
        {
            foreach (KeyValuePair<string, GameObjectPoolData> item in poolDic)
                item.Value.Destroy();
            poolDic.Clear();
        }
    }
}

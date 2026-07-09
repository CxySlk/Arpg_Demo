using System;
using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// 静态对象池系统，支持普通 C# 对象和 GameObject 的复用，减少频繁的创建和销毁开销
    /// </summary>
    public static class PoolSystem
    {
        private static ObjectPoolModule objectPoolModule;
        private static GameObjectPoolModule gameObjectPoolModule;
        private static Transform poolRootTransform;

        public static void Init()
        {
            objectPoolModule = new ObjectPoolModule();
            gameObjectPoolModule = new GameObjectPoolModule();
            poolRootTransform = new GameObject("Pool").transform;
            poolRootTransform.parent = GameRoot.RootTransform;
            gameObjectPoolModule.Init(poolRootTransform);
        }
        #region Object对象池
        public static object GetObject(Type type) => objectPoolModule.GetObject(type);
        public static T GetObject<T>() where T : class => objectPoolModule.GetObject(typeof(T)) as T;
        public static T GetOrNewObject<T>() where T : class, new()
        {
            T res = objectPoolModule.GetObject(typeof(T)) as T;
            if (res == null)
                res = new T();
            return res;
        }
        public static void PushObject(object obj)
        {
            if (obj != null)
                objectPoolModule.PushObject(obj);
        }

        public static void ClearObjectPool(Type type) => objectPoolModule.Clear(type);
        public static void ClearAllObjectPool() => objectPoolModule.ClearAll();
        #endregion
        #region GameObject对象池
        public static GameObject GetGameObject(string keyName, Transform parent = null) => gameObjectPoolModule.GetGameObject(keyName, parent);
        public static T GetGameObject<T>(string keyName, Transform parent = null) where T : Component => gameObjectPoolModule.GetGameObject(keyName, parent)?.GetComponent<T>();
        public static GameObject GetGameObject(GameObject prefab, Transform parent = null)
        {
            string prefabName = prefab.name;
            GameObject obj = gameObjectPoolModule.GetGameObject(prefabName, parent);
            if (obj == null)
            {
                obj = GameObject.Instantiate(prefab, parent);
                obj.name = prefabName;
            }
            return obj;
        }
        public static T GetGameObject<T>(GameObject prefab, Transform parent = null) where T : Component
        {
            string prefabName = prefab.name;
            GameObject obj = gameObjectPoolModule.GetGameObject(prefabName, parent);
            if (obj == null)
            {
                obj = GameObject.Instantiate(prefab, parent);
                obj.name = prefabName;
            }
            return obj.GetComponent<T>();
        }
        public static void PushGameObject(string keyName, GameObject obj)
        {
            if (obj != null)
                gameObjectPoolModule.PushGameObject(keyName, obj);
        }

        public static void PushGameObject(GameObject obj) => PushGameObject(obj.name, obj);
        public static void ClearGameObjectPool(string keyName) => gameObjectPoolModule.Clear(keyName);
        public static void ClearAllGameObjectPool() => gameObjectPoolModule.ClearAll();
        #endregion
    }
}

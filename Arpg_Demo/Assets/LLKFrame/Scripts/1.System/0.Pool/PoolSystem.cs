using System;
using UnityEngine;

namespace LLKFrame
{
    /// <summary>
    /// 对象池系统，支持普通 C# 对象和 GameObject 的复用，减少频繁的创建和销毁开销。
    /// 全局使用 PoolSystem.Global，也可 new PoolSystem() 创建局部独立的小池。
    /// </summary>
    public class PoolSystem
    {
        /// <summary>全局对象池实例</summary>
        public static readonly PoolSystem Global = new PoolSystem();

        private readonly ObjectPoolModule objectPoolModule;
        private readonly GameObjectPoolModule gameObjectPoolModule;
        public Transform PoolRootTransform { get; private set; }

        public PoolSystem()
        {
            objectPoolModule = new ObjectPoolModule();
            gameObjectPoolModule = new GameObjectPoolModule();
            CreatePoolRoot();
            gameObjectPoolModule.Init(PoolRootTransform);
        }

        /// <summary>创建池根：运行时挂到 GameRoot，编辑器非 Play 模式挂到统一编辑器根下</summary>
        private void CreatePoolRoot()
        {
            PoolRootTransform = new GameObject("Pool").transform;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                PoolRootTransform.hideFlags = HideFlags.HideAndDontSave;
                // 挂到统一编辑器根下，随播放清理一起销毁
                Transform editorRoot = GameObject.Find("GASEditorPreviewRoot")?.transform;
                if (editorRoot == null)
                {
                    editorRoot = new GameObject("GASEditorPreviewRoot").transform;
                    editorRoot.hideFlags = HideFlags.HideAndDontSave;
                }
                PoolRootTransform.parent = editorRoot;
                return;
            }
#endif
            PoolRootTransform.parent = GameRoot.RootTransform;
        }

        #region Object对象池
        public object GetObject(Type type) => objectPoolModule.GetObject(type);
        public T GetObject<T>() where T : class => GetObject(typeof(T)) as T;
        public T GetOrNewObject<T>() where T : class, new()
        {
            T res = objectPoolModule.GetObject(typeof(T)) as T;
            if (res == null)
                res = new T();
            return res;
        }
        public void PushObject(object obj)
        {
            if (obj != null)
                objectPoolModule.PushObject(obj);
        }

        public void ClearObjectPool(Type type) => objectPoolModule.Clear(type);
        public void ClearAllObjectPool() => objectPoolModule.ClearAll();
        #endregion

        #region GameObject对象池
        public GameObject GetGameObject(string keyName, Transform parent = null) => gameObjectPoolModule.GetGameObject(keyName, parent);
        public T GetGameObject<T>(string keyName, Transform parent = null) where T : Component => GetGameObject(keyName, parent)?.GetComponent<T>();
        public GameObject GetGameObject(GameObject prefab, Transform parent = null)
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
        public T GetGameObject<T>(GameObject prefab, Transform parent = null) where T : Component
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
        public void PushGameObject(string keyName, GameObject obj)
        {
            if (obj != null)
                gameObjectPoolModule.PushGameObject(keyName, obj);
        }
        public void PushGameObject(GameObject obj)
        {
            if (obj == null) return;
            PushGameObject(obj.name, obj);
        }
        public void ClearGameObjectPool(string keyName) => gameObjectPoolModule.Clear(keyName);
        public void ClearAllGameObjectPool() => gameObjectPoolModule.ClearAll();
        #endregion
    }
}

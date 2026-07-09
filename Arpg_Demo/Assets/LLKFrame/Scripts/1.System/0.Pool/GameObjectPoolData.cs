using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LLKFrame
{
    public class GameObjectPoolData
    {
        private Transform layerRoot;
        private Stack<GameObject> poolStack;

        public GameObjectPoolData(string layerName, Transform poolRoot)
        {
            layerRoot = new GameObject(layerName).transform;
            layerRoot.SetParent(poolRoot);
            poolStack = new Stack<GameObject>();
        }

        public GameObject GetGameObject(Transform parent = null)
        {
            GameObject obj = null;
            if (poolStack.Count > 0)
            {
                obj = poolStack.Pop();
                obj.SetActive(true);
                obj.transform.SetParent(parent);
                if (parent == null) SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
            }
            return obj;
        }

        public void PushGameObject(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.parent = layerRoot;
            poolStack.Push(obj);
        }

        public void Destroy() => GameObject.Destroy(layerRoot.gameObject);
    }
}

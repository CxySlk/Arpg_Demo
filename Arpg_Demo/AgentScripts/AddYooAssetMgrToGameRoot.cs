using UnityEditor;
using UnityEngine;

namespace AgentTools
{
    public static class AddYooAssetMgrToGameRoot
    {
        private const string GameRootPath = "Assets/LLKFrame/Prefab/GameRoot.prefab";

        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameRootPath);
            try
            {
                if (root.GetComponent<YooAssetMgr>() == null)
                    root.AddComponent<YooAssetMgr>();

                PrefabUtility.SaveAsPrefabAsset(root, GameRootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("YooAssetMgr added to GameRoot prefab.");
        }
    }
}

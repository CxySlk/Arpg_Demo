using LLKFrame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AgentTools
{
    public static class HotUpdateConfigBuilder
    {
        private const string SettingsPath = "Assets/LLKFrame/Setting/LLKFrameSetting.asset";
        private const string ConfirmPath =
            "Assets/Prefabs/UI/HotUpdateScene/UI_UpdateConfirmWindow.prefab";
        private const string ProgressPath =
            "Assets/Prefabs/UI/HotUpdateScene/UI_HotUpdateWindow.prefab";

        public static void Build()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new System.InvalidOperationException("Addressables settings not found.");

            AddressableAssetGroup group = settings.FindGroup("Common");
            if (group == null)
                throw new System.InvalidOperationException("Addressables group 'Common' not found.");

            AddAddress(settings, group, ConfirmPath, "UI_UpdateConfirmWindow");
            AddAddress(settings, group, ProgressPath, "UI_HotUpdateWindow");

            LLKFrameSetting frameSetting = AssetDatabase.LoadAssetAtPath<LLKFrameSetting>(SettingsPath);
            if (frameSetting == null)
                throw new System.InvalidOperationException("LLKFrameSetting not found.");

            frameSetting.InitForEditor();
            EditorUtility.SetDirty(frameSetting);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Hot update UI config and Addressables entries updated.");
        }

        private static void AddAddress(
            AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new System.InvalidOperationException("Asset not found: " + assetPath);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entry.address = address;
        }
    }
}

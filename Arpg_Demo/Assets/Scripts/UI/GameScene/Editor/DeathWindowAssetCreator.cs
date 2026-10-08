using LLKFrame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 一键生成 UI_DeathWindow 预制体(仅根节点 + 脚本,面板内容由脚本运行时代码构建)
/// 并注册 Addressable(地址 = 窗口 key),同时刷新 LLKFrameSetting 的 uiWindowDataDic。
/// 菜单:Tools/FloatingText/生成死亡面板并注册
/// </summary>
public static class DeathWindowAssetCreator
{
    private const string PrefabPath = "Assets/Prefabs/UI/GameScene/UI_DeathWindow.prefab";

    [MenuItem("Tools/FloatingText/生成死亡面板并注册")]
    public static void CreateDeathWindow()
    {
        // 0. 删除旧资产避免残留
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);
        AssetDatabase.Refresh();

        // 1. 构造根:RectTransform + UI_DeathWindow(内容代码自建)
        GameObject root = new(nameof(UI_DeathWindow), typeof(RectTransform));
        root.AddComponent<UI_DeathWindow>();

        // 2. 存预制体并清理
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);

        // 3. 注册 Addressable(地址 = 窗口 key)
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("DeathWindowAssetCreator: AddressableAssetSettings 为空");
            return;
        }

        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, true);
        entry.SetAddress(nameof(UI_DeathWindow), false);
        AssetDatabase.SaveAssets();

        // 4. 刷新 LLKFrameSetting.uiWindowDataDic(反射收集 UIWindowAttribute)
        string[] settingGuids = AssetDatabase.FindAssets("t:LLKFrameSetting");
        foreach (string settingGuid in settingGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(settingGuid);
            LLKFrameSetting setting = AssetDatabase.LoadAssetAtPath<LLKFrameSetting>(path);
            if (setting == null) continue;
            setting.Reset(); // #if UNITY_EDITOR public 方法,内部 InitUIDataDic 反射收集
            EditorUtility.SetDirty(setting);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"DeathWindowAssetCreator: 已生成 {PrefabPath} 并注册 Addressable '{nameof(UI_DeathWindow)}'");
    }
}

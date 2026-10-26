using System.Collections.Generic;
using System.Reflection;
using Animancer;
using UnityEditor;
using UnityEngine;

// 把旧 SO 的 standardAnimationDic 填进预制体 CharacterView._animationTransitionDic
public static class FillCharacterViewAnims
{
    static readonly string[] PlayerKeys =
    {
        "Death", "Idle", "Jump", "InAir_End", "Move", "Move_LockTarget", "InAir_Loop"
    };

    static readonly (string key, string path)[] EnemyAnims =
    {
        ("Idle", "Assets/Config/Enemy/Animation/Idle.asset"),
        ("Death", "Assets/Config/Enemy/Animation/Death.asset"),
        ("Idle_Combat", "Assets/Config/Enemy/Animation/Idle_Combat.asset"),
        ("Idle_Combat_to_Idle", "Assets/Config/Enemy/Animation/Idle_Combat_to_Idle.asset"),
        ("Idle_to_Idle_Combat", "Assets/Config/Enemy/Animation/Idle_to_Idle_Combat.asset"),
        ("Move", "Assets/Config/Enemy/Animation/Move.asset"),
        ("MoveBack", "Assets/Config/Enemy/Animation/MoveBack.asset"),
        ("Turn_90_L", "Assets/Config/Enemy/Animation/Turn_90_L.asset"),
        ("Turn_90_R", "Assets/Config/Enemy/Animation/Turn_90_R.asset"), // 修正：旧 SO 错指 Turn_180_R
        ("Turn_180_L", "Assets/Config/Enemy/Animation/Turn_180_L.asset"),
        ("Turn_180_R", "Assets/Config/Enemy/Animation/Turn_180_R.asset"),
    };

    public static void Run()
    {
        Dictionary<string, TransitionAsset> player = new();
        foreach (string k in PlayerKeys)
            player[k] = AssetDatabase.LoadAssetAtPath<TransitionAsset>($"Assets/Config/Player/Animancer/Animation/{k}.asset");

        Dictionary<string, TransitionAsset> enemy = new();
        foreach ((string key, string path) in EnemyAnims)
            enemy[key] = AssetDatabase.LoadAssetAtPath<TransitionAsset>(path);

        Fill("Assets/Prefabs/Player/Player1Root.prefab", player);
        Fill("Assets/Prefabs/Player/Player1View.prefab", player);
        Fill("Assets/Prefabs/Enemy/Enemy.prefab", enemy);
    }

    static void Fill(string prefabPath, Dictionary<string, TransitionAsset> dict)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            CharacterView cv = root.GetComponentInChildren<CharacterView>(true);
            if (cv == null) { Debug.LogError($"[FillAnims] 没有 CharacterView: {prefabPath}"); return; }

            FieldInfo fi = typeof(CharacterView).GetField("_animationTransitionDic",
                BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(cv, new Dictionary<string, TransitionAsset>(dict));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[FillAnims] {prefabPath} <- {dict.Count} 项");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 回读验证
        GameObject check = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            CharacterView cv = check.GetComponentInChildren<CharacterView>(true);
            FieldInfo fi = typeof(CharacterView).GetField("_animationTransitionDic",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var back = fi.GetValue(cv) as Dictionary<string, TransitionAsset>;
            Debug.Log($"[FillAnims] 回读 {prefabPath}: {(back == null ? "null" : back.Count + " 项")}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(check);
        }
    }
}

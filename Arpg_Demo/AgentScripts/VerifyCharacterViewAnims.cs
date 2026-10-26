using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Animancer;
using UnityEditor;
using UnityEngine;

public static class VerifyCharacterViewAnims
{
    public static void Run()
    {
        foreach (string p in new[]
                 {
                     "Assets/Prefabs/Player/Player1Root.prefab",
                     "Assets/Prefabs/Player/Player1View.prefab",
                     "Assets/Prefabs/Enemy/Enemy.prefab"
                 })
        {
            GameObject root = PrefabUtility.LoadPrefabContents(p);
            try
            {
                CharacterView cv = root.GetComponentInChildren<CharacterView>(true);
                FieldInfo fi = typeof(CharacterView).GetField("_animationTransitionDic",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Dictionary<string, TransitionAsset> d = fi.GetValue(cv) as Dictionary<string, TransitionAsset>;
                StringBuilder sb = new();
                if (d == null) sb.Append("null");
                else foreach (var kv in d) sb.Append(kv.Key).Append(kv.Value == null ? "(NULL!)" : "").Append(' ');
                Debug.Log($"[VerifyAnims] {p} ({root.GetComponentsInChildren<CharacterView>(true).Length} 个CV) -> {(d == null ? "null" : d.Count + " 项")}: {sb}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

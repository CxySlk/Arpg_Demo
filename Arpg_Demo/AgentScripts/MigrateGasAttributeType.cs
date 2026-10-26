using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 迁移序列化数据里失效的类型名/程序集：GAS.AttributeType -> cfg.EAttributeType，程序集 GAS -> Assembly-CSharp
public static class MigrateGasAttributeType
{
    static readonly (string from, string to)[] Rules =
    {
        ("GAS.AttributeType", "cfg.EAttributeType"),
        ("GAS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
         "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
        (", GAS]", ", Assembly-CSharp]"),
    };

    public static void Run()
    {
        string assets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        string[] exts = { ".asset", ".prefab", ".unity" };
        UTF8Encoding enc = new UTF8Encoding(false);
        List<string> changed = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string f in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (System.Array.IndexOf(exts, Path.GetExtension(f).ToLowerInvariant()) < 0) continue;
                string text = File.ReadAllText(f, Encoding.UTF8);
                if (!text.Contains("GAS.AttributeType") && !text.Contains(", GAS]") &&
                    !text.Contains("GAS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
                    continue;

                string outp = text;
                foreach ((string from, string to) in Rules)
                    outp = outp.Replace(from, to);

                if (outp != text)
                {
                    File.WriteAllText(f, outp, enc);
                    changed.Add(f.Substring(assets.Length + 1));
                }
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        AssetDatabase.Refresh();
        foreach (string c in changed) Debug.Log($"[MigrateGAS] changed: {c}");
        Debug.Log($"[MigrateGAS] done, {changed.Count} files");
    }
}

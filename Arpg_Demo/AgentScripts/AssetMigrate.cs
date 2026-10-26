using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 阶段0：把 Config/Prefabs 引用到、且不在 Res 下的 .anim / .png 复制进 Res 并分类，然后重定向引用。
public static class AssetMigrate
{
    static readonly Regex GuidRx = new Regex("guid: ([0-9a-fA-F]{32})", RegexOptions.Compiled);
    static readonly string[] ScanExt = { ".asset", ".prefab" };

    public static void Run()
    {
        string proj = Directory.GetCurrentDirectory();
        string assetsDir = Path.Combine(proj, "Assets");
        string report = Path.Combine(proj, "AgentScripts", "migrate_report.txt");
        StringBuilder sb = new StringBuilder();

        // 1) guid -> 相对路径(Assets/...)
        Dictionary<string, string> guid2path = BuildGuidIndex(proj);
        sb.AppendLine($"guid index = {guid2path.Count}");

        // 2) 扫描 Config + Prefabs 的引用
        HashSet<string> referenced = new HashSet<string>();
        Dictionary<string, HashSet<string>> cat = new Dictionary<string, HashSet<string>>(); // guid -> {Player/Base,...}
        foreach (string dir in new[] { "Assets/Config", "Assets/Prefabs" })
        {
            string full = Path.Combine(proj, dir);
            if (!Directory.Exists(full)) continue;
            foreach (string f in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
            {
                if (!ScanExt.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
                string rel = Rel(proj, f);
                string ch = rel.Contains("/Config/Player/") ? "Player" : rel.Contains("/Config/Enemy/") ? "Enemy" : null;
                string sub = rel.Contains("/Skill/") ? "Skill" : "Base";
                string text = File.ReadAllText(f);
                foreach (Match m in GuidRx.Matches(text))
                {
                    string g = m.Groups[1].Value.ToLowerInvariant();
                    referenced.Add(g);
                    if (ch != null)
                    {
                        if (!cat.TryGetValue(g, out var set)) cat[g] = set = new HashSet<string>();
                        set.Add(ch + "/" + sub);
                    }
                }
            }
        }

        // 3) 规划：只处理 .anim / .png 且不在 Res 下的
        List<(string src, string dst, string kind, string tag)> plan = new List<(string, string, string, string)>();
        List<string> conflicts = new List<string>();
        Dictionary<string, int> dstUsed = new Dictionary<string, int>();
        foreach (string g in referenced)
        {
            if (!guid2path.TryGetValue(g, out string p)) continue;
            if (p.StartsWith("Assets/Res/")) continue;
            string ext = Path.GetExtension(p).ToLowerInvariant();
            if (ext != ".anim" && ext != ".png") continue;

            string dstDir, tag;
            if (ext == ".anim")
            {
                string ch = "Player", sub = "Base";
                if (cat.TryGetValue(g, out var set))
                {
                    ch = set.Any(s => s.StartsWith("Player/")) ? "Player" : "Enemy";
                    sub = set.Any(s => s.EndsWith("/Base")) ? "Base" : "Skill";
                    bool chMixed = set.Any(s => s.StartsWith("Player/")) && set.Any(s => s.StartsWith("Enemy/"));
                    bool subMixed = set.Any(s => s.EndsWith("/Base")) && set.Any(s => s.EndsWith("/Skill"));
                    if (chMixed || subMixed) conflicts.Add($"{p}  cats={string.Join(",", set)}");
                }
                else
                {
                    conflicts.Add($"{p}  no-category->Player/Base");
                }
                dstDir = $"Assets/Res/Animations/{ch}/{sub}";
                tag = ch + "/" + sub;
            }
            else // png
            {
                bool icon = p.Contains("/ArtRes/SkillIcons/");
                dstDir = icon ? "Assets/Res/Sprite/Player/SkillIcon" : "Assets/Res/Sprite/UI";
                tag = icon ? "Sprite/Player/SkillIcon" : "Sprite/UI";
            }

            string fileName = Path.GetFileName(p);
            string dst = dstDir + "/" + fileName;
            string key = dst.ToLowerInvariant();
            if (dstUsed.ContainsKey(key))
            {
                string n = Path.GetFileNameWithoutExtension(fileName);
                string e = Path.GetExtension(fileName);
                int k = dstUsed[key] + 1;
                dstUsed[key] = k;
                dst = $"{dstDir}/{n}_{k}{e}";
                key = dst.ToLowerInvariant();
            }
            if (File.Exists(Path.Combine(proj, dst))) { dstUsed[key] = 1; continue; } // 已存在→跳过(幂等)
            dstUsed[key] = 0;
            plan.Add((p, dst, ext, tag));
        }

        sb.AppendLine($"plan copies = {plan.Count} (anim={plan.Count(x => x.kind == ".anim")}, png={plan.Count(x => x.kind == ".png")})");
        sb.AppendLine("---- conflicts ----");
        foreach (var c in conflicts) sb.AppendLine(c);

        // 4) 建目录 + 复制
        Dictionary<string, string> old2new = new Dictionary<string, string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in plan)
            {
                EnsureFolder(proj, Path.GetDirectoryName(item.dst).Replace('\\', '/'));
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();

        int ok = 0;
        foreach (var item in plan)
        {
            string srcAbs = Path.Combine(proj, item.src);
            string dstAbs = Path.Combine(proj, item.dst);
            if (File.Exists(dstAbs)) continue;
            bool done = AssetDatabase.CopyAsset(item.src, item.dst);
            if (done)
            {
                ok++;
                string oldG = AssetDatabase.AssetPathToGUID(item.src).ToLowerInvariant();
                string newG = AssetDatabase.AssetPathToGUID(item.dst).ToLowerInvariant();
                if (!string.IsNullOrEmpty(oldG) && !string.IsNullOrEmpty(newG)) old2new[oldG] = newG;
                sb.AppendLine($"COPY {item.src} -> {item.dst}");
            }
            else sb.AppendLine($"FAIL {item.src} -> {item.dst}");
        }
        sb.AppendLine($"copied = {ok}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 5) 重定向：把所有 Assets 下 .asset/.prefab/.unity 里出现的 old guid 换成 new guid
        int changedFiles = Repoint(proj, old2new, sb);
        sb.AppendLine($"repoint changed files = {changedFiles}, guid pairs = {old2new.Count}");

        Directory.CreateDirectory(Path.GetDirectoryName(report));
        File.WriteAllText(report, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[AssetMigrate] copies={ok} repointFiles={changedFiles} report={report}");
    }

    static int Repoint(string proj, Dictionary<string, string> old2new, StringBuilder sb)
    {
        if (old2new.Count == 0) return 0;
        Regex rx = new Regex("guid: ([0-9a-fA-F]{32})", RegexOptions.Compiled);
        string assets = Path.Combine(proj, "Assets");
        string[] exts = { ".asset", ".prefab", ".unity" };
        int changed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string f in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (!exts.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
                string text = File.ReadAllText(f);
                bool any = false;
                string outp = rx.Replace(text, m =>
                {
                    string g = m.Groups[1].Value.ToLowerInvariant();
                    if (old2new.TryGetValue(g, out string ng)) { any = true; return "guid: " + ng; }
                    return m.Value;
                });
                if (any)
                {
                    File.WriteAllText(f, outp, new UTF8Encoding(false));
                    changed++;
                }
            }
        }
        catch (Exception e) { sb.AppendLine("repoint EX: " + e.Message); }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();
        return changed;
    }

    static Dictionary<string, string> BuildGuidIndex(string proj)
    {
        var map = new Dictionary<string, string>();
        string assets = Path.Combine(proj, "Assets");
        foreach (string meta in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
        {
            try
            {
                string[] lines = File.ReadAllLines(meta, Encoding.UTF8);
                int n = Math.Min(6, lines.Length);
                for (int i = 0; i < n; i++)
                {
                    if (lines[i].StartsWith("guid: "))
                    {
                        string g = lines[i].Substring(6).Trim().ToLowerInvariant();
                        string assetPath = Rel(proj, meta.Substring(0, meta.Length - 5));
                        if (!map.ContainsKey(g)) map[g] = assetPath;
                        break;
                    }
                }
            }
            catch { }
        }
        return map;
    }

    static void EnsureFolder(string proj, string relFolder)
    {
        string cur = proj;
        foreach (string seg in relFolder.Replace('\\', '/').Split('/'))
        {
            string next = Path.Combine(cur, seg);
            if (!Directory.Exists(next))
            {
                string parentRel = Rel(proj, cur);
                if (!AssetDatabase.IsValidFolder(Rel(proj, next)))
                    AssetDatabase.CreateFolder(parentRel, seg);
            }
            cur = next;
        }
    }

    static string Rel(string proj, string abs)
    {
        string r = Path.GetRelativePath(proj, abs).Replace('\\', '/');
        return r;
    }
}

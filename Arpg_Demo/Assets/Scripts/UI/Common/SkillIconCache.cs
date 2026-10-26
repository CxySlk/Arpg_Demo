using System.Collections.Generic;
using Data;
using LLKFrame;
using UnityEngine;

/// <summary>技能图标缓存：每个技能只加载一次 SkillConfig，窗口销毁时统一释放。</summary>
public sealed class SkillIconCache
{
    private readonly Dictionary<int, SkillConfig> _configs = new();
    private readonly Dictionary<int, string> _paths = new();

    public Sprite Get(int skillId)
    {
        if (_configs.TryGetValue(skillId, out SkillConfig cached))
            return cached != null ? cached.SkillIcon : null;

        cfg.Skill data = ConfigManager.Tables.TbSkill.GetOrDefault(skillId);
        if (data == null || string.IsNullOrEmpty(data.SkillConfigPath)) return null;

        SkillConfig config = ResSystem.LoadAsset<SkillConfig>(data.SkillConfigPath);
        if (config == null) return null;

        _configs[skillId] = config;
        _paths[skillId] = data.SkillConfigPath;
        return config.SkillIcon;
    }

    public void ReleaseAll()
    {
        foreach (string path in _paths.Values)
            ResSystem.Release<SkillConfig>(path);

        _configs.Clear();
        _paths.Clear();
    }
}

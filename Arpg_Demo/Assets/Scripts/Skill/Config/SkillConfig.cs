using System.Collections.Generic;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;

/// <summary>
/// 技能资产包：只放"Excel 装不下"的 Unity 资产（技能行为、技能片段、图标）。
/// 技能的数据（名字/描述/CD/消耗/是否显示UI/片段间隔）在 Luban 技能表 <c>cfg.Skill</c> 里。
/// </summary>
/// <remarks>
/// 一个 <c>SkillConfig</c> 对应一个技能 id（<c>cfg.Skill.SkillConfigPath</c> 指向它）：
/// - <see cref="SkillBehavior"/>：技能运行时的行为逻辑模板（SkillBrain 启动时深拷贝）；
/// - <see cref="Abilities"/>：组成该技能的多段技能片段（AbilityAsset）；
/// - <see cref="SkillIcon"/>：技能图标。
/// </remarks>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/SkillConfig")]
public class SkillConfig : ConfigBase
{
    [LabelText("技能图标")] [SerializeField] private Sprite _skillIcon;

    /// <summary>技能图标（资产）。</summary>
    public Sprite SkillIcon => _skillIcon;

    [LabelText("技能行为")] [SerializeField] private SkillBehaviorBase _skillBehavior;

    /// <summary>技能运行时的行为实例模板（如普攻连段行为）。SkillBrain 启动时深拷贝该实例。</summary>
    public SkillBehaviorBase SkillBehavior => _skillBehavior;

    [LabelText("技能片段列表")] [SerializeField] private List<AbilityAsset> _abilities = new();

    /// <summary>组成该技能的多段技能片段（AbilityAsset，时间轴/帧事件数据），可多段连招。</summary>
    public List<AbilityAsset> Abilities => _abilities;
}

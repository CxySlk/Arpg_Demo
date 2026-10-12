using Sirenix.OdinInspector;
using System.Collections.Generic;
using GAS;
using LLKFrame;
using Skill;
using UnityEngine;

/// <summary>
/// 技能配置资产。组合多个技能片段（<see cref="AbilityAsset"/>），并定义技能级参数（冷却、消耗、技能行为）。
/// </summary>
/// <remarks>
/// 一个 <c>SkillConfig</c> 对应玩家学会的一个"技能"：
/// - <see cref="SkillBehavior"/>：技能运行时的行为逻辑（CD、施法流程、帧事件处理等）；
/// - <see cref="Abilities"/>：组成该技能的多段技能片段（如普攻连段的第 1、2、3 段）；
/// - <see cref="CostDic"/> / <see cref="Cost"/>：释放技能需要消耗的属性/数值。
/// 由 <c>SkillBrainBase</c> 加载到玩家身上，并根据 <c>SkillLearnData</c> 决定是否可用。
/// </remarks>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Config/SkillConfig")]
public class SkillConfig : ConfigBase
{
    [Header("技能信息")] [SerializeField, LabelText("技能名称")]
    private string _skillName = "新技能";
    /// <summary>技能名称（读写访问）。</summary>
    public string SkillName => _skillName;
    
    [LabelText("是否显示在UI面板"), SerializeField]
    private bool _isShowUI = true;
    public bool IsShowUI => _isShowUI;

    [SerializeField, LabelText("技能描述")] [TextArea]
    private string _skillDescription;

    /// <summary>技能描述（多行文本，读写访问）。</summary>
    public string SkillDescription => _skillDescription;

    [SerializeField, LabelText("技能图标")] private Sprite _skillIcon;
    public Sprite SkillIcon => _skillIcon;

    [Header("技能参数"), LabelText("冷却时间(秒)"), SerializeField, MinValue(0)]
    private float _cdTime;

    /// <summary>技能冷却时间（秒）。由技能行为在施法后按此值进入冷却。</summary>
    public float CdTime => _cdTime;

    [LabelText("技能行为"), SerializeField] private SkillBehaviorBase _skillBehavior;

    /// <summary>技能运行时的行为实例（如普攻连段行为）。SkillBrain 启动时深拷贝该实例。</summary>
    public SkillBehaviorBase SkillBehavior => _skillBehavior;

    [LabelText("技能消耗"), SerializeField] private Dictionary<AttributeType, float> _costDic = new();

    /// <summary>释放技能消耗的属性字典：属性类型 → 消耗数值。</summary>
    public Dictionary<AttributeType, float> CostDic => _costDic;

    [Header("技能片段")] [SerializeField, LabelText("技能片段列表")]
    private List<AbilityAsset> _abilities = new();

    /// <summary>组成该技能的多段技能片段（AbilityAsset，时间轴/帧事件数据），可多段连招。</summary>
    public List<AbilityAsset> Abilities => _abilities;

    [SerializeField, LabelText("技能片段间隔")] private float _abilityInterval = 3;

    /// <summary>组成该技能的多段技能片段（AbilityAsset，时间轴/帧事件数据），可多段连招。</summary>
    public float AbilityInterval => _abilityInterval;
}
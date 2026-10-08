using System.Collections.Generic;
using Animancer;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterConfigBase : ConfigBase
{
    [BoxGroup("资源路径")] [LabelText("预制体路径"), SerializeField, Required]
    protected string prefabPath;

    public string PrefabPath => prefabPath;

    [BoxGroup("角色数据")]
    [LabelText("角色基础动画")]
    [DictionaryDrawerSettings(KeyLabel = "动画名", ValueLabel = "动画片段",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    [SerializeField, Required]
    protected Dictionary<string, TransitionAsset> standardAnimationDic;

    public Dictionary<string, TransitionAsset> StandardAnimationDic => standardAnimationDic;

    [BoxGroup("角色数据")] [LabelText("ASC预设数据"), SerializeField, Required]
    protected AbilitySystemComponentPreset ascPreset;

    public AbilitySystemComponentPreset AscPreset => ascPreset;

    [BoxGroup("技能")]
    [LabelText("技能配置列表"), SerializeField,
     ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "SkillName", ShowItemCount = true,
         AddCopiesLastElement = true)]
    protected List<SkillConfig> skillConfigList = new();

    public List<SkillConfig> SkillConfigList => skillConfigList;

    [BoxGroup("技能")]
    [LabelText("初始默认会的技能"), SerializeField,
     ListDrawerSettings(ShowItemCount = true, AddCopiesLastElement = true),
     Tooltip("填入的技能 ID 对应上方技能配置列表的索引")]
    protected List<int> defaultLearnSkills = new();

    public List<int> DefaultLearnSkills => defaultLearnSkills;
}
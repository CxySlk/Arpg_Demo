using System.Collections.Generic;
using cfg;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAS
{
    /// <summary>
    /// ASC 初始化预设资产。定义角色的初始属性集合和固定标签集。
    /// 挂载在 AbilitySystemComponent 的 _preset 字段上。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(AbilitySystemComponentPreset), menuName = "GAS/ASCPreset")]
    public class AbilitySystemComponentPreset : ConfigBase
    {
        [FoldoutGroup("属性配置")]
        [BoxGroup("属性配置/固定标签集")]
        [GUIColor(0.30f, 0.80f, 0.45f)]
        [LabelText("固定标签集"), SerializeField]
        private GameplayTagSet _tagSet = new();

        [FoldoutGroup("属性配置")] [BoxGroup("属性配置/效果配置")] [GUIColor(1f, 0.62f, 0.20f)] [SerializeField, LabelText("效果配置")]
        private List<GameplayEffect> _effects = new();

        public GameplayTagSet TagSet => _tagSet;
        public List<GameplayEffect> Effects => _effects;
    }
}
using LLKFrame;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// ASC 初始化预设资产。定义角色的初始属性集合和固定标签集。
    /// 挂载在 AbilitySystemComponent 的 _preset 字段上。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(AbilitySystemComponentPreset), menuName = "GAS/ASCPreset")]
    public class AbilitySystemComponentPreset : ConfigBase
    {
        [SerializeField]
        private HashSet<Attribute> _attributes;
        [SerializeField]
        private GameplayTagSet _tagSet;

        public HashSet<Attribute> Attributes => _attributes;
        public GameplayTagSet TagSet => _tagSet;
    }
}

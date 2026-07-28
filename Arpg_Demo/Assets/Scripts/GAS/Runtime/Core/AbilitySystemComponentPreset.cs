using LLKFrame;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(fileName = nameof(AbilitySystemComponentPreset), menuName = "GAS/" + nameof(AbilitySystemComponentPreset))]
    public class AbilitySystemComponentPreset : ConfigBase
    {
        [SerializeField, Header("属性")] private HashSet<Attribute> _attributes = new HashSet<Attribute>();
        public HashSet<Attribute> Attributes => _attributes;
        [SerializeField, Header("标签")] private GameplayTagSet _tagSet = new GameplayTagSet();
        public GameplayTagSet TagSet => _tagSet;
    }
}

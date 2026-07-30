using MessagePack;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 游戏标签结构体（值类型）。通过字符串的 HashCode 作为唯一标识，
    /// 支持父子层级关系（用于 IsChildOf/IsParentOf 层级匹配）。
    /// Editor 模式下保留字符串名称以供可读性。
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public partial struct GameplayTag
    {
        [Key(0)]
        [SerializeField]
        private int _hashCode;

        [Key(1)]
        [SerializeField]
        private int[] _ancestorHashCode;

        /// <summary>标签哈希标识符</summary>
        public int HashCode => _hashCode;
        /// <summary>所有祖先标签的哈希数组（由编辑器根据分隔符层级计算）</summary>
        public int[] AncestorHashCode => _ancestorHashCode;

        /// <summary>当前标签是否为 other 的子标签（即 other 是否在祖先列表中）</summary>
        public bool IsChildOf(GameplayTag other) => _ancestorHashCode.Contains(other._hashCode);
        /// <summary>当前标签是否为 other 的父标签</summary>
        public bool IsParentOf(GameplayTag other) => other._ancestorHashCode.Contains(_hashCode);

        public override int GetHashCode() => _hashCode;
        public override bool Equals(object obj) => obj is GameplayTag tag && _hashCode == tag._hashCode;
        public static bool operator ==(GameplayTag a, GameplayTag b) => a._hashCode == b._hashCode;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a._hashCode != b._hashCode;

#if UNITY_EDITOR
        [IgnoreMember]
        [SerializeField, LabelText("标签名"), ValueDropdown(nameof(TagGetter)), OnValueChanged(nameof(OnTagChanged))]
        private string _tagName;

        private void OnTagChanged()
        {
            _hashCode = _tagName.GetHashCode();
            var parts = _tagName.Split('.');
            _ancestorHashCode = new int[parts.Length - 1];
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var ancestor = string.Join(".", parts, 0, i + 1);
                _ancestorHashCode[i] = ancestor.GetHashCode();
            }
        }

        private IList<string> TagGetter() => GASEditorData.Instance.Tags;
#endif
    }
}

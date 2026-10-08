using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using GAS.EditorData;
#endif

namespace GAS
{
    /// <summary>
    /// 游戏标签结构体（值类型）。通过字符串的 HashCode 作为唯一标识，
    /// 支持父子层级关系（用于 IsChildOf/IsParentOf 层级匹配）。
    /// Editor 模式下保留字符串名称以供可读性。
    /// </summary>
    [InlineProperty(LabelWidth = 13)]
    [System.Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>
    {
        [HideLabel, SerializeField, ReadOnly, Required]
        private string _shortTagName;
        public string ShortTagName=>_shortTagName;

        [HideInInspector, SerializeField] private int _hashCode;

        [HideInInspector, SerializeField] private int[] _ancestorHashCode;

        /// <summary>标签哈希标识符</summary>
        public int HashCode => _hashCode;

        /// <summary>所有祖先标签的哈希数组（由编辑器根据分隔符层级计算）</summary>
        public int[] AncestorHashCode => _ancestorHashCode;

        /// <summary>当前标签是否为 other 的子标签（即 other 是否在祖先列表中）</summary>
        public bool IsChildOf(GameplayTag other) => _ancestorHashCode.Contains(other._hashCode);

        /// <summary>当前标签是否为 other 的父标签</summary>
        public bool IsParentOf(GameplayTag other) => other._ancestorHashCode.Contains(_hashCode);

        public override int GetHashCode() => _hashCode;
        public bool Equals(GameplayTag tag) => _hashCode == tag._hashCode;
        public override bool Equals(object obj) => obj is GameplayTag tag && _hashCode == tag._hashCode;
        public static bool operator ==(GameplayTag a, GameplayTag b) => a._hashCode == b._hashCode;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a._hashCode != b._hashCode;

#if UNITY_EDITOR
        // 这里不要加 [IgnoreMember]：它是本结构体唯一的可编辑成员（下拉选标签 + 自动重建哈希），
        // 一旦被 Odin 忽略，Inspector 里就只剩只读的 _shortTagName，标签就"配置不了"了
        [Required("标签不能为空", InfoMessageType.Error)]
        [SerializeField, HideLabel, ValueDropdown("TagGetter"), OnValueChanged("OnTagChanged")]
        private string _tagName;

        // [ShowInInspector, HideLabel, ReadOnly] public string ShortTagName => _tagName?.Split(".").Last();

        private void OnTagChanged() => Rebuild(_tagName);

        /// <summary>编辑器用：设置标签名并重建哈希（供 NodeCanvas 等外部 drawer 调用）</summary>
        public void SetTagName(string tagName)
        {
            _tagName = tagName;
            Rebuild(tagName);
        }

        /// <summary>编辑器用：完整标签名</summary>
        public string TagName => _tagName;

        private IList<string> TagGetter() => GASEditorData.Instance.GetTags();
#endif

        /// <summary>
        /// 用完整标签名构建一个 GameplayTag（运行时/编辑器通用）。
        /// 哈希算法与编辑器 <c>OnTagChanged</c> 完全一致，因此与资产里序列化的同名标签相等。
        /// </summary>
        public static GameplayTag FromName(string tagName)
        {
            GameplayTag tag = default;
            tag.Rebuild(tagName);
#if UNITY_EDITOR
            tag._tagName = tagName;
#endif
            return tag;
        }

        /// <summary>按点分层级重建 哈希 / 祖先哈希 / 短名；空名时清空。</summary>
        private void Rebuild(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                _hashCode = 0;
                _ancestorHashCode = Array.Empty<int>();
                _shortTagName = null;
                return;
            }

            _hashCode = tagName.GetHashCode();
            string[] parts = tagName.Split('.');
            _ancestorHashCode = new int[parts.Length - 1];
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string ancestor = string.Join(".", parts, 0, i + 1);
                _ancestorHashCode[i] = ancestor.GetHashCode();
            }

            _shortTagName = parts[parts.Length - 1];
        }
    }
}
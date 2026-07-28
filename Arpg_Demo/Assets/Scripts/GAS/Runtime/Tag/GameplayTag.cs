using MessagePack;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GAS
{
    [MessagePackObject(AllowPrivate = true)]
    public partial struct GameplayTag
    {
        [Key(0)]
        [SerializeField, ReadOnly, LabelText("哈希值")]
        private int _hashCode;
        [Key(1)]
        [SerializeField, ReadOnly, LabelText("祖先的哈希值")]
        private int[] _ancestorHashCode;

        #region 公开属性
        [IgnoreMember]
        public int HashCode => _hashCode;
        [IgnoreMember]
        public int[] AncestorHashCode => _ancestorHashCode;
        #endregion
        #region 重写基本函数
        public override int GetHashCode() => _hashCode;
        public override bool Equals(object obj) => obj is GameplayTag tag && _hashCode == tag._hashCode;
        public static bool operator ==(GameplayTag a, GameplayTag b) => a._hashCode == b._hashCode;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a._hashCode != b._hashCode;
        #endregion
        #region tag判断相关
        public bool IsChildOf(GameplayTag tag)
        {
            foreach (var item in _ancestorHashCode)
            {
                if (item == tag._hashCode)
                    return true;
            }
            return false;
        }
        public bool IsParentOf(GameplayTag tag)
        {
            foreach (var item in tag._ancestorHashCode)
            {
                if (item == _hashCode)
                    return true;
            }
            return false;
        }
        #endregion
        #region Editor
#if UNITY_EDITOR
        [IgnoreMember]
        [SerializeField, OnValueChanged(nameof(OnTagNameChanged)), ValueDropdown(nameof(TagGetter)), LabelText("标签名")]
        private string tagName;
        [IgnoreMember]
        [SerializeField, ReadOnly, LabelText("简称")] private string _shortName;
        [IgnoreMember]
        [SerializeField, ReadOnly, LabelText("祖先标签")] private string[] _ancestorNames;
        private void OnTagNameChanged()
        {
            _hashCode = tagName.GetHashCode();
            string[] ancestorShortName = tagName.Split('.');
            // ����ռ�
            _ancestorHashCode = new int[ancestorShortName.Length - 1];
            _ancestorNames = new string[ancestorShortName.Length - 1];

            string ancestorName = "";
            for (int i = 0; i < ancestorShortName.Length - 1; i++)
            {
                if (i != 0) ancestorName += '.';
                ancestorName += ancestorShortName[i];
                _ancestorHashCode[i] = ancestorName.GetHashCode();
                _ancestorNames[i] = ancestorName;
            }
            // ����鿴
            _shortName = ancestorShortName.Last();
        }

        private IList<string> TagGetter() => GASEditorData.Instance.Tags;
#endif
        #endregion
    }
}
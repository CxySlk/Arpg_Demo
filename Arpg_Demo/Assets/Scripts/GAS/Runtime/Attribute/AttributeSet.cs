using LLKFrame;
using MessagePack;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 属性集合。以属性 HashCode 为键的字典，存储角色所有属性。
    /// 支持对象池回收。
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    public partial class AttributeSet
    {
        [Key(0)]
        [SerializeField] private Dictionary<int, Attribute> _attrDic;

        /// <summary>属性字典（HashCode → Attribute）</summary>
        [IgnoreMember]
        public Dictionary<int, Attribute> AttrDic => _attrDic;

        /// <summary>属性集是否为空</summary>
        [IgnoreMember]
        public bool Empty => _attrDic == null || _attrDic.Count == 0;

        #region 创建、初始化、反初始化

        /// <summary>从对象池获取或新建 AttributeSet 实例</summary>
        public static AttributeSet Create() => PoolSystem.GetOrNewObject<AttributeSet>();

        /// <summary>用给定的属性集合初始化</summary>
        public void Init(HashSet<Attribute> attrSet)
        {
            _attrDic = new Dictionary<int, Attribute>();
            foreach (var attr in attrSet)
                _attrDic.Add(attr.HashCode, attr);
        }

        /// <summary>清空并回收到对象池</summary>
        public void Destroy()
        {
            _attrDic?.Clear();
            this.PushObject();
        }

        #endregion

        /// <summary>根据属性引用检查是否存在</summary>
        public bool HasAttr(Attribute attr) => _attrDic.ContainsKey(attr.HashCode);
        /// <summary>根据属性 HashCode 检查是否存在</summary>
        public bool HasAttr(int attrHashCode) => _attrDic.ContainsKey(attrHashCode);
    }
}

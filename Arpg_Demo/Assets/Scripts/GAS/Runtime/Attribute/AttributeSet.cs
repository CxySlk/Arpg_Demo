using LLKFrame;
using MessagePack;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class AttributeSet
    {
        [Key(0)]
        [SerializeField]
        private Dictionary<int, Attribute> _attrDic = new Dictionary<int, Attribute>();

        #region 公开属性
        [IgnoreMember]
        public Dictionary<int, Attribute> AttrDic => _attrDic;
        [IgnoreMember]
        public bool Empty => _attrDic.Count == 0;
        #endregion
        #region 创建、初始化、反初始化
        /// <summary>工厂模式创建</summary>
        public static AttributeSet Create() => PoolSystem.GetOrNewObject<AttributeSet>();

        /// <summary>初始化，从对象池中取出来后使用时，需要先调用此函数</summary>
        public void Init(HashSet<Attribute> tagSet)
        {
            foreach (Attribute attr in tagSet)
                _attrDic.Add(attr.HashCode, attr);
        }

        /// <summary>销毁对象，放入对象池中，放入对象池时需要调用此函数</summary>
        public void Destroy()
        {
            _attrDic.Clear();
            this.PushObject();
        }
        #endregion
        /// <summary>检查是否有该属性</summary>
        public bool HasAttr(Attribute attr) => _attrDic.ContainsKey(attr.HashCode);
        public bool HasAttr(int attrHashCode) => _attrDic.ContainsKey(attrHashCode);
    }
}
using LLKFrame;
using MessagePack;
using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 固定的标签
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    public partial class GameplayTagSet
    {
        [Key(0)]
        [SerializeField] private HashSet<GameplayTag> _tagSet = new HashSet<GameplayTag>();

        #region 公开属性
        [IgnoreMember]
        public HashSet<GameplayTag> TagSet => _tagSet;
        [IgnoreMember]
        public bool Empty => _tagSet.Count == 0;
        #endregion
        #region 创建、初始化、反初始化
        /// <summary>工厂模式创建</summary>
        public static GameplayTagSet Create() => PoolSystem.GetOrNewObject<GameplayTagSet>();

        /// <summary>初始化，从对象池中取出来后使用时，需要先调用此函数</summary>
        public void Init(HashSet<GameplayTag> tagSet) => _tagSet.AddRange(tagSet);

        /// <summary>销毁对象，放入对象池中，放入对象池时需要调用此函数</summary>
        public void Destroy()
        {
            _tagSet?.Clear();
            this.PushObject();
        }
        #endregion
        #region 标签的增、删、查
        public bool AddTag(GameplayTag tag) => _tagSet.Add(tag);
        public void AddTag(GameplayTagSet tagSet) => _tagSet.AddRange(tagSet._tagSet);
        public bool RemoveTag(GameplayTag tag) => _tagSet.Remove(tag);
        public void RemoveTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet._tagSet)
                RemoveTag(tag);
        }
        /// <summary>检查是否有该标签</summary>
        public bool HasTag(GameplayTag tag) => _tagSet.Contains(tag);
        /// <summary>检查是否有该标签或该标签的祖宗</summary>
        public bool CheckTag(GameplayTag tag)
        {
            if (_tagSet.Contains(tag)) return true;
            foreach (GameplayTag _tag in _tagSet)
                if (tag.IsChildOf(_tag)) return true;
            return false;
        }

        public bool HasAnyTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag _tag in tagSet._tagSet)
                if (HasTag(_tag)) return true;
            return false;
        }
        public bool CheckAnyTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag _tag in tagSet._tagSet)
                if (CheckTag(_tag)) return true;
            return false;
        }

        public bool HasAllTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag _tag in tagSet._tagSet)
                if (!HasTag(_tag)) return false;
            return true;
        }
        public bool CheckAllTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag _tag in tagSet._tagSet)
                if (!CheckTag(_tag)) return false;
            return true;
        }
        #endregion
    }
}
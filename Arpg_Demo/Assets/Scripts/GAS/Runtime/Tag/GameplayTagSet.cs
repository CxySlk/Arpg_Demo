using LLKFrame;
using MessagePack;
using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 游戏标签集合。封装 HashSet&lt;GameplayTag&gt;，提供增删查操作。
    /// 支持对象池回收，使用前需调用 Init，使用后调用 Destroy。
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    public partial class GameplayTagSet
    {
        [Key(0)]
        [SerializeField] private HashSet<GameplayTag> _tagSet = new HashSet<GameplayTag>();

        #region 公开属性

        /// <summary>内部标签集合（只读引用）</summary>
        [IgnoreMember]
        public HashSet<GameplayTag> TagSet => _tagSet;
        /// <summary>标签集合是否为空</summary>
        [IgnoreMember]
        public bool Empty => _tagSet.Count == 0;

        #endregion

        #region 创建、初始化、反初始化

        /// <summary>从对象池获取或新建 GameplayTagSet 实例</summary>
        public static GameplayTagSet Create() => PoolSystem.GetOrNewObject<GameplayTagSet>();

        /// <summary>用给定的标签集合初始化（合并到现有集合中）</summary>
        public void Init(HashSet<GameplayTag> tagSet) => _tagSet.AddRange(tagSet);

        /// <summary>销毁实例，清空标签并回收到对象池</summary>
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
            foreach (var tag in tagSet._tagSet)
                _tagSet.Remove(tag);
        }

        /// <summary>直接检查标签是否在集合中（不检查祖先关系）</summary>
        public bool HasTag(GameplayTag tag) => _tagSet.Contains(tag);
        /// <summary>检查标签或它的任一祖先是否在集合中</summary>
        public bool CheckTag(GameplayTag tag)
        {
            if (_tagSet.Contains(tag)) return true;
            foreach (var t in _tagSet)
                if (tag.IsChildOf(t)) return true;
            return false;
        }

        /// <summary>检查集合中是否存在传入标签集的任一标签</summary>
        public bool HasAnyTag(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet._tagSet)
                if (_tagSet.Contains(tag)) return true;
            return false;
        }

        /// <summary>检查集合中是否存在传入标签集的任一标签（含祖先匹配）</summary>
        public bool CheckAnyTag(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet._tagSet)
                if (CheckTag(tag)) return true;
            return false;
        }

        /// <summary>检查集合中是否包含传入标签集的所有标签</summary>
        public bool HasAllTag(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet._tagSet)
                if (!_tagSet.Contains(tag)) return false;
            return true;
        }

        /// <summary>检查集合中是否包含传入标签集的所有标签（含祖先匹配）</summary>
        public bool CheckAllTag(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet._tagSet)
                if (!CheckTag(tag)) return false;
            return true;
        }

        #endregion
    }
}

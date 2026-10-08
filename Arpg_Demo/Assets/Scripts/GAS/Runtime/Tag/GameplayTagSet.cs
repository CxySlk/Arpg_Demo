using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 游戏标签集合。封装 List&lt;GameplayTag&gt;，提供增删查操作。
    /// 用 List 而不是 HashSet：HashSet 不是 Unity 可序列化类型，
    /// Odin 的 Inspector 会把这个成员整个过滤掉（面板里标签集是空的、配置不了），
    /// 也无法被 Unity 原生序列化。去重语义由 AddTag 自己保证。
    /// </summary>
    [System.Serializable]
    public class GameplayTagSet
    {
        [SerializeField, HideLabel]
        [ListDrawerSettings(ShowItemCount = true)]
        public List<GameplayTag> tagSet = new();

        /// <summary>
        /// 标签列表（懒加载保证不为 null）。
        /// Odin 反序列化不走构造函数，老资产的 HashSet 节点读进 List 字段会留下 null，
        /// 所以运行时统一从这里取，避免遍历标签集时 NRE。Inspector 改的仍是 tagSet 字段本身。
        /// </summary>
        public List<GameplayTag> Tags => tagSet ??= new List<GameplayTag>();

        public bool Empty => Tags.Count == 0; // 标签集合是否为空

        public GameplayTagSet() { }
        public GameplayTagSet(GameplayTagSet tagSet) => this.tagSet.AddRange(tagSet.Tags);

        #region 标签的增、删、查

        /// <summary>添加标签。已存在则返回 false（保持原 HashSet 的去重语义）</summary>
        public bool AddTag(GameplayTag tag)
        {
            if (Tags.Contains(tag)) return false;
            Tags.Add(tag);
            return true;
        }

        public void AddTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                AddTag(tag);
        }

        public bool RemoveTag(GameplayTag tag) => Tags.Remove(tag);

        public void RemoveTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                Tags.Remove(tag);
        }

        /// <summary>直接检查标签是否在集合中（不检查祖先关系）</summary>
        public bool HasTag(GameplayTag tag) => Tags.Contains(tag);

        /// <summary>检查标签或它的任意祖先是否在集合中</summary>
        public bool CheckTag(GameplayTag tag)
        {
            if (Tags.Contains(tag)) return true;
            foreach (GameplayTag t in Tags)
                if (tag.IsChildOf(t)) return true;
            return false;
        }

        /// <summary>检查集合中是否存在传入标签集的任意标签</summary>
        public bool HasAnyTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (Tags.Contains(tag)) return true;
            return false;
        }

        /// <summary>检查集合中是否包含传入标签集的所有标签</summary>
        public bool HasAllTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (!Tags.Contains(tag)) return false;
            return true;
        }

        /// <summary>检查集合中是否存在传入标签集的任意标签（含祖先匹配）</summary>
        public bool CheckAnyTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (CheckTag(tag)) return true;
            return false;
        }

        /// <summary>检查集合中是否包含传入标签集的所有标签（含祖先匹配）</summary>
        public bool CheckAllTag(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (!CheckTag(tag)) return false;
            return true;
        }

        #endregion
    }
}

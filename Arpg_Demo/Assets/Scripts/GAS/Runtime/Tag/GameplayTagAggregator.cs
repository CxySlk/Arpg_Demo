using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 标签聚合器——合并固定标签和动态标签的统一查询入口。
    /// 固定标签来自 ASC 的 Preset，动态标签由 Effect 的 GrantedTags/RemovedTags 控制。
    /// 标签变化时通过 Action 回调通知外部（替代原来的 ASC 反向引用）。
    /// </summary>
    public class GameplayTagAggregator
    {
        /// <summary>动态标签及引用计数。移除时减到 0 才真正删除</summary>
        [ShowInInspector, DictionaryDrawerSettings(KeyLabel = "标签", ValueLabel = "数量")]
        private readonly Dictionary<GameplayTag, int> _dynamicTags = new();

        /// <summary>固定标签（来自 Preset，不可通过 Effect 移除）</summary>
        [SerializeField, ReadOnly] private GameplayTagSet _fixedTag;

        /// <summary>标签变化时调用的外部回调</summary>
        private Action _onTagDirty;

        /// <summary>初始化聚合器，设置固定标签和脏回调</summary>
        public GameplayTagAggregator()
        {
            _fixedTag = new GameplayTagSet();
        }

        public GameplayTagAggregator(GameplayTagSet fixedTagSet) => _fixedTag = new GameplayTagSet(fixedTagSet);

        #region 固定标签增删

        public void AddFixedTag(GameplayTag tag)
        {
            _fixedTag.AddTag(tag);
        }

        public void AddFixedTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                AddFixedTag(tag);
        }

        public bool RemoveFixedTag(GameplayTag tag) => _fixedTag.RemoveTag(tag);

        public void RemoveFixedTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                RemoveFixedTag(tag);
        }

        #endregion

        #region 动态标签增删

        /// <summary>尝试添加动态标签。如固定标签已有则跳过，否则引用计数 +1</summary>
        public void AddDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return;

            if (_dynamicTags.TryGetValue(tag, out int tagCount))
                _dynamicTags[tag] = tagCount + 1;
            else _dynamicTags.Add(tag, 1);
        }

        /// <summary>按完整标签名添加动态标签（运行时/编辑器通用，内部转成 GameplayTag）。空名忽略。</summary>
        public void AddDynamicTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return;
            AddDynamicTag(GameplayTag.FromName(tagName));
        }

        public void AddDynamicTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                AddDynamicTag(tag);
        }

        /// <summary>尝试移除动态标签。引用计数减到 0 时真正删除。固定标签不可移除</summary>
        public void RemoveDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return;

            if (_dynamicTags.TryGetValue(tag, out int tagCount))
            {
                tagCount--;
                if (tagCount <= 0) _dynamicTags.Remove(tag);
                else _dynamicTags[tag] = tagCount;
            }
        }

        /// <summary>按完整标签名移除动态标签。空名忽略。</summary>
        public void RemoveDynamicTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return;
            RemoveDynamicTag(GameplayTag.FromName(tagName));
        }

        public void RemoveDynamicTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                RemoveDynamicTag(tag);
        }

        #endregion

        #region 标签检查

        /// <summary>检查单一标签是否存在（先查固定标签，再查动态标签，含子标签匹配）</summary>
        public bool CheckTag(GameplayTag tag)
        {
            if (_fixedTag.CheckTag(tag)) return true;
            foreach (GameplayTag _tag in _dynamicTags.Keys)
            {
                if (tag == _tag || tag.IsChildOf(_tag))
                    return true;
            }

            return false;
        }

        /// <summary>按完整标签名检查单一标签是否存在。空名返回 false。</summary>
        public bool CheckTag(string tagName) =>
            !string.IsNullOrEmpty(tagName) && CheckTag(GameplayTag.FromName(tagName));

        /// <summary>检查传入标签集中是否有任一标签存在</summary>
        public bool CheckAnyTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (CheckTag(tag))
                    return true;
            return false;
        }

        /// <summary>检查传入标签集中是否所有标签都存在</summary>
        public bool CheckAllTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.Tags)
                if (!CheckTag(tag))
                    return false;
            return true;
        }

        #endregion
    }
}
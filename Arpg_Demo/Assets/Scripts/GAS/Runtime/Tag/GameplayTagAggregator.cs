using LLKFrame;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 标签聚合器——合并固定标签和动态标签的统一查询入口。
    /// 固定标签来自 ASC 的 Preset，动态标签由 Effect 的 GrantTags/RemovedTags 控制。
    /// 标签变化时通过 Action 回调通知外部（替代原来的 ASC 反向引用）。
    /// </summary>
    public class GameplayTagAggregator
    {
        /// <summary>动态标签及引用计数。移除时减到 0 才真正删除</summary>
        [ShowInInspector, DictionaryDrawerSettings(KeyLabel = "标签", ValueLabel = "数量")]
        private readonly Dictionary<GameplayTag, int> _dynamicTags = new Dictionary<GameplayTag, int>();
        /// <summary>固定标签（来自 Preset，不可通过 Effect 移除）</summary>
        [SerializeField, ReadOnly]
        private GameplayTagSet _fixedTag;

        /// <summary>标签变化时调用的外部回调</summary>
        private Action _onTagDirty;

        #region 生命周期

        /// <summary>从对象池获取或新建 GameplayTagAggregator 实例</summary>
        public static GameplayTagAggregator Create() => PoolSystem.GetOrNewObject<GameplayTagAggregator>();

        /// <summary>初始化聚合器，设置固定标签和脏回调</summary>
        public void Init(GameplayTagSet tagSet, Action onTagDirty)
        {
            _onTagDirty = onTagDirty;
            _fixedTag = GameplayTagSet.Create();
            _fixedTag.Init(tagSet.TagSet);
        }

        /// <summary>清空并回收到对象池</summary>
        public void Destroy()
        {
            _dynamicTags.Clear();
            _fixedTag.Destroy();
            this.PushObject();
        }

        #endregion

        private void OnTagIsDirty() => _onTagDirty?.Invoke();

        #region 固定标签增删

        private bool TryAddFixedTag(GameplayTag tag)
        {
            bool tagIsDirty = _fixedTag.AddTag(tag);
            tagIsDirty |= _dynamicTags.Remove(tag);
            return tagIsDirty;
        }

        public void AddFixedTag(GameplayTag tag)
        {
            if (TryAddFixedTag(tag)) OnTagIsDirty();
        }

        public void AddFixedTags(GameplayTagSet tagSet)
        {
            bool isTagDirty = false;
            foreach (var tag in tagSet.TagSet)
                isTagDirty |= TryAddFixedTag(tag);
            if (isTagDirty) OnTagIsDirty();
        }

        private bool TryRemoveFixedTag(GameplayTag tag)
        {
            bool isTagDirty = _fixedTag.RemoveTag(tag);
            isTagDirty |= _dynamicTags.Remove(tag);
            return isTagDirty;
        }

        public void RemoveFixedTag(GameplayTag tag)
        {
            if (TryRemoveFixedTag(tag)) OnTagIsDirty();
        }

        public void RemoveFixedTags(GameplayTagSet tagSet)
        {
            bool isTagDirty = false;
            foreach (var tag in tagSet.TagSet)
                isTagDirty |= TryRemoveFixedTag(tag);
            if (isTagDirty) OnTagIsDirty();
        }

        #endregion

        #region 动态标签增删

        /// <summary>尝试添加动态标签。如固定标签已有则跳过，否则引用计数 +1</summary>
        private bool TryAddDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return false;

            if (_dynamicTags.TryGetValue(tag, out int tagCount))
                _dynamicTags[tag] = tagCount + 1;
            else
            {
                _dynamicTags.Add(tag, 1);
                return true;
            }
            return false;
        }

        public void AddDynamicTag(GameplayTag tag)
        {
            if (TryAddDynamicTag(tag)) OnTagIsDirty();
        }

        public void AddDynamicTags(GameplayTagSet tagSet)
        {
            bool tagIsDirty = false;
            foreach (var tag in tagSet.TagSet)
                tagIsDirty |= TryAddDynamicTag(tag);
            if (tagIsDirty) OnTagIsDirty();
        }

        /// <summary>尝试移除动态标签。引用计数减到 0 时真正删除。固定标签不可移除</summary>
        private bool TryRemoveDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return false;

            if (_dynamicTags.TryGetValue(tag, out int tagCount))
            {
                tagCount--;
                if (tagCount <= 0)
                {
                    _dynamicTags.Remove(tag);
                    return true;
                }
                _dynamicTags[tag] = tagCount;
            }
            return false;
        }

        public void RemoveDynamicTag(GameplayTag tag)
        {
            if (TryRemoveDynamicTag(tag)) OnTagIsDirty();
        }

        public void RemoveDynamicTags(GameplayTagSet tagSet)
        {
            bool tagIsDirty = false;
            foreach (var tag in tagSet.TagSet)
                tagIsDirty |= TryRemoveDynamicTag(tag);
            if (tagIsDirty) OnTagIsDirty();
        }

        #endregion

        #region 标签检查

        /// <summary>检查单一标签是否存在（先查固定标签，再查动态标签，含子标签匹配）</summary>
        public bool CheckTag(GameplayTag tag)
        {
            if (_fixedTag.CheckTag(tag)) return true;
            foreach (var kv in _dynamicTags)
            {
                if (tag == kv.Key || tag.IsChildOf(kv.Key))
                    return true;
            }
            return false;
        }

        /// <summary>检查传入标签集中是否有任一标签存在</summary>
        public bool CheckAnyTags(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet.TagSet)
                if (CheckTag(tag)) return true;
            return false;
        }

        /// <summary>检查传入标签集中是否所有标签都存在</summary>
        public bool CheckAllTags(GameplayTagSet tagSet)
        {
            foreach (var tag in tagSet.TagSet)
                if (!CheckTag(tag)) return false;
            return true;
        }

        #endregion
    }
}

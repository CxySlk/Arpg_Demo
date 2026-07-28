using LLKFrame;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public class GameplayTagAggregator
    {
        // 拥有者
        private AbilitySystemComponent _owner;
        // 动态标签
        [ShowInInspector, DictionaryDrawerSettings(KeyLabel = "标签", ValueLabel = "数量")]
        private Dictionary<GameplayTag, int> _dynamicTags = new Dictionary<GameplayTag, int>();
        // 固定标签
        [SerializeField, ReadOnly]
        private GameplayTagSet _fixedTag;
        #region 生命周期相关
        public static GameplayTagAggregator Create() => PoolSystem.GetOrNewObject<GameplayTagAggregator>();
        public void Init(AbilitySystemComponent owner, GameplayTagSet tagSet)
        {
            _owner = owner;
            _fixedTag = GameplayTagSet.Create();
            _fixedTag.Init(tagSet.TagSet);
        }

        public void Destroy()
        {
            _dynamicTags.Clear();
            _fixedTag.Destroy();
            this.PushObject();
        }
        #endregion

        private void OnTagIsDirty()
        {
            // TODO：标签发生了改变，AbilitySystemComponent的Effect和Ability需要进行检查
            _owner.TagIsDirty();
        }
        #region 增加固定标签
        private bool TryAddFixedTag(GameplayTag tag)
        {
            bool tagIsDirty = _fixedTag.AddTag(tag);
            tagIsDirty = tagIsDirty || _dynamicTags.Remove(tag);
            return tagIsDirty;
        }

        public void AddFixedTag(GameplayTag tag)
        {
            if (TryAddFixedTag(tag)) OnTagIsDirty();
        }

        public void AddFixedTags(GameplayTagSet tagSet)
        {
            bool isTagDirty = false;
            foreach (GameplayTag tag in tagSet.TagSet)
                isTagDirty |= TryAddFixedTag(tag);
            if (isTagDirty) OnTagIsDirty();
        }
        #endregion
        #region 移除固定标签
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
            foreach (GameplayTag _tag in tagSet.TagSet)
                isTagDirty |= TryRemoveFixedTag(_tag);
            if (isTagDirty) OnTagIsDirty();
        }
        #endregion
        #region 增加动态标签
        private bool TryAddDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return false;

            bool tagIsDirty = false;
            if (_dynamicTags.TryGetValue(tag, out int tagCount))
                _dynamicTags[tag] = tagCount + 1;
            else
            {
                tagIsDirty = true;
                tagCount = 1;
                _dynamicTags.Add(tag, tagCount);
            }
            return tagIsDirty;
        }
        public void AddDynamicTag(GameplayTag tag)
        {
            if (TryAddDynamicTag(tag)) OnTagIsDirty();
        }
        public void AddDynamicTags(GameplayTagSet tagSet)
        {
            bool tagIsDirty = false;
            foreach (GameplayTag tag in tagSet.TagSet)
                tagIsDirty |= TryAddDynamicTag(tag);
            if (tagIsDirty) OnTagIsDirty();
        }
        #endregion
        #region 移除动态标签
        private bool TryRemoveDynamicTag(GameplayTag tag)
        {
            if (_fixedTag.HasTag(tag)) return false;

            bool tagIsDirty = false;
            if (_dynamicTags.TryGetValue(tag, out int tagCount))
            {
                tagCount--;
                _dynamicTags[tag] = tagCount;
                if (tagCount <= 0)
                {
                    tagIsDirty = true;
                    _dynamicTags.Remove(tag);
                }
            }
            return tagIsDirty;
        }
        public void RemoveDynamicTag(GameplayTag tag)
        {
            if (TryRemoveDynamicTag(tag)) OnTagIsDirty();
        }
        public void RemoveDynamicTags(GameplayTagSet tagSet)
        {
            bool tagIsDirty = false;
            foreach (GameplayTag tag in tagSet.TagSet)
                tagIsDirty |= TryRemoveDynamicTag(tag);
            if (tagIsDirty) OnTagIsDirty();
        }
        #endregion
        #region 检查标签
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

        public bool CheckAnyTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.TagSet)
                if (CheckTag(tag)) return true;
            return false;
        }

        public bool CheckAllTags(GameplayTagSet tagSet)
        {
            foreach (GameplayTag tag in tagSet.TagSet)
                if (!CheckTag(tag)) return false;
            return true;
        }
        #endregion
    }
}
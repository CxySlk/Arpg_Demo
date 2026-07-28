using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    #region 枚举类型
    /// <summary>效果持续类型</summary>
    public enum EffectDurationType
    {
        [LabelText("立即")] Instance,
        [LabelText("永久")] Infinite,
        [LabelText("持续一段时间")] Durational
    }
    #endregion
    public class GameplayEffect
    {
        public EntityId AssetEntityID { get; private set; }
        #region 效果持续方式
        public EffectDurationType DurationType { get; set; }
        public GameplayEffectStackData StackData { get; set; }
        #endregion
        #region 时间相关
        public int DurationFrame { get; private set; }
        public int PeriodicFrame { get; private set; }
        #endregion
        #region 标签相关
        public GameplayTagSet RequiredTags { get; private set; }
        public GameplayTagSet BlockedTags { get; private set; }
        public GameplayTagSet GrantTags { get; private set; }
        public GameplayTagSet RemovedTags { get; private set; }
        #endregion
        #region 属性修饰器
        public GameplayEffectAttrModifier[] AttrModifiers { get; private set; }
        #endregion
        #region Cue表现层相关
        public GameplayCue[] OnExcuteCues { get; private set; } 
        public GameplayCue[] OnAddCues { get; private set; } 
        #endregion

        public static GameplayEffect Create() => PoolSystem.GetOrNewObject<GameplayEffect>();
        public void Init(GameplayEffectAsset asset)
        {
            AssetEntityID = asset.GetEntityId();

            DurationType = asset.DurationType;
            StackData = asset.StackData;
            DurationFrame = (int)(asset.DurationSeconds * GASTimer.FrameRate);
            PeriodicFrame = asset.PeriodicSeconds <= 0 ? -1 : (int)(asset.PeriodicSeconds * GASTimer.FrameRate);

            RequiredTags = GameplayTagSet.Create();
            if (asset.RequiredTags != null)
                RequiredTags.Init(asset.RequiredTags.TagSet);

            BlockedTags = GameplayTagSet.Create();
            if (asset.BlockedTags != null)
                BlockedTags.Init(asset.BlockedTags.TagSet);

            GrantTags = GameplayTagSet.Create();
            if (asset.GrantTags != null)
                GrantTags.Init(asset.GrantTags.TagSet);

            RemovedTags = GameplayTagSet.Create();
            if (asset.RemovedTags != null)
                RemovedTags.Init(asset.RemovedTags.TagSet);

            AttrModifiers = asset.AttrModifiers != null ? new GameplayEffectAttrModifier[asset.AttrModifiers.Length] : null;
            if (AttrModifiers != null)
                for (int i = 0; i < AttrModifiers.Length; i++)
                {
                    AttrModifiers[i] = GameplayEffectAttrModifier.Create();
                    AttrModifiers[i].Init(asset.AttrModifiers[i]);
                }

            // Cue相关
            OnExcuteCues = asset.OnExcuteCues;
            OnAddCues = asset.OnAddCues;
        }
        public void Destroy()
        {
            RequiredTags?.Destroy();
            BlockedTags?.Destroy();
            GrantTags?.Destroy();
            RemovedTags?.Destroy();
            if (AttrModifiers != null)
                for (int i = 0; i < AttrModifiers.Length; i++)
                {
                    AttrModifiers[i].Destroy();
                }
            this.PushObject();
        }
    }
}

using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    #region 枚举类型

    /// <summary>效果持续类型</summary>
    public enum EffectDurationType
    {
        /// <summary>立即生效（修改 BaseValue，不创建 Spec）</summary>
        [LabelText("立即")] Instance,
        /// <summary>永久生效（修改 CurValue，不自动过期）</summary>
        [LabelText("永久")] Infinite,
        /// <summary>持续一段时间（到期后自动移除）</summary>
        [LabelText("持续一段时间")] Durational
    }

    #endregion

    /// <summary>
    /// GameplayEffect 运行时数据对象（从 GameplayEffectAsset 配置创建）。
    /// 将 Asset 的秒数转换为帧数，深拷贝 TagSet/Modifier 等配置数据。
    /// 支持对象池回收。
    /// </summary>
    public class GameplayEffect
    {
        /// <summary>效果唯一标识符（来自 Asset 的 EntityId）</summary>
        public EntityId AssetEntityID { get; private set; }

        #region 效果持续方式

        public EffectDurationType DurationType { get; set; }
        public GameplayEffectStackData StackData { get; set; }

        #endregion

        #region 时间相关

        /// <summary>持续时间（帧数）</summary>
        public int DurationFrame { get; private set; }
        /// <summary>周期触发间隔（帧数，<=0 表示无周期）</summary>
        public int PeriodicFrame { get; private set; }

        #endregion

        #region 标签相关

        public GameplayTagSet RequiredTags { get; private set; }
        public GameplayTagSet BlockedTags { get; private set; }
        public GameplayTagSet GrantTags { get; private set; }
        public GameplayTagSet RemovedTags { get; private set; }

        #endregion

        #region 属性修饰器

        /// <summary>属性修改器数组</summary>
        public GameplayEffectAttrModifier[] AttrModifiers { get; private set; }

        #endregion

        #region 生命周期相关

        /// <summary>从对象池获取或新建 GameplayEffect 实例</summary>
        public static GameplayEffect Create() => PoolSystem.GetOrNewObject<GameplayEffect>();

        /// <summary>
        /// 从 Asset 配置初始化运行时数据。
        /// 复制标签集（深拷贝）、克隆属性修改器、引用 Cue 数组。
        /// </summary>
        public void Init(GameplayEffectAsset asset)
        {
            AssetEntityID = asset.GetEntityId();

            DurationType = asset.DurationType;
            StackData = asset.StackData;
            DurationFrame = (int)(asset.DurationSeconds * GASTimer.FrameRate);
            PeriodicFrame = asset.PeriodicSeconds <= 0 ? -1 : (int)(asset.PeriodicSeconds * GASTimer.FrameRate);

            // 深拷贝标签集
            RequiredTags = GameplayTagSet.Create();
            if (asset.RequiredTags != null) RequiredTags.Init(asset.RequiredTags.TagSet);

            BlockedTags = GameplayTagSet.Create();
            if (asset.BlockedTags != null) BlockedTags.Init(asset.BlockedTags.TagSet);

            GrantTags = GameplayTagSet.Create();
            if (asset.GrantTags != null) GrantTags.Init(asset.GrantTags.TagSet);

            RemovedTags = GameplayTagSet.Create();
            if (asset.RemovedTags != null) RemovedTags.Init(asset.RemovedTags.TagSet);

            // 克隆属性修改器
            AttrModifiers = GameplayEffectAttrModifier.CopyArray(asset.AttrModifiers);
            if (AttrModifiers != null)
                for (int i = 0; i < AttrModifiers.Length; i++)
                {
                    AttrModifiers[i] = GameplayEffectAttrModifier.Create();
                    AttrModifiers[i].Init(asset.AttrModifiers[i]);
                }

            // Cue 直接引用 Asset 上的数组
            OnExcuteCues = asset.OnExcuteCues;
            OnAddCues = asset.OnAddCues;
        }

        /// <summary>销毁效果对象，释放标签集和修改器，回收到对象池</summary>
        public void Destroy()
        {
            RequiredTags?.Destroy();
            BlockedTags?.Destroy();
            GrantTags?.Destroy();
            RemovedTags?.Destroy();
            RequiredTags = null;
            BlockedTags = null;
            GrantTags = null;
            RemovedTags = null;


            if (AttrModifiers != null)
                for (int i = 0; i < AttrModifiers.Length; i++)
                    AttrModifiers[i].Destroy();
            AttrModifiers = null;
            this.PushObject();
        }

        #endregion

        #region Cue表现层

        /// <summary>效果执行时触发的 Cue</summary>
        public GameplayCue[] OnExcuteCues { get; private set; }
        /// <summary>效果添加时触发的 Cue</summary>
        public GameplayCue[] OnAddCues { get; private set; }

        #endregion
    }
}

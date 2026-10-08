using LLKFrame;
using Sirenix.OdinInspector;

namespace GAS
{
    /// <summary>
    /// GameplayCue 模板基类（普通类，非资产）。
    /// 负责标签条件（判断能否运行）以及创建对应 Spec 运行时实例。
    /// </summary>
    [System.Serializable]
    public abstract class GameplayCue : ITagCondition
    {
        [LabelText("需要的标签")] public GameplayTagSet requiredTags = new();
        [LabelText("阻塞的标签")] public GameplayTagSet blockedTags = new();
        public GameplayTagSet RequiredTags => requiredTags;
        public GameplayTagSet BlockedTags => blockedTags;

        /// <summary>创建对应的 Spec 运行时实例（对象池）</summary>
        protected abstract GameplayCueSpec CreateSpec();

        /// <summary>触发入口：标签满足则创建 Spec 并运行</summary>
        public GameplayCueSpec Apply(AbilitySystemComponent target)
        {
            if (!target || !target.CheckCanRelease(this)) return null;
            GameplayCueSpec spec = CreateSpec();
            spec.Init(this, target);
            spec.Apply();
            return spec;
        }
    }
}
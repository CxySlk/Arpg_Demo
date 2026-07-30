using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// GameplayCue 抽象基类——表现层触发入口。
    /// 定义 RequireTags/BlockedTags 条件过滤，以及 ApplyFrom 触发接口。
    /// </summary>
    public abstract class GameplayCue : ConfigBase
    {
        [SerializeField, LabelText("需要的标签")]
        private GameplayTagSet _requireTags;
        public GameplayTagSet RequireTags => _requireTags;

        [SerializeField, LabelText("阻塞的标签")]
        private GameplayTagSet _blockedTags;
        public GameplayTagSet BlockedTags => _blockedTags;

        /// <summary>以指定 ASC 为上下文中触发 Cue</summary>
        public abstract void ApplyFrom(AbilitySystemComponent asc);

        /// <summary>
        /// 检查 ASC 是否满足该 Cue 的触发条件（RequiredTags 全部存在，BlockedTags 全部不存在）。
        /// </summary>
        public virtual bool Triggerable(AbilitySystemComponent asc)
        {
            if (_requireTags != null && !_requireTags.Empty && !asc.CheckAllTags(_requireTags)) return false;
            if (_blockedTags != null && !_blockedTags.Empty && asc.CheckAnyTags(_blockedTags)) return false;
            return true;
        }
    }

    /// <summary>
    /// 泛型 Cue 基类。引入 CreateSpec() 工厂方法，创建对应的 Spec 运行时实例。
    /// </summary>
    public abstract class GameplayCue<TSpec> : GameplayCue
        where TSpec : GameplayCueSpec
    {
        /// <summary>创建该 Cue 的类型化 Spec 运行时实例</summary>
        public abstract TSpec CreateSpec();
    }
}

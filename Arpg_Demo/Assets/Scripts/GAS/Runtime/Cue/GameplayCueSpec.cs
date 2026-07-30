using LLKFrame;

namespace GAS
{
    /// <summary>
    /// GameplayCueSpec 抽象基类——Cue 的运行时执行上下文。
    /// 持有所有者 ASC 引用，由 Cue.ApplyFrom 创建并驱动执行。
    /// </summary>
    public abstract class GameplayCueSpec
    {
        /// <summary>效果的拥有者 ASC</summary>
        public AbilitySystemComponent Owner { get; protected set; }

        /// <summary>用 Cue 资产和所有者初始化 Spec</summary>
        public abstract void Init(GameplayCue cue, AbilitySystemComponent owner);
    }

    /// <summary>
    /// 泛型 Spec 基类。提供类型化的 Cue 访问。
    /// </summary>
    public abstract class GameplayCueSpec<TCue> : GameplayCueSpec
        where TCue : GameplayCue
    {
        protected TCue Cue;

        public override void Init(GameplayCue cue, AbilitySystemComponent owner)
        {
            Cue = cue as TCue;
            Owner = owner;
        }

        public virtual void Destroy()
        {
            Cue = null;
            Owner = null;
            this.PushObject();
        }
    }
}

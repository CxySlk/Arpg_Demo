using Sirenix.OdinInspector;

namespace GAS
{
    /// <summary>
    /// 瞬时效果：配置(标签条件、Cue 列表)与运行时(表现)合并在一起。
    /// <para>对外只有 <see cref="Execute"/> 一个入口：它会拷贝一份运行时实例、绑定来源与目标，再执行表现。</para>
    /// </summary>
    public abstract class GameplayInstantEffect : GameplayEffect
    {
        /// <summary>应用效果（唯一对外入口）：拷贝配置 → 绑定来源/目标 → 执行表现。</summary>
        internal virtual void Execute(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (!target.CheckCanRelease(this)) return;
            GameplayInstantEffect runtime = (GameplayInstantEffect)Copy();
            runtime.Init(source, target);
            runtime.ApplyEffect();
        }

        /// <summary>实际表现（子类覆写；跑在拷贝出来的运行时实例上，source / target 已就绪）。</summary>
        protected abstract void ApplyEffect();
    }
}

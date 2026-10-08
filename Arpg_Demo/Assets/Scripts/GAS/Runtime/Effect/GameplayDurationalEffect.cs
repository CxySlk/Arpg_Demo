using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAS
{
    /// <summary>
    /// 持续类效果：配置与运行时一体（在技能配置 / ASC 预设里内联配置，运行时 Copy 一份使用）。
    /// <para>对外只有一个入口：<see cref="Execute"/>。其余 <see cref="Enter"/> / <see cref="OnUpdate"/> /
    /// <see cref="Exit"/> 都是内部流程，由 ASC 在自己的生命周期里调用（OnUpdate 由 ASC 每帧驱动，
    /// 效果从效果表里移除后 ASC 就不会再调用它）。</para>
    /// <para>计时用秒（<see cref="durationSeconds"/> / <see cref="periodicSeconds"/>），不再用帧：
    /// 暂停与时间缩放由 TimeSystem 统一改 Time.timeScale，所以这里直接用 deltaTime 就自带暂停/慢动作。</para>
    /// <para>表现(Cue)不在这里配 —— 见 <see cref="GameplayCueEffect"/>。</para>
    /// </summary>
    public abstract class GameplayDurationalEffect : GameplayEffect
    {
        #region 效果配置

        [InfoBox("在此配置效果的标签条件、持续方式与时间参数。")]
        [BoxGroup("基础信息")]
        [LabelText("效果标识")]
        public GameplayTag effectTag;

        [BoxGroup("持续方式")]
        [LabelText("永久生效")]
        public bool isInfinite;

        [BoxGroup("持续方式")]
        [LabelText("叠层配置")]
        // ShowInInspector：readonly 字段 Unity/Odin 默认不生成属性，这里强制显示（内部字段可改）
        [ShowInInspector]
        public readonly GameplayEffectStackData stackData = new();

        [BoxGroup("时间相关")]
        [LabelText("持续时长（秒）"), ShowIf("@isInfinite == false")]
        public float durationSeconds;

        [BoxGroup("时间相关")]
        [FormerlySerializedAs("PeriodicSeconds")] [LabelText("触发周期（秒）")]
        public float periodicSeconds;

        [BoxGroup("时间相关")]
        [FormerlySerializedAs("BeginApplyOnce")] [LabelText("开始立即生效")]
        public bool beginApplyOnce;

        #endregion

        #region 运行时数据

        /// <summary>当前层数</summary>
        protected int effectStack;

        /// <summary>这次持续已经过去多久（秒）</summary>
        protected float elapsedTime;

        /// <summary>周期计时（秒，保留余数避免时间漂移）</summary>
        private float _periodTimer;

        protected int EffectStack
        {
            get => effectStack;
            set
            {
                value = Mathf.Clamp(value, 0, stackData.MaxStackCount + 1);
                if (effectStack == value) return;
                effectStack = value;
                // 层数归零 = 效果结束；否则只是广播层数变化
                if (value == 0) Exit();
                else target.EffectStackChanged(this, value);
            }
        }

        #endregion

        #region 只读状态

        public GameplayTag EffectTag => effectTag; // 效果标识（同类效果按它判重）
        public bool IsInfinite => isInfinite;
        public int StackCount => effectStack;

        /// <summary>剩余时长（秒）。永久效果没有"到期"，返回 0。</summary>
        public float RemainTime => isInfinite ? 0f : Mathf.Max(0f, durationSeconds - elapsedTime);

        /// <summary>剩余时长的归一化值（1 = 刚挂上，0 = 即将到期）。永久效果恒为 1。</summary>
        public float RemainNormalizeTime =>
            isInfinite
                ? 1f
                : Mathf.Clamp01(RemainTime / durationSeconds);

        #endregion

        #region 对外唯一入口

        /// <summary>
        /// 应用这个效果：
        /// 目标身上没有同类效果（按 <see cref="effectTag"/> 判重）→ 拷贝一份配置作为运行时实例并进入；
        /// 已经有了 → 叠层，并按叠层规则决定是否刷新持续时间。
        /// </summary>
        /// <param name="source">效果来源（谁造成的）</param>
        /// <param name="target">效果目标（挂在谁身上）</param>
        /// <param name="stack">本次叠几层</param>
        public virtual void Execute(AbilitySystemComponent source, AbilitySystemComponent target, int stack = 1)
        {
            if (target == null || !target.CheckCanRelease(this)) return;

            // 已经有同类效果 → 叠层（不新建实例，时间按叠层规则处理）
            if (target.TryGetEffect(effectTag, out GameplayDurationalEffect existEffect))
            {
                existEffect.IncreaseStack(stack);
                return;
            }

            // 首次进入 → 拷贝一份配置作为运行时实例
            GameplayDurationalEffect runtime = (GameplayDurationalEffect)Copy();
            runtime.Init(source, target);
            runtime.Enter();
            runtime.IncreaseStack(stack);

            // 开始立即生效
            if (beginApplyOnce && runtime.target.CheckCanRelease(runtime)) runtime.ApplyEffect();
        }

        #endregion

        #region 内部流程（由 ASC 调用）

        /// <summary>
        /// 进入效果：登记到目标身上（加减标签 + 进效果表 + 广播）。
        /// 计时在 <see cref="Init"/> 里已经归零，这里不重置时间，避免组件重新启用时把持续时间刷掉。
        /// </summary>
        internal void Enter()
        {
            target.AddEffect(this);
        }

        /// <summary>
        /// 每帧由 ASC 驱动（<paramref name="deltaTime"/> 已包含暂停与时间缩放）。
        /// 到时间就降层/清空，到周期就试着结算一次。
        /// </summary>
        internal void OnUpdate(float deltaTime)
        {
            elapsedTime += deltaTime;

            // 时间到：层数减一层（降层方式决定是清空移除还是降层刷新）
            if (!IsInfinite && elapsedTime >= durationSeconds)
            {
                DecreaseStack(1);
                return;
            }

            if (periodicSeconds <= 0f) return;

            _periodTimer += deltaTime;
            if (_periodTimer < periodicSeconds) return;

            // 保留余数：一帧最多结算一次，长时间卡顿不会把周期一次性补爆
            _periodTimer -= periodicSeconds;
            if (target.CheckCanRelease(this)) ApplyEffect();
        }

        /// <summary>结束效果：摘掉标签并从效果表移除（移除后 ASC 不会再驱动 OnUpdate）。</summary>
        internal void Exit()
        {
            target.RemoveEffect(this);
        }

        /// <summary>叠层（叠层规则决定要不要刷新持续时间）。</summary>
        internal void IncreaseStack(int stack)
        {
            EffectStack += stack;

            switch (stackData.AggregateType)
            {
                case EffectStackAggregateType.OnlyStack:
                    // 只有叠满时才刷新持续时间
                    if (effectStack == stackData.MaxStackCount) RefreshTime();
                    break;
                case EffectStackAggregateType.StackAndRefresh:
                    RefreshTime();
                    break;
            }
        }

        /// <summary>降层（降到 0 会走 <see cref="EffectStack"/> 的 setter 结束效果）。</summary>
        internal void DecreaseStack(int stack)
        {
            switch (stackData.DecreaseType)
            {
                case EffectStackDecreaseType.Clear:
                    EffectStack = 0;
                    break;
                case EffectStackDecreaseType.Refresh:
                    EffectStack -= stack;
                    if (effectStack >= 1) RefreshTime();
                    break;
            }
        }

        #endregion

        protected override void Init(AbilitySystemComponent source, AbilitySystemComponent target)
        {
            base.Init(source, target);

            effectStack = 0;
            RefreshTime();
        }

        /// <summary>把持续时间与周期计时归零（等于"从现在重新开始算"）。</summary>
        private void RefreshTime()
        {
            elapsedTime = 0f;
            _periodTimer = 0f;
        }

        /// <summary>周期到点 / 开始立即生效时的结算入口，子类实现具体的属性改动。</summary>
        protected abstract void ApplyEffect();
    }
}

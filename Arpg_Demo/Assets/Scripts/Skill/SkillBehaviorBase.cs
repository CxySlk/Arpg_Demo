using System.Collections.Generic;
using Character;
using GAS;
using Hovl;
using LLKFrame;
using UnityEngine;
using Attribute = GAS.Attribute;

namespace Skill
{
    /// <summary>
    /// 技能行为基类。封装一个技能在运行时的生命周期：CD 计时、播放驱动、消耗结算、帧事件/Cue/Effect 处理。
    /// </summary>
    /// <remarks>
    /// 每个玩家已学会的技能对应一个 <c>SkillBehaviorBase</c> 实例（由 <see cref="SkillBrainBase"/> 从
    /// <see cref="SkillConfig.SkillBehavior"/> 深拷贝而来）。子类通过实现 <see cref="DeepCopy"/> 与
    /// 覆写各 Hook 方法，定制具体技能的连段、消耗与逻辑。
    /// 职责划分：<see cref="AbilityPlayer"/> 负责按帧驱动 AbilityAsset 的帧事件；本类负责技能级的状态与结算。
    /// </remarks>
    public abstract class SkillBehaviorBase
    {
        #region 字段

        /// <summary>技能 ID（在技能配置列表中的索引），用于事件广播与行为字典索引。</summary>
        protected int skillID;

        /// <summary>所属技能大脑（技能的拥有者/管理者）。</summary>
        protected SkillBrainBase skillBrain;

        /// <summary>本行为对应的技能配置资产。</summary>
        protected SkillConfig skillConfig;

        /// <summary>帧驱动播放器：负责按帧执行 AbilityAsset 的帧事件。</summary>
        protected AbilityPlayer abilityPlayer = new();

        /// <summary>续段间隙计时器：片段播完时装载 <see cref="abilityInterval"/>，只在"片段已播完、等续段输入"的间隙里递减。</summary>
        protected float intervalTimer ;

        /// <summary>续段间隙计时是否已启动（片段播完时启动，续上下一段或技能结束时关闭）。</summary>
        protected bool isAbilityInterval;

        /// <summary>当前正在播放的技能片段资产。</summary>
        protected AbilityAsset curAbilityAsset;

        /// <summary>当前播放到的技能片段下标（连段用）。</summary>
        protected int abilityIndex = 0;

        /// <summary>技能是否处于播放（施法）中。</summary>
        protected bool skillIsPlaying;

        /// <summary>技能片段是否处于播放（施法）中。</summary>
        protected bool abilityIsPlaying;

        /// <summary>当前冷却计时器（秒），归零后技能才可再次释放。</summary>
        protected float cdTimer;

        /// <summary>是否已触发过"可以释放技能"的帧事件信号（用于能力播完时兜底解锁）。</summary>
        protected bool isTriggerCanReleaseSkillFlag = false;

        /// <summary>当前段是否已自然播完（AbilityPlayer 帧走完，AbilityFrameEnd 触发）。</summary>
        protected bool isCurrentAbilityDone;

        /// <summary>本行为当前是否已给自身加上了前摇标签（保证加/减 1:1，避免误删别人授的同一标签）。</summary>
        protected bool startupTagApplied;

        /// <summary>本段是否已触发"输入打断"帧事件（触发后移动/跳跃可打断本段；每段重置）。</summary>
        protected bool inputInterruptEnabled;

        /// <summary>是否已触发"开启武器拖尾"但还没触发"关闭武器拖尾"（段结束/打断/停驱动时兜底关闭）。</summary>
        protected bool weaponTrailOpened;

        #endregion

        #region 快捷访问（配置与角色）

        // 角色控制器
        protected CharacterControllerBase CharacterController => skillBrain.characterController;

        // 角色View控制器
        protected CharacterView CharacterView => CharacterController.CharacterView;

        /// <summary>施法者（ASC，GAS 能力系统拥有者）。</summary>
        protected AbilitySystemComponent GasOwner => skillBrain.GasOwner;

        /// <summary>公开的施法者（供 <see cref="AbilityPlayer"/> 等外部访问）。</summary>
        public AbilitySystemComponent Owner => GasOwner;

        /// <summary>技能专属事件中心（用于广播 CD、播放状态等）。</summary>
        protected EventCenter EventCenter => skillBrain.SkillEventCenter;

        /// <summary>技能完整冷却时间（秒），取自 <see cref="SkillConfig.CdTime"/>。</summary>
        protected float CdTime => skillConfig.CdTime;

        // 消耗
        protected Dictionary<AttributeType, float> CostDic => skillConfig.CostDic;

        /// <summary>该技能的多段技能片段（AbilityAsset 列表），取自配置。</summary>
        protected List<AbilityAsset> Abilities => skillConfig.Abilities;

        #endregion

        #region 状态（读写属性，变化时广播）

        /// <summary>
        /// 冷却计时器的读写属性；值变化时通过事件中心广播 <see cref="SkillEventKey.SkillCdTimerKey"/>，
        /// 供 UI 刷新冷却进度。
        /// </summary>
        protected virtual float CdTimer
        {
            get => cdTimer;
            set
            {
                if (Mathf.Approximately(cdTimer, value)) return;
                cdTimer = value;
                EventCenter.Publish(SkillEventKey.SkillCdTimerKey, skillID, cdTimer, CalculateCdTime());
            }
        }

        /// <summary>技能播放状态（读写）；值变化时通过事件中心广播 <see cref="SkillEventKey.SkillIsPlayingKey"/>，
        /// 供 UI 展示技能是否在施法/播放。整套连段的开关：所有片段释放完 / 续段间隙超时 / 被打断才变 false。</summary>
        public bool SkillIsPlaying
        {
            get => skillIsPlaying;
            protected set
            {
                if (skillIsPlaying == value) return;
                skillIsPlaying = value;

                // 广播给外界，技能是否处于技能播放中
                EventCenter.Publish(SkillEventKey.SkillIsPlayingKey, skillID, skillIsPlaying);
            }
        }

        /// <summary>技能片段播放状态（读写）；值变化时通过事件中心广播 <see cref="SkillEventKey.SkillAbilityIsPlayingKey"/>。
        /// 只覆盖"当前这一段 AbilityAsset"：片段开始播放变 true，片段播完（或被打断）立刻变 false。</summary>
        public bool AbilityIsPlaying
        {
            get => abilityIsPlaying;
            protected set
            {
                if (abilityIsPlaying == value) return;
                abilityIsPlaying = value;

                // 广播给外界，当前片段是否在播放
                EventCenter.Publish(SkillEventKey.SkillAbilityIsPlayingKey, skillID, abilityIsPlaying);
            }
        }

        /// <summary>当前段的前摇是否已结束（供 FSM 技能状态判定"可移动打断"）：只要身上没有前摇标签就算已结束。</summary>
        public bool IsStartupFinished => !IsUninterruptible;

        /// <summary>
        /// 是否处于"不可打断"阶段（前摇/霸体）：技能正在播放、且身上带着前摇标签
        /// （由前摇开始帧事件 FrameEventType.SkillStartupBegin 加上、前摇结束帧事件 SkillStartupEnd 移除）。
        /// 这段时间吃伤害不进受击硬直、也不能被移动打断。没配前摇开始事件的技能不带标签，即不霸体。
        /// </summary>
        public bool IsUninterruptible =>
            skillIsPlaying && GasOwner.CheckTag(GASGameplayTags.SkillStartup);

        /// <summary>当前段是否已播完（供 FSM 技能状态判定自然结束）。</summary>
        public bool IsCurrentAbilityDone => isCurrentAbilityDone;

        /// <summary>本段是否已解锁"输入打断"（由 SkillInputInterrupt 帧事件置位，每段重置）。</summary>
        public bool IsInputInterruptEnabled => inputInterruptEnabled;

        #endregion

        #region 初始化与生命周期

        /// <summary>
        /// 初始化技能行为。
        /// </summary>
        /// <param name="skillBrain">所属技能大脑。</param>
        /// <param name="skillConfig">技能配置。</param>
        /// <param name="skillID">技能 ID（配置列表索引）。</param>
        public void Init(SkillBrainBase skillBrain, SkillConfig skillConfig, int skillID)
        {
            this.skillBrain = skillBrain;
            this.skillConfig = skillConfig;
            this.skillID = skillID;
            abilityPlayer.Init(this, OnAbilityPlayEnd);
        }

        /// <summary>启动技能行为（订阅时间系统的帧更新，开始 CD 计时等）。</summary>
        public virtual void Start()
        {
            TimeSystem.Global.Subscribe(TickUpdate);
        }

        /// <summary>停止技能行为（退订帧更新、停止能力播放）。技能卸载/销毁时调用。</summary>
        public virtual void Stop()
        {
            TimeSystem.Global.UnSubscribe(TickUpdate);
            abilityPlayer.Stop();
            RemoveStartupTag(); // 停驱动：清掉前摇标签，避免残留
            inputInterruptEnabled = false;
            CloseWeaponTrailIfOpened(); // 停驱动：兜底关掉未关的拖尾
        }

        /// <summary>深拷贝本行为（用于为每个玩家/每个已学技能创建独立实例）。</summary>
        public abstract SkillBehaviorBase DeepCopy();

        /// <summary>能力播放完毕回调。</summary>
        /// <remarks>
        /// 若整个播放过程中没有触发过 <c>FrameEventType.CanReleaseSkill</c>，则在此兜底解锁释放；
        /// 若已触发过，则复位该标志，避免重复解锁。
        /// </remarks>
        protected virtual void OnAbilityPlayEnd()
        {
            // 当前片段播完：片段播放状态立刻置 false（"这一段的 AbilityAsset 放完了"）
            AbilityIsPlaying = false;

            // 当前段已自然播完（FSM 技能状态据此退出）
            isCurrentAbilityDone = true;

            // 只有没有触发过可以释放技能的事件，才会在能力播放结束时设置SkillBrain可以释放技能
            // 因此，这一步是防止忘记设置FrameEventType.CanReleaseSkill自动执行的
            if (!isTriggerCanReleaseSkillFlag) skillBrain.SetCanReleaseFlag(true);
            else isTriggerCanReleaseSkillFlag = false;

            // 还有下一段可连 → 进入"续段间隙"计时（间隙超时同样会让技能播放结束）
            // 已经是最后一段 → 整套技能播放结束
            CloseWeaponTrailIfOpened(); // 每段自然结束（含连段切段）：兜底关掉未关的拖尾
            if (abilityIndex < Abilities.Count) StartAbilityInterval();
            else OnSkillPlayEnd();
        }

        #endregion

        #region 每帧驱动

        /// <summary>每帧更新（由时间系统驱动）；技能未播放时推进冷却计时。</summary>
        protected virtual void TickUpdate(int frame)
        {
            if (!skillIsPlaying && cdTimer > 0)
                CdTimer = Mathf.Max(0, cdTimer - TimeSystem.FrameTime);
            TickAbilityInterval();
        }

        /// <summary>
        /// 逐帧递减续段间隙计时：只在"整套技能还在播放 + 当前片段已经播完 + 间隙计时已启动"时才算，
        /// 减到 0 表示玩家没在间隙内续上下一段 → 技能播放结束。
        /// </summary>
        protected virtual void TickAbilityInterval()
        {
            if (!skillIsPlaying || abilityIsPlaying || !isAbilityInterval) return;

            intervalTimer -= TimeSystem.FrameTime;
            if (intervalTimer <= 0)
            {
                OnSkillPlayEnd();
            }
        }

        #endregion

        #region 技能播放与连段

        /// <summary>播放技能（根据当前是否已处于播放状态分发到不同逻辑）。</summary>
        public virtual void PlaySkill()
        {
            // 每段（首段或连段切段）启动前复位段级标志：新的一段重新开始（前摇标签由开始帧事件再加）
            isCurrentAbilityDone = false;

            // 根据播放状态不同，处理不同的逻辑
            if (skillIsPlaying) PlaySkillInPlaying();
            else PlaySkillInNotPlaying();
        }

        /// <summary>技能已处于播放中时的再触发（连段/切段逻辑，默认播下一段）。</summary>
        protected virtual void PlaySkillInPlaying()
        {
            if (abilityIndex >= Abilities.Count) return; // 没有下一段可连了（正常情况下已被 CheckCanRelease 拦住）
            isAbilityInterval = false; // 续上了，关掉续段间隙计时
            PlayAbilitySegment(abilityIndex);
        }

        /// <summary>技能未播放时的首段触发（打开技能播放状态、结算消耗、播第一段）。</summary>
        protected virtual void PlaySkillInNotPlaying()
        {
            SkillIsPlaying = true;
            ApplySkillCost();
            PlayAbilitySegment(0);
        }

        /// <summary>播放第 index 段片段：打开片段播放状态、推进连段下标并驱动它的帧事件。</summary>
        protected virtual void PlayAbilitySegment(int index)
        {
            // 新的一段开始：先清掉上一段可能残留的前摇标签（本段的前摇由开始帧事件重新加上）
            RemoveStartupTag();
            inputInterruptEnabled = false;

            curAbilityAsset = Abilities[index];
            isCurrentAbilityDone = false;
            AbilityIsPlaying = true;
            abilityPlayer.PlayAbility(Abilities[index]);
            abilityIndex = index + 1;
        }

        /// <summary>启动续段间隙计时（当前片段播完、还有下一段可连时调用）。</summary>
        protected void StartAbilityInterval()
        {
            isAbilityInterval = true;
            intervalTimer = skillConfig.AbilityInterval;
        }

        /// <summary>
        /// 技能播放结束（所有片段播完 / 续段间隙超时 / 被打断都会走这里）：
        /// 复位连段下标与两个播放状态、通知技能大脑把自己从播放缓存移除，并按 resetCd 决定是否进入 CD。
        /// </summary>
        protected virtual void OnSkillPlayEnd(bool resetCd = true)
        {
            abilityIndex = 0;
            isAbilityInterval = false;
            intervalTimer = 0;
            AbilityIsPlaying = false;
            isCurrentAbilityDone = true;
            SkillIsPlaying = false;

            RemoveStartupTag(); // 技能结束：清掉前摇标签，避免残留
            inputInterruptEnabled = false;

            skillBrain.NotifySkillEnded(this);

            if (resetCd) ResetCdTimer();
        }

        /// <summary>
        /// 打断：立即结束当前片段与整套技能（片段状态、技能状态都置 false），并通知技能大脑从播放缓存移除。
        /// 处于前摇期（不可打断）时会直接忽略，见 <see cref="force"/>。
        /// </summary>
        /// <param name="resetCd">是否按一次完整释放结算进入 CD（默认 true，与移动打断的既有行为一致）。</param>
        /// <param name="force">true = 无视"前摇不可打断"（死亡、强制清场等必须打断的场合用）。</param>
        public void Interrupt(bool resetCd = true, bool force = false)
        {
            if (!skillIsPlaying && !abilityIsPlaying) return; // 本来就没在播
            if (!force && IsUninterruptible) return; // 前摇期：不可打断

            // 片段结束：停止帧驱动（剩余帧的伤害/效果不再触发，也不会再触发 AbilityFrameEnd）
            abilityPlayer.Stop();

            // 复位段级标志（本段结束；手动解锁释放，避免下一段被卡住）
            RemoveStartupTag(); // 被打断：清掉前摇标签，解锁打断
            inputInterruptEnabled = false;
            isTriggerCanReleaseSkillFlag = false;
            skillBrain.SetCanReleaseFlag(true);
            CloseWeaponTrailIfOpened(); // 段中途被打断：兜底关掉未关的拖尾（打断不走 OnAbilityPlayEnd）

            // 技能结束（片段状态与技能状态一起置 false、通知大脑、按需进 CD）
            OnSkillPlayEnd(resetCd);
        }

        /// <summary>移动打断当前技能段（前摇帧事件触发之后按移动触发）。等价于 <see cref="Interrupt"/>。</summary>
        public void CancelPlaying() => Interrupt();

        /// <summary>
        /// 输入打断：本段已解锁"输入打断"时取消当前技能（移动/跳跃输入由外部喂入）。
        /// 打断的判定与执行都归本行为，状态/输入层只负责调用。
        /// </summary>
        /// <returns>是否真的打断了。</returns>
        public bool TryInputInterrupt()
        {
            if (!skillIsPlaying || !inputInterruptEnabled) return false;

            Interrupt();
            return true;
        }

        #endregion

        #region 冷却计算

        /// <summary>重置技能 CD（通常为整段连段播放完毕时）。</summary>
        protected virtual void ResetCdTimer() => cdTimer = CalculateCdTime();

        /// <summary>计算技能 CD 时长（默认取配置 CD）。</summary>
        protected virtual float CalculateCdTime() => CdTime;

        #endregion

        #region 技能释放相关

        /// <summary>检查当前是否满足释放条件（CD 就绪 且 消耗足够）。</summary>
        public virtual bool CheckCanRelease()
        {
            return CheckCdTimer() && CheckSkillCost() && (!skillIsPlaying || abilityIndex < Abilities.Count);
        }

        /// <summary>检查技能 CD 是否就绪。</summary>
        protected virtual bool CheckCdTimer() => cdTimer <= 0;

        /// <summary>检查技能消耗是否满足（默认检查 MP 是否足够扣除 <see cref="Cost"/>）。</summary>
        protected virtual bool CheckSkillCost()
        {
            if (abilityIndex != 0) return true;
            foreach (KeyValuePair<AttributeType, float> cost in skillConfig.CostDic)
            {
                if (GasOwner.TryGetAttrValue(cost.Key, out Attribute attr))
                {
                    if (attr.CurValue < cost.Value)
                        return false;
                }
            }

            return true;
        }

        /// <summary>结算技能消耗（默认扣除 MP 属性的 <see cref="Cost"/>）。</summary>
        protected virtual void ApplySkillCost()
        {
            foreach (KeyValuePair<AttributeType, float> cost in CostDic)
            {
                GasOwner.AddAttrValue(cost.Key, AttributeValueType.CurValue, -cost.Value);
            }
        }

        #endregion

        #region 帧事件相关

        /// <summary>处理自定义帧事件前的预处理钩子（可修改/替换事件，默认原样返回）。</summary>
        public virtual FrameEvent BeforeHandleFrameEvent(FrameEvent frameEvent)
        {
            return frameEvent;
        }

        /// <summary>处理帧事件（按事件类型分发：解锁释放 / 自定义事件等）。</summary>
        public virtual void AfterHandleFrameEvent(FrameEvent frameEvent)
        {
            switch (frameEvent.EventType)
            {
                case FrameEventType.None:
                    break;
                case FrameEventType.CanReleaseSkill:
                    skillBrain.SetCanReleaseFlag(true);
                    isTriggerCanReleaseSkillFlag = true; // 已经触发了可以释放技能标志
                    break;
                // 前摇开始帧事件：给自己加上前摇(霸体)标签 → 进入不可打断阶段
                case FrameEventType.SkillStartupBegin:
                    AddStartupTag();
                    break;
                // 前摇结束帧事件：移除前摇标签 → 解锁打断（FSM 技能状态可在移动输入下打断本段；受击也能打断）
                case FrameEventType.SkillStartupEnd:
                    RemoveStartupTag();
                    break;
                // 开启根运动：动画位移交给本行为的 OnRootMotion 处理（受 AbilityAsset.rootMotionDis 限制）
                case FrameEventType.SkillRootMotionOn:
                    CharacterView.SetRootMotion(OnRootMotion);
                    break;
                // 关闭根运动：清空回调，动画不再推角色
                case FrameEventType.SkillRootMotionOff:
                    CharacterView.SetRootMotion(null);
                    break;
                case FrameEventType.LockTarget:
                    CharacterController.LockTarget(true);
                    break;
                case FrameEventType.UnLockTarget:
                    CharacterController.LockTarget(false);
                    break;
                case FrameEventType.StartWeaponTrail: // 开启武器拖尾
                    HS_SwordTrailPreset tarilPreset = frameEvent.objArg as HS_SwordTrailPreset;
                    CharacterController.StartWeaponTrail(frameEvent.intArg,tarilPreset);
                    weaponTrailOpened = true;
                    break;
                case FrameEventType.StopWeaponTrail: // 关闭武器拖尾
                    CharacterController.StopWeaponTrail();
                    weaponTrailOpened = false;
                    break;
                // 输入打断帧事件：解锁本段的"输入打断"（之后移动/跳跃可打断本段）
                case FrameEventType.SkillInputInterrupt:
                    inputInterruptEnabled = true;
                    break;
                case FrameEventType.Custom:
                    HandleCustomEvent(frameEvent.strArg, frameEvent);
                    break;
            }
        }

        /// <summary>处理自定义事件（子类覆写实现具体逻辑）。</summary>
        /// <param name="eventName">自定义事件名（取自 FrameEvent.strArg）。</param>
        /// <param name="frameEvent">帧事件对象。</param>
        protected virtual void HandleCustomEvent(string eventName, FrameEvent frameEvent)
        {
        }

        /// <summary>给自身 ASC 加上固定的前摇(霸体)标签（前摇开始帧事件触发时）。已加过则不重复加。</summary>
        private void AddStartupTag()
        {
            if (startupTagApplied) return;
            GasOwner.AddDynamicTag(GASGameplayTags.SkillStartup);
            startupTagApplied = true;
        }

        /// <summary>
        /// 移除自身 ASC 的固定前摇(霸体)标签（前摇结束帧事件 / 技能结束 / 被打断 / 停驱动时）。
        /// 只有本行为确实加过才移除：结束帧已移除、或开始帧根本没触发时直接跳过，
        /// 既保证意外被打断（开始帧触发、结束帧没来）时标签会被摘掉，又不会误减别人授的同一标签的引用计数。
        /// </summary>
        private void RemoveStartupTag()
        {
            if (!startupTagApplied) return;
            GasOwner.RemoveDynamicTag(GASGameplayTags.SkillStartup);
            startupTagApplied = false;
        }

        /// <summary>段结束/打断/停驱动时，若拖尾开了没关，则兜底关闭。</summary>
        private void CloseWeaponTrailIfOpened()
        {
            if (!weaponTrailOpened) return;
            CharacterController.StopWeaponTrail();
            weaponTrailOpened = false;
        }

        #endregion

        #region Cue显示相关

        /// <summary>触发 Cue 前的预处理钩子（可修改/替换 Cue，默认原样返回）。</summary>
        public virtual GameplayCueDurational BeforeHandleCue(GameplayCueDurational cue)
        {
            return cue;
        }

        /// <summary>处理 Cue：对施法者应用并返回创建的 Spec（未触发/标签不满足时返回 null）。</summary>
        public virtual void AfterHandleCue(GameplayCueDurational cue)
        {
            cue.Apply(GasOwner);
            if (cue is GameplayCueDurationalAnimation animCue)
            {
                CharacterView.SetRootMotion(animCue.RootMotion ? OnRootMotion : null);
            }
        }

        /// <summary>动画根运动回调（播放带根运动的动画 Cue 时注册）。</summary>
        protected virtual void OnRootMotion(Vector3 deltaPos, Quaternion deltaRot)
        {
            if (curAbilityAsset.rootMotionDis <= 0 ||
                !CharacterController.Target ||
                CharacterController.GetTargetSqrDis() > Mathf.Pow(curAbilityAsset.rootMotionDis, 2))
            {
                CharacterController.Move(deltaPos);
            }

            CharacterController.Rotate(deltaRot);
        }

        #endregion

        #region ReleaseGE相关

        /// <summary>释放效果前的预处理钩子（可修改/替换效果资产，默认原样返回）。</summary>
        public virtual ReleaseGEMarkEvent BeforeHandleReleaseGe(ReleaseGEMarkEvent geMarkEvent)
        {
            return geMarkEvent;
        }

        /// <summary>对单个目标应用效果（默认按层数 stack 调用目标的 ApplyEffect）。</summary>
        public virtual void AfterHandleReleaseGe(ReleaseGEMarkEvent geMarkEvent)
        {
            geMarkEvent.Execute(skillBrain.GasOwner);
        }

        #endregion
    }
}
using System.Collections.Generic;
using Character;
using GAS;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill
{
    public abstract class SkillBrainBase : MonoBehaviour
    {
        [LabelText("角色控制器"), SerializeField, Required]
        public CharacterControllerBase characterController;

        // ASC拥有者
        public AbilitySystemComponent GasOwner => characterController.Asc;

        // 技能的事件中心
        protected EventCenter skillEventCenter = new();
        public EventCenter SkillEventCenter => skillEventCenter; // 事件中心
        public bool CanRelease { get; private set; } = true; // 能否释放技能标志

        protected List<SkillConfig> skillConfigs;

        // 技能行为：[技能列表索引，技能行为]
        protected readonly Dictionary<int, SkillBehaviorBase> behaviorDic = new();

        /// <summary>正在播放的技能行为缓存（技能自然播完/续段间隙超时/被打断时由行为自己回调移除）。
        /// 允许同时存在多个：比如一个持续型技能 + 一个攻击段。</summary>
        private readonly List<SkillBehaviorBase> playingSkills = new();

        /// <summary>正在播放的技能行为列表（只读，供 UI/调试查看）。</summary>
        public IReadOnlyList<SkillBehaviorBase> PlayingSkills => playingSkills;

        /// <summary>最近一次播放中的技能行为；没有正在播放的技能时为 null。供 FSM 技能状态查询段状态。</summary>
        public SkillBehaviorBase CurrentBehavior => playingSkills.Count > 0 ? playingSkills[playingSkills.Count - 1] : null;

        /// <summary>是否有技能正在播放。</summary>
        public bool HasPlayingSkill => playingSkills.Count > 0;

        #region 属性

        // 角色控制器
        public CharacterControllerBase Character => characterController;

        /// <summary>当前技能是否已解锁"输入打断"（输入打断帧事件触发后可被移动/跳跃打断）。</summary>
        public bool CanCancelCurrentSkill => CurrentBehavior != null && CurrentBehavior.IsInputInterruptEnabled;

        /// <summary>无技能在播、或当前技能已解锁输入打断 → 允许（移动/跳跃）输入打断。</summary>
        public bool CanInputInterruptCurrentSkill => CurrentBehavior == null || CurrentBehavior.IsInputInterruptEnabled;

        /// <summary>把输入喂给当前技能行为，由行为按"是否解锁输入打断"决定是否取消自己。</summary>
        public void TryInputInterruptCurrentSkill() => CurrentBehavior?.TryInputInterrupt();

        /// <summary>是否有技能正处于"前摇（不可打断）"阶段：此时受击不该进硬直、也不该打断技能。</summary>
        public bool IsAnySkillUninterruptible
        {
            get
            {
                for (int i = 0; i < playingSkills.Count; i++)
                {
                    if (playingSkills[i].IsUninterruptible) return true;
                }
                return false;
            }
        }

        #endregion

        public virtual bool CheckCanPlaySkill(int skillID)
        {
            if (behaviorDic.TryGetValue(skillID, out SkillBehaviorBase skillBehavior))
                return CanRelease && skillBehavior.CheckCanRelease();
            return false;
        }

        public void PlaySkill(int skillID)
        {
            if (behaviorDic.TryGetValue(skillID, out SkillBehaviorBase skillBehavior))
            {
                skillBehavior.PlaySkill();
                AddPlayingSkill(skillBehavior);
                CanRelease = false;
            }
        }

        /// <summary>技能结束回调（技能自然播完 / 续段间隙超时 / 被打断时由技能行为调用）：从播放缓存里移除。</summary>
        public void NotifySkillEnded(SkillBehaviorBase skillBehavior)
        {
            if (skillBehavior == null) return;
            playingSkills.Remove(skillBehavior);
        }

        /// <summary>打断所有正在播放的技能：技能播放状态与片段播放状态都会被结束，并各自走一遍结束流程。</summary>
        /// <param name="resetCd">是否按一次完整释放结算进入 CD（默认 true）。</param>
        /// <param name="force">true = 无视"前摇不可打断"（死亡等必须打断的场合）。</param>
        public void InterruptAllSkills(bool resetCd = true, bool force = false)
        {
            // 倒序遍历：行为的 Interrupt 内部会回调 NotifySkillEnded 把自己从列表里摘掉
            for (int i = playingSkills.Count - 1; i >= 0; i--)
                playingSkills[i].Interrupt(resetCd, force);

            playingSkills.Clear();
            CanRelease = true;
        }

        /// <summary>打断指定技能。</summary>
        public void InterruptSkill(SkillBehaviorBase skillBehavior, bool resetCd = true, bool force = false)
        {
            if (skillBehavior == null) return;
            skillBehavior.Interrupt(resetCd, force);
        }

        /// <summary>按技能索引打断指定技能。</summary>
        public void InterruptSkill(int skillID, bool resetCd = true, bool force = false)
        {
            if (!behaviorDic.TryGetValue(skillID, out SkillBehaviorBase skillBehavior)) return;
            skillBehavior.Interrupt(resetCd, force);
        }

        /// <summary>移动打断当前技能段（前摇帧事件之后按移动时由 FSM 技能状态调用）。</summary>
        public void CancelCurrentSkill()
        {
            SkillBehaviorBase behavior = CurrentBehavior;
            if (behavior == null) return;

            // 打断会走技能结束流程，行为把自己从播放缓存里移除（CurrentBehavior 随之变成上一个或 null）
            behavior.Interrupt();
        }

        private void AddPlayingSkill(SkillBehaviorBase skillBehavior)
        {
            if (skillBehavior == null || playingSkills.Contains(skillBehavior)) return;
            playingSkills.Add(skillBehavior);
        }

        private void OnDestroy()
        {
            // 先打断所有在播技能(会广播状态、从播放缓存移除),再清事件中心,避免残留"播放中"状态
            InterruptAllSkills(false);

            foreach (SkillBehaviorBase behavior in behaviorDic.Values)
                behavior.Stop();
            behaviorDic.Clear();

            skillEventCenter.ClearAll();
            skillEventCenter = null;
        }

        /// <summary>
        /// 清掉已创建的技能行为(重复初始化时调用):停止帧驱动、清空行为字典与播放缓存，避免
        /// "旧行为还在逐帧驱动(继续结算伤害/效果)"以及 behaviorDic.Add 的重复键异常。
        /// </summary>
        protected void ClearBehaviors()
        {
            InterruptAllSkills(false);

            foreach (SkillBehaviorBase behavior in behaviorDic.Values)
                behavior.Stop();
            behaviorDic.Clear();
            CanRelease = true;
        }

        public void SetCanReleaseFlag(bool flag)
        {
            CanRelease = flag;
        }

        /// <summary>指定技能的"整套连段"是否正在播放（全部片段播完 / 续段间隙超时 / 被打断后才为 false）。</summary>
        public bool SkillIsPlaying(int skillID)
        {
            behaviorDic.TryGetValue(skillID, out SkillBehaviorBase skillBehavior);
            return skillBehavior is { SkillIsPlaying: true };
        }

        /// <summary>指定技能的"当前片段"是否正在播放（这一段 AbilityAsset 播完就为 false，连段时会反复 true→false）。</summary>
        public bool AbilityIsPlaying(int skillID)
        {
            behaviorDic.TryGetValue(skillID, out SkillBehaviorBase skillBehavior);
            return skillBehavior is { AbilityIsPlaying: true };
        }
    }
}
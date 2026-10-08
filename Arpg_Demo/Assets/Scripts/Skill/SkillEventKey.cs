using LLKFrame;

namespace Skill
{
    /// <summary>
    /// 技能系统的事件键（Key）定义。
    /// 统一在此处声明技能相关事件所用的 <see cref="EventKey{T1,T2,T3}"/>，
    /// 供 SkillBrain 通过事件中心（<c>EventCenter</c>）广播/订阅，避免魔法字符串散落。
    /// </summary>
    public static class SkillEventKey
    {
        /// <summary>
        /// 技能冷却计时事件。
        /// </summary>
        /// <remarks>
        /// 参数类型：1.技能索引（skillID）、2.技能当前剩余 CD 时间（秒）、3.技能的完整 CD 时间（秒）。
        /// 用于驱动 UI 展示技能冷却进度。
        /// </remarks>
        public static readonly EventKey<int, float, float> SkillCdTimerKey = "Skill.Cd.Timer";

        /// <summary>
        /// 技能播放状态事件。
        /// </summary>
        /// <remarks>
        /// 参数类型：1.技能索引（skillID）、2.是否正在播放（true-播放中，false-未播放）。
        /// 用于驱动 UI 展示技能是否处于施法/播放状态。
        /// </remarks>
        public static readonly EventKey<int, bool> SkillIsPlayingKey = "Skill.IsPlaying";

        /// <summary>
        /// 技能片段（单段 Ability）播放状态事件。
        /// </summary>
        /// <remarks>
        /// 参数类型：1.技能索引（skillID）、2.片段是否正在播放（true-当前片段播放中，false-片段已播完/被打断）。
        /// 与 <see cref="SkillIsPlayingKey"/> 的区别：技能状态是"整套连段"的开关（全部片段释放完 / 续段间隙超时 / 被打断才关闭），
        /// 而片段状态只覆盖"当前这一段 AbilityAsset"，所以连段时技能状态会保持 true 而片段状态会反复 true→false。
        /// </remarks>
        public static readonly EventKey<int, bool> SkillAbilityIsPlayingKey = "Skill.Ability.IsPlaying";
    }
}

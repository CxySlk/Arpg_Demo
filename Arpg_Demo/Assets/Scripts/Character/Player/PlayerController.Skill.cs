public partial class PlayerController
{
    #region 技能状态与打断

    public override bool SkillIsPlaying(int skillID)
    {
        return skillBrain.SkillIsPlaying(skillID);
    }

    /// <summary>打断所有正在播放的技能(技能状态与片段状态一起结束)。</summary>
    public override void InterruptAllSkills(bool resetCd = true, bool force = false)
    {
        skillBrain.InterruptAllSkills(resetCd, force);
    }

    /// <summary>打断指定技能。</summary>
    public override void InterruptSkill(int skillID, bool resetCd = true, bool force = false)
    {
        skillBrain.InterruptSkill(skillID, resetCd, force);
    }

    #endregion
}

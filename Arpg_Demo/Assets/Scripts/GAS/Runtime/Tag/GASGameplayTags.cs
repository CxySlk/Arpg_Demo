namespace GAS
{
    /// <summary>
    /// 代码里按名引用的固定 GameplayTag 名（需与 GASEditorData 标签表中的名称一致）。
    /// 通过 <see cref="GameplayTagAggregator"/> 的字符串重载加/减标签，避免给每个配置挂一个标签字段。
    /// </summary>
    public static class GASGameplayTags
    {
        /// <summary>技能前摇/霸体标签</summary>
        public const string SkillStartup = "Status.技能前摇";
    }
}

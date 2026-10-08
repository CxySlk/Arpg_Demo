namespace GAS
{
    public interface ITagCondition
    {
        public GameplayTagSet RequiredTags { get; }
        public GameplayTagSet BlockedTags { get; }
    }
}

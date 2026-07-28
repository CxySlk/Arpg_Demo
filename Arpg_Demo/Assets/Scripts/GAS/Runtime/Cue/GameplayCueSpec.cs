namespace GAS
{
    public abstract class GameplayCueSpec
    {
        public AbilitySystemComponent Owner { get; protected set; }
        public abstract void Init(GameplayCue cue, AbilitySystemComponent owner);
    }

    public abstract class GameplayCueSpec<T> : GameplayCueSpec where T : GameplayCue
    {
        protected T Cue;

        public override void Init(GameplayCue cue, AbilitySystemComponent owner)
        {
            Cue = cue as T;
            Owner = owner;
        }
    }
}

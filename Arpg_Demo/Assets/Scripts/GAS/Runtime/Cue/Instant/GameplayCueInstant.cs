namespace GAS
{
    public abstract class GameplayCueInstant<T> : GameplayCue<T> where T : GameplayCueInstantSpec
    {
        public override void ApplyFrom(AbilitySystemComponent asc)
        {
            if (Triggerable(asc))
            {
                GameplayCueInstantSpec instantCue = CreateSpec();
                instantCue.Init(this, asc);
                instantCue.Trigger();
            }
        }
    }
}

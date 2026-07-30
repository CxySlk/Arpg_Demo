namespace GAS
{
    public abstract class GameplayCueDurational : GameplayCue<GameplayCueDurationalSpec>
    {
        protected int _startFrame;
        public int DurationFrame;

        public override void ApplyFrom(AbilitySystemComponent asc)
        {
            GameplayCueDurationalSpec spec = CreateSpec();
            spec.Init(this, asc);
            _startFrame = asc.CurrentFrame;
            spec.TriggerAdd();
        }
    }
}

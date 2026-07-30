namespace GAS
{
    /// <summary>
    /// 瞬时 Cue 的非泛型基类。CreateSpec 返回 GameplayCueInstantSpec，子类可改用泛型版本。
    /// </summary>
    public abstract class GameplayCueInstant : GameplayCue<GameplayCueInstantSpec>
    {
        public override void ApplyFrom(AbilitySystemComponent asc)
        {
            GameplayCueInstantSpec spec = CreateSpec();
            spec.Init(this, asc);
            spec.Trigger();
        }
    }
}

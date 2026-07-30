namespace GAS
{
    /// <summary>
    /// 瞬时 Cue 的 Spec 基类。泛型化以支持不同类型的 Cue 资产。
    /// </summary>
    public abstract class GameplayCueInstantSpec : GameplayCueSpec<GameplayCueInstant>
    {
        public abstract void Trigger();
    }
    /// <summary>
    /// 瞬时 Cue 的 Spec 基类。泛型化以支持不同类型的 Cue 资产。
    /// </summary>
    public abstract class GameplayCueInstantSpec<TCue> : GameplayCueInstantSpec
        where TCue : GameplayCueInstant
    {
        protected TCue InstanceCue => Cue as TCue;
    }
}

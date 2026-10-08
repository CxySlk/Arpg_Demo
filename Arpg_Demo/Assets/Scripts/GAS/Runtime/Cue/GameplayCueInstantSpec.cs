namespace GAS
{
    public abstract class GameplayCueInstantSpec<TCue> : GameplayCueSpec where TCue : GameplayCueInstant
    {
        protected TCue InstanceCue => cue as TCue;
    }
}
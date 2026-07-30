using LLKFrame;

namespace GAS
{
    /// <summary>
    /// 瞬时动画 Cue 的运行时 Spec。在所有者 ASC 的 AnimationController 上播放动画片段。
    /// </summary>
    public class GameplayCueAnimationInstantSpec : GameplayCueInstantSpec<GameplayCueAnimationInstant>
    {
        public override void Trigger()
        {
            if (Owner == null) return;
            AnimationController controller = Owner.GetComponent<AnimationController>();
            if (controller == null || Cue == null) return;
            controller.PlaySingleAnimation(InstanceCue.AnimationClip);

            Destroy();
        }
    }
}

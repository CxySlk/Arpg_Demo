using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public class GameplayCueAnimationInstantSpec : GameplayCueInstantSpec
    {
        public override void Trigger()
        {
            if (Owner == null) return;
            AnimationController controller = Owner.GetComponent<AnimationController>();
            if (controller == null) return;
            controller.PlaySingleAnimation(Cue.AnimationClip);
        }
    }
}

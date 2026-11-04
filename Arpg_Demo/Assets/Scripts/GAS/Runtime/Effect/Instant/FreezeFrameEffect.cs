using LLKFrame;
using Sirenix.OdinInspector;

namespace GAS
{
    public class FreezeFrameEffect : GameplayInstantEffect
    {
        [LabelText("顿帧时长")] public float freezeTime = .3f;

        protected override void ApplyEffect()
        {
            TimeSystem.FreezeFrame(0.5f, freezeTime);
        }
    }
}
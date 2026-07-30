using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace GAS
{
    public class ReleaseGEMarkEvent : MarkEventBase
    {
        [LabelText("目标捕捉器")]
        public TargetCatcherBase TargetCatcher;

        [LabelText("Effect")]
        public List<GameplayEffectAsset> EffectAssets;
    }
}

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public class InstanceCueMarkEvent : MarkEventBase
    {
        [SerializeField,LabelText("即时Cue")]
        private List<GameplayCueInstant> _instantCues;
        public List<GameplayCueInstant> InstantCues => _instantCues;
    }
}

using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public abstract class MarkEventBase
    {
        [SerializeField, LabelText("起始帧")]
        private int _startFrame;
        public int StartFrame => _startFrame;
    }
}

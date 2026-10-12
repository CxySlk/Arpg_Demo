using UnityEngine;

namespace GAS
{
    /// <summary>屏幕震动契约：由表现层实现，GAS 效果通过它触发屏震，避免反向依赖 Cinemachine。</summary>
    public interface IScreenShaker
    {
        /// <param name="velocity">脉冲方向 + 力度。</param>
        void Shake(Vector3 velocity);
    }
}

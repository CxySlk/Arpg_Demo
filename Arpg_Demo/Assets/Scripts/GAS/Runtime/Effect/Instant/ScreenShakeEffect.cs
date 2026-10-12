using Sirenix.OdinInspector;
using UnityEngine;

namespace GAS
{
    public class ScreenShakeEffect : GameplayInstantEffect
    {
        [LabelText("力度")] public float force = 1f;

        [LabelText("方向"), ShowIf("@randomDirection == false")]
        public Vector3 direction = Vector3.down;

        [LabelText("随机方向")] public bool randomDirection = true;

        protected override void ApplyEffect()
        {
            IScreenShaker shaker = source.GetComponent<IScreenShaker>(); // 取施法者身上的屏震
            if (shaker == null) return;

            Vector3 dir = randomDirection ? Random.onUnitSphere : direction.normalized;
            if (dir == Vector3.zero) return; // 方向为零 → 不震

            shaker.Shake(dir * force);
        }
    }
}

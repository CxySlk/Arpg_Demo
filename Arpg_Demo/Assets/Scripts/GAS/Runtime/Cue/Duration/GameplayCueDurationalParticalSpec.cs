using LLKFrame;
using UnityEngine;

namespace GAS
{
    public class GameplayCueDurationalParticalSpec : GameplayCueDurationalSpec<GameplayCueDurationalPartical>
    {
        private GameObject instance;

        public override void TriggerAdd()
        {
            Transform parent = InstantCue.FollowTarget && Owner != null ? Owner.transform : null;
            instance = PoolSystem.GetGameObject(InstantCue.ParticlePrefab, parent);
            instance.transform.localPosition = InstantCue.PosOffset;
            instance.transform.localRotation = Quaternion.LookRotation(InstantCue.RotationOffset);

            foreach (ParticleSystem ps in parent.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }

        protected override void OnTick(int curFrame)
        {
            if (curFrame >= InstantCue.DurationFrame)
            {
                // 回收
                Destroy();
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            instance.PushGameObject();
            instance = null;
        }
    }
}

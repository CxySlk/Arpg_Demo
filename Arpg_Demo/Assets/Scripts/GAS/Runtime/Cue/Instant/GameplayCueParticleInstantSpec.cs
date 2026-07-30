using LLKFrame;
using System.Collections;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// 瞬时粒子 Cue 的运行时 Spec。在所有者 ASC 位置生成粒子预制体实例。
    /// 支持跟随目标和定时自动回收。
    /// </summary>
    public class GameplayCueParticleInstantSpec : GameplayCueInstantSpec<GameplayCueParticleInstant>
    {
        private GameObject instance;

        public override void Trigger()
        {
            if (Owner == null || Cue == null || InstanceCue.ParticlePrefab == null) return;

            Transform parent = InstanceCue.FollowTarget ? Owner.transform : null;
            instance = PoolSystem.GetGameObject(InstanceCue.ParticlePrefab, parent);
            instance.transform.localPosition = InstanceCue.PosOffset;
            instance.transform.localRotation = Quaternion.LookRotation(InstanceCue.RotationOffset);

            foreach (ParticleSystem ps in instance.GetComponentsInChildren<ParticleSystem>())
                ps.Play();

            if (InstanceCue.AutoRecycleTime > 0)
                MonoSystem.Start_Coroutine(AutoRecycle(InstanceCue.AutoRecycleTime));

            Destroy();
        }

        private IEnumerator AutoRecycle(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            instance.PushGameObject();
            instance = null;
        }
    }
}
